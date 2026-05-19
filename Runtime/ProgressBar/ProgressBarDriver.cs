using System.Collections;
using UnityEngine;

namespace CNoom.UnityGameTool.ProgressBar
{
    /// <summary>
    /// 进度条驱动组件。通过协程驱动进度条平滑过渡和延迟扣减条效果，
    /// 零外部依赖，开箱即用。
    /// </summary>
    [DisallowMultipleComponent]
    public class ProgressBarDriver : MonoBehaviour, IProgressBar
    {
        [Header("进度条配置")]
        [Tooltip("进度条配置，可直接在 Inspector 中编辑")]
        [SerializeField]
        private ProgressBarConfig _config = new ProgressBarConfig();

        private ProgressBarEngine _engine;
        private Coroutine _tickCoroutine;

        /// <inheritdoc />
        public float Value => _engine != null ? _engine.Value : 0f;

        /// <inheritdoc />
        public float DelayedValue => _engine != null ? _engine.DelayedValue : 0f;

        /// <inheritdoc />
        public bool IsTransitioning => _engine != null && _engine.IsTransitioning;

        /// <inheritdoc />
        public event ProgressBarUpdateHandler OnValueChanged;

        /// <inheritdoc />
        public event ProgressBarCompleteHandler OnComplete;

        private void Awake()
        {
            _engine = new ProgressBarEngine(_config);
        }

        /// <inheritdoc />
        public void Set(float value)
        {
            _engine.SetValue(value);
            OnValueChanged?.Invoke(_engine.Value);
        }

        /// <inheritdoc />
        public void TransitionTo(float targetValue)
        {
            _engine.BeginTransition(targetValue);
            EnsureRunning();
        }

        /// <inheritdoc />
        public void Add(float delta)
        {
            float target = _engine != null ? _engine.TargetValue + delta : delta;
            TransitionTo(target);
        }

        /// <inheritdoc />
        public void Subtract(float delta)
        {
            float target = _engine != null ? _engine.TargetValue - delta : -delta;
            TransitionTo(target);
        }

        /// <inheritdoc />
        public void Stop()
        {
            StopDriver();
            _engine?.Stop();
        }

        /// <inheritdoc />
        public void Reset()
        {
            StopDriver();
            _engine?.Reset();
            OnValueChanged?.Invoke(0f);
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
            while (_engine.IsTransitioning)
            {
                var frameData = _engine.Tick(Time.deltaTime);

                OnValueChanged?.Invoke(frameData.Value);

                // 主进度条完成过渡
                if (frameData.ValueTransitionComplete)
                {
                    if (frameData.Value >= 0.9999f)
                    {
                        OnComplete?.Invoke();
                    }
                }

                yield return null;
            }

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
        }
    }
}
