using System.Collections;
using UnityEngine;

namespace CNoom.UnityGameTool.Pulse
{
    /// <summary>
    /// 脉冲/呼吸效果驱动组件。通过协程驱动周期性动画，
    /// 零外部依赖，开箱即用。
    /// </summary>
    [DisallowMultipleComponent]
    public class PulseDriver : MonoBehaviour, IPulse
    {
        [Header("默认脉冲配置")]
        [Tooltip("默认脉冲配置，Play() 无参时使用")]
        [SerializeField]
        private PulseConfig _defaultConfig = new PulseConfig();

        private PulseEngine _engine;
        private Coroutine _tickCoroutine;

        private Vector3 _originalLocalScale;
        private float _originalAlpha;
        private Vector3 _originalLocalPos;
        private bool _hasOriginal;

        /// <inheritdoc />
        public bool IsPlaying => _engine != null && _engine.IsPlaying;

        /// <inheritdoc />
        public float CurrentScale => _engine != null ? 1f : 1f;

        /// <inheritdoc />
        public float CurrentAlpha => _engine != null ? 1f : 1f;

        /// <inheritdoc />
        public float CurrentYOffset => _engine != null ? 0f : 0f;

        /// <inheritdoc />
        public event PulseCompleteHandler OnComplete;

        private void Awake()
        {
            _engine = new PulseEngine();
        }

        /// <inheritdoc />
        public void Play()
        {
            CaptureOriginal();
            _engine.Begin(_defaultConfig);
            EnsureRunning();
        }

        /// <inheritdoc />
        public void Play(PulseConfig config)
        {
            CaptureOriginal();
            _engine.Begin(config);
            EnsureRunning();
        }

        /// <inheritdoc />
        public void Pause()
        {
            _engine?.Pause();
        }

        /// <inheritdoc />
        public void Resume()
        {
            _engine?.Resume();
        }

        /// <inheritdoc />
        public void Stop()
        {
            StopDriver();
            _engine?.Stop();
            RestoreOriginal();
        }

        /// <inheritdoc />
        public void Skip()
        {
            if (_engine != null && (_engine.IsPlaying || _engine.IsPaused))
            {
                StopDriver();
                _engine.SkipToEnd();
                RestoreOriginal();
                OnComplete?.Invoke();
            }
        }

        private void CaptureOriginal()
        {
            if (!_hasOriginal)
            {
                _originalLocalScale = transform.localScale;
                _originalLocalPos = transform.localPosition;
                _hasOriginal = true;
            }
        }

        private void RestoreOriginal()
        {
            if (_hasOriginal)
            {
                transform.localScale = _originalLocalScale;
                transform.localPosition = _originalLocalPos;
                _hasOriginal = false;
            }
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
            while (_engine.IsPlaying)
            {
                var frameData = _engine.Tick(Time.deltaTime);

                if (frameData.Completed)
                {
                    _tickCoroutine = null;
                    RestoreOriginal();
                    OnComplete?.Invoke();
                    yield break;
                }

                // 应用缩放
                if (frameData.Scale != 1f)
                {
                    transform.localScale = _originalLocalScale * frameData.Scale;
                }

                // 应用 Y 偏移
                if (frameData.YOffset != 0f)
                {
                    transform.localPosition = _originalLocalPos + new Vector3(0f, frameData.YOffset, 0f);
                }

                // Alpha 需要由子类或外部处理（通过 CanvasGroup 等）
                // 这里通过事件通知外部
                OnAlphaChanged(frameData.Alpha);

                yield return null;
            }

            _tickCoroutine = null;
            RestoreOriginal();
        }

        /// <summary>
        /// Alpha 变化时调用。子类可重写以应用 Alpha 到 CanvasGroup 或材质。
        /// 默认尝试通过 CanvasGroup 应用。
        /// </summary>
        /// <param name="alpha">当前 Alpha 值 0~1</param>
        protected virtual void OnAlphaChanged(float alpha)
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
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
