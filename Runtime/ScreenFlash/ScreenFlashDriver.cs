using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CNoom.UnityGameTool.ScreenFlash
{
    /// <summary>
    /// 屏幕特效驱动组件。自动创建全屏覆盖层，通过协程驱动闪烁动画，
    /// 零外部依赖，开箱即用。支持多特效叠加。
    /// </summary>
    [DisallowMultipleComponent]
    public class ScreenFlashDriver : MonoBehaviour, IScreenFlash
    {
        [Header("默认闪烁配置")]
        [Tooltip("默认闪烁配置，Flash() 无参时使用")]
        [SerializeField]
        private ScreenFlashConfig _defaultConfig = new ScreenFlashConfig();

        private ScreenFlashEngine _engine;
        private Coroutine _tickCoroutine;
        private Image _overlayImage;
        private readonly List<int> _completedIds = new List<int>();

        /// <inheritdoc />
        public bool IsPlaying => _engine != null && _engine.IsPlaying;

        /// <inheritdoc />
        public int ActiveCount => _engine != null ? _engine.ActiveCount : 0;

        /// <inheritdoc />
        public event ScreenFlashCompleteHandler OnComplete;

        private void Awake()
        {
            _engine = new ScreenFlashEngine(_defaultConfig);
            CreateOverlay();
        }

        /// <inheritdoc />
        public int Flash()
        {
            int id = _engine.AddFlash(null);
            EnsureRunning();
            return id;
        }

        /// <inheritdoc />
        public int Flash(ScreenFlashConfig config)
        {
            int id = _engine.AddFlash(config);
            EnsureRunning();
            return id;
        }

        /// <inheritdoc />
        public int Flash(Color color, float duration)
        {
            var config = new ScreenFlashConfig(color, duration);
            int id = _engine.AddFlash(config);
            EnsureRunning();
            return id;
        }

        /// <inheritdoc />
        public void StopAll()
        {
            StopDriver();
            _engine?.StopAll();
            HideOverlay();
        }

        /// <inheritdoc />
        public void Stop(int flashId)
        {
            if (_engine != null && _engine.Stop(flashId))
            {
                if (!_engine.IsPlaying)
                {
                    StopDriver();
                    HideOverlay();
                }
            }
        }

        private void CreateOverlay()
        {
            // 创建全屏覆盖 Image
            var go = new GameObject("ScreenFlashOverlay");
            go.transform.SetParent(transform, false);

            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            _overlayImage = go.AddComponent<Image>();
            _overlayImage.color = Color.clear;
            _overlayImage.raycastTarget = false;

            // 置于最上层
            go.transform.SetAsLastSibling();
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
                var frameData = _engine.Tick(Time.deltaTime);

                // 触发完成回调
                _completedIds.Clear();
                _engine.GetCompletedIds(_completedIds);
                for (int i = 0; i < _completedIds.Count; i++)
                {
                    OnComplete?.Invoke(_completedIds[i]);
                }

                // 应用颜色到覆盖层
                if (frameData.Alpha > 0.001f)
                {
                    _overlayImage.enabled = true;
                    _overlayImage.color = new Color(
                        frameData.R,
                        frameData.G,
                        frameData.B,
                        frameData.Alpha);
                }
                else
                {
                    HideOverlay();
                }

                yield return null;
            } while (_engine.IsPlaying);

            _tickCoroutine = null;
            HideOverlay();
        }

        private void HideOverlay()
        {
            if (_overlayImage != null)
            {
                _overlayImage.enabled = false;
                _overlayImage.color = Color.clear;
            }
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
        }
    }
}
