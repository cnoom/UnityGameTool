// asmdef Version Defines，安装 UniTask 包后自动启用。

#if UNITASK_SUPPORT

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.FloatingText
{
    /// <summary>
    /// 基于 UniTask 的飘字组件。支持异步等待单条飘字完成和 CancellationToken 取消。
    /// 需要安装 UniTask 包后自动编译。
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingTextSpawnerUniTask : MonoBehaviour, IFloatingTextSpawner
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
        private CancellationTokenSource _tickCts;
        private readonly Dictionary<int, TMP_Text> _activeTexts = new Dictionary<int, TMP_Text>();
        private readonly Dictionary<int, FloatingTextRequest> _activeRequests = new Dictionary<int, FloatingTextRequest>();
        private readonly Queue<TMP_Text> _pool = new Queue<TMP_Text>();
        private readonly List<int> _completedIds = new List<int>();

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

            PrewarmPool();
        }

        /// <inheritdoc />
        public int Spawn(FloatingTextRequest request)
        {
            float randomX = UnityEngine.Random.Range(-_config.RandomSpreadX, _config.RandomSpreadX);
            float randomY = UnityEngine.Random.Range(-_config.RandomSpreadY, _config.RandomSpreadY);

            Vector3 worldPos = new Vector3(request.WorldX, 0f, request.WorldZ);
            Vector3 screenPos = _worldCamera.WorldToScreenPoint(worldPos);
            screenPos.x += randomX;
            screenPos.y += randomY;

            int id = _engine.Add(
                request.Text,
                screenPos.x,
                screenPos.y,
                request.ScaleMultiplier,
                request.EnableShake);

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

        /// <summary>
        /// 异步生成一条飘字，等待该飘字完成后返回。
        /// </summary>
        /// <param name="request">飘字请求参数</param>
        /// <param name="token">取消令牌</param>
        public async UniTask SpawnAsync(FloatingTextRequest request, CancellationToken token = default)
        {
            int id = Spawn(request);

            try
            {
                await UniTask.WaitWhile(
                    () => _engine.IsActive(id),
                    cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                _engine.Stop(id);
                throw;
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            CancelTick();
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
            text.rectTransform.localScale = Vector3.one;
            text.alpha = 1f;
            _pool.Enqueue(text);
        }

        private void EnsureRunning()
        {
            if (_tickCts == null || _tickCts.IsCancellationRequested)
            {
                _tickCts?.Dispose();
                _tickCts = new CancellationTokenSource();
                TickLoop(_tickCts.Token).Forget();
            }
        }

        private async UniTaskVoid TickLoop(CancellationToken token)
        {
            try
            {
                while (_engine.HasActive)
                {
                    await UniTask.Yield(token);

                    _engine.Tick(Time.deltaTime);

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

                        if (_activeRequests.TryGetValue(id, out var req))
                        {
                            Vector3 wp = new Vector3(req.WorldX, 0f, req.WorldZ);
                            Vector3 sp = _worldCamera.WorldToScreenPoint(wp);
                            Vector3 pos = textObj.rectTransform.position;
                            pos.x = sp.x + animData.XOffset;
                            pos.y = sp.y + animData.YOffset;
                            textObj.rectTransform.position = pos;
                        }

                        textObj.rectTransform.localScale = Vector3.one * animData.Scale;
                        textObj.alpha = animData.Alpha;
                    }

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

                    _engine.RemoveCompleted();
                }
            }
            catch (OperationCanceledException)
            {
                // 取消时清理
            }
            finally
            {
                _tickCts?.Dispose();
                _tickCts = null;
            }
        }

        private void CancelTick()
        {
            if (_tickCts != null)
            {
                _tickCts.Cancel();
            }
        }

        private void OnDestroy()
        {
            CancelTick();

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

#endif
