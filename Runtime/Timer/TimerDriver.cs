using System.Collections;
using UnityEngine;

namespace CNoom.UnityGameTool.Timer
{
    /// <summary>
    /// 基于协程的游戏计时器组件。零外部依赖，开箱即用。
    /// 支持倒计时/正计时、暂停/恢复、时间缩放、警告阈值。
    /// </summary>
    public class TimerDriver : MonoBehaviour, ITimer
    {
        [Header("计时器配置")]
        [Tooltip("计时器配置，可直接在 Inspector 中编辑")]
        [SerializeField]
        private TimerConfig _config = new TimerConfig();

        private TimerEngine _engine;
        private Coroutine _tickCoroutine;

        /// <inheritdoc />
        public TimerState State => _engine != null ? _engine.State : TimerState.Stopped;

        /// <inheritdoc />
        public TimerMode Mode => _engine != null ? _engine.Mode : TimerMode.Countdown;

        /// <inheritdoc />
        public float Elapsed => _engine != null ? _engine.Elapsed : 0f;

        /// <inheritdoc />
        public float Remaining => _engine != null ? _engine.Remaining : 0f;

        /// <inheritdoc />
        public float Progress => _engine != null ? _engine.Progress : 0f;

        /// <inheritdoc />
        public bool IsRunning => _engine != null && _engine.IsRunning;

        /// <inheritdoc />
        public bool IsPaused => _engine != null && _engine.IsPaused;

        /// <inheritdoc />
        public event TimerCompleteHandler OnComplete;

        /// <inheritdoc />
        public event TimerUpdateHandler OnUpdate;

        /// <inheritdoc />
        public event TimerWarningHandler OnWarning;

        private void Awake()
        {
            _engine = new TimerEngine(_config);
        }

        /// <inheritdoc />
        public void StartCountdown(float duration)
        {
            StopDriver();
            _engine.BeginCountdown(duration);
            _tickCoroutine = StartCoroutine(TickRoutine());
        }

        /// <inheritdoc />
        public void StartStopwatch(float maxDuration = -1f)
        {
            StopDriver();
            _engine.BeginStopwatch(maxDuration);
            _tickCoroutine = StartCoroutine(TickRoutine());
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
        }

        /// <inheritdoc />
        public void Skip()
        {
            if (_engine != null && (_engine.IsRunning || _engine.IsPaused))
            {
                StopDriver();
                _engine.SkipToEnd();
                OnUpdate?.Invoke(_engine.Elapsed, _engine.Remaining);
                OnComplete?.Invoke();
            }
        }

        private IEnumerator TickRoutine()
        {
            while (_engine.IsRunning || _engine.IsPaused)
            {
                yield return null;

                if (!_engine.IsRunning) continue;

                float dt = _config.UseUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

                var result = _engine.Tick(dt);

                if (result.Ticked || result.WarningTriggered || result.Completed)
                {
                    OnUpdate?.Invoke(_engine.Elapsed, _engine.Remaining);
                }

                if (result.WarningTriggered)
                {
                    OnWarning?.Invoke(_engine.Remaining);
                }

                if (result.Completed)
                {
                    _tickCoroutine = null;
                    OnComplete?.Invoke();
                    yield break;
                }
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
