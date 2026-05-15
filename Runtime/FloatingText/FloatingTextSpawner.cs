using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.FloatingText
{
    /// <summary>
    /// 飘字驱动组件。管理 TMP 对象池，将引擎计算的动画数据应用到 TMP_Text，
    /// 支持世界坐标到屏幕坐标的转换。
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingTextSpawner : MonoBehaviour, IFloatingTextSpawner
    {
        [Header("飘字配置")]
        [Tooltip("飘字动画参数配置")]
        [SerializeField]
        private FloatingTextConfig _config = new FloatingTextConfig();

        [Header("预制体")]
        [Tooltip("飘字 TMP 预制体")]
        [SerializeField]
        private TMP_Text _prefab;

        [Header("相机")]
        [Tooltip("世界相机（用于 WorldToScreen 坐标转换）")]
        [SerializeField]
        private Camera _worldCamera;

        private FloatingTextEngine _engine;
        private readonly Dictionary<int, TMP_Text> _activeTexts = new Dictionary<int, TMP_Text>();
        private readonly Dictionary<int, FloatingTextRequest> _activeRequests = new Dictionary<int, FloatingTextRequest>();
        private readonly Queue<TMP_Text> _pool = new Queue<TMP_Text>();
        private readonly List<int> _completedIds = new List<int>();
        private Coroutine _tickCoroutine;

        /// <inheritdoc />
        public int ActiveCount => _engine != null ? _engine.ActiveCount : 0;

        /// <inheritdoc />
        public event FloatingTextCompleteHandler OnComplete;

        private void Awake()
        {
            _engine = new FloatingTextEngine(_config);

            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }

            // 预热对象池
            PrewarmPool();
        }

        /// <inheritdoc />
        public int Spawn(FloatingTextRequest request)
        {
            // 生成随机偏移
            float randomX = Random.Range(-_config.RandomSpreadX, _config.RandomSpreadX);
            float randomY = Random.Range(-_config.RandomSpreadY, _config.RandomSpreadY);

            // 世界坐标转屏幕坐标
            Vector3 worldPos = new Vector3(request.WorldX, 0f, request.WorldZ);
            Vector3 screenPos = _worldCamera.WorldToScreenPoint(worldPos);
            screenPos.x += randomX;
            screenPos.y += randomY;

            // 添加到引擎
            int id = _engine.Add(
                request.Text,
                screenPos.x,
                screenPos.y,
                request.ScaleMultiplier,
                request.EnableShake);

            // 获取 TMP 对象并初始化
            TMP_Text textObj = GetFromPool();
            textObj.text = request.Text;
            textObj.color = request.Color;
            textObj.alpha = 0f;
            textObj.rectTransform.position = new Vector3(screenPos.x, screenPos.y, 0f);

            _activeTexts[id] = textObj;
            _activeRequests[id] = request;

            EnsureRunning();
            return id;
        }

        /// <inheritdoc />
        public void Clear()
        {
            StopDriver();
            _engine?.StopAll();

            foreach (var kvp in _activeTexts)
            {
                if (kvp.Value != null)
                {
                    ReturnToPool(kvp.Value);
                }
            }

            _activeTexts.Clear();
            _activeRequests.Clear();
        }

        private void PrewarmPool()
        {
            if (_prefab == null) return;

            for (int i = 0; i < _config.PoolSize; i++)
            {
                TMP_Text obj = Instantiate(_prefab, transform);
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        private TMP_Text GetFromPool()
        {
            if (_pool.Count > 0)
            {
                TMP_Text obj = _pool.Dequeue();
                obj.gameObject.SetActive(true);
                return obj;
            }

            return Instantiate(_prefab, transform);
        }

        private void ReturnToPool(TMP_Text text)
        {
            if (text == null) return;
            text.gameObject.SetActive(false);

            // 重置状态
            text.rectTransform.localScale = Vector3.one;
            text.alpha = 1f;

            _pool.Enqueue(text);
        }

        private void EnsureRunning()
        {
            if (_tickCoroutine == null)
            {
                _tickCoroutine = StartCoroutine(TickRoutine());
            }
        }

        private IEnumerator TickRoutine()
        {
            do
            {
                _engine.Tick(Time.deltaTime);

                // 应用动画数据到所有活跃实例
                var keys = new List<int>(_activeTexts.Keys);
                for (int i = keys.Count - 1; i >= 0; i--)
                {
                    int id = keys[i];
                    if (!_engine.TryGetAnimData(id, out var animData))
                    {
                        continue;
                    }

                    if (!_activeTexts.TryGetValue(id, out var textObj) || textObj == null)
                    {
                        continue;
                    }

                    // 位置偏移
                    Vector3 pos = textObj.rectTransform.position;
                    // 找到初始位置（存储在 request 中）
                    if (_activeRequests.TryGetValue(id, out var req))
                    {
                        float randomX = 0f; // 随机偏移已在 Spawn 时应用
                        float randomY = 0f;
                        // 重新计算屏幕位置作为基准
                        Vector3 wp = new Vector3(req.WorldX, 0f, req.WorldZ);
                        Vector3 sp = _worldCamera.WorldToScreenPoint(wp);
                        pos.x = sp.x + animData.XOffset;
                        pos.y = sp.y + animData.YOffset;
                    }

                    textObj.rectTransform.position = pos;

                    // 缩放
                    textObj.rectTransform.localScale = Vector3.one * animData.Scale;

                    // 透明度
                    textObj.alpha = animData.Alpha;
                }

                // 处理完成的实例
                _completedIds.Clear();
                _engine.GetCompletedIds(_completedIds);
                for (int i = 0; i < _completedIds.Count; i++)
                {
                    int completedId = _completedIds[i];
                    if (_activeTexts.TryGetValue(completedId, out var textObj) && textObj != null)
                    {
                        ReturnToPool(textObj);
                    }

                    _activeTexts.Remove(completedId);
                    _activeRequests.Remove(completedId);
                    OnComplete?.Invoke(completedId);
                }

                // 清理引擎中已完成的实例
                _engine.RemoveCompleted();

                yield return null;
            } while (_engine.HasActive);

            _tickCoroutine = null;
        }

        private void StopDriver()
        {
            if (_tickCoroutine != null)
            {
                StopCoroutine(_tickCoroutine);
                _tickCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            StopDriver();

            // 清理对象池
            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
            }
        }
    }
}
