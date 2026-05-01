// asmdef Version Defines，安装 UniTask 包后自动启用。

#if UNITASK_SUPPORT

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CNoom.UnityGameTool.Timer
{
    /// <summary>
    /// 基于 UniTask 的游戏计时器组件。支持异步等待完成和 CancellationToken 取消。
    /// 需要安装 UniTask 包后自动编译。
    /// </summary>
    public class TimerUniTask : MonoBehaviour, ITimer
    {
        [Header("计时器配置")]
        [Tooltip("计时器配置，可直接在 Inspector 中编辑")]
        [SerializeField]
        private TimerConfig _config = new TimerConfig();

        private TimerEngine _engine;
        private CancellationTokenSource _tickCts;

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
            StartCountdownAsync(duration).Forget();
        }

        /// <inheritdoc />
        public void StartStopwatch(float maxDuration = -1f)
        {
            StartStopwatchAsync(maxDuration).Forget();
        }

        /// <summary>
        /// 异步开始倒计时，支持 await 等待完成。
        /// </summary>
        /// <param name="duration">倒计时时长（秒）</param>
        public async UniTask StartCountdownAsync(float duration)
        {
            CancelTick();
            _tickCts = new CancellationTokenSource();
            var token = _tickCts.Token;

            try
            {
                _engine.BeginCountdown(duration);
                await TickLoop(token);
            }
            catch (OperationCanceledException)
            {
                // 由 Stop/Skip/Destroy 触发
            }
        }

        /// <summary>
        /// 异步开始正计时，支持 await 等待完成。
        /// 无限计时模式下不会自然完成，需外部 Stop/Skip。
        /// </summary>
        /// <param name="maxDuration">最大时长（秒），-1 表示无限</param>
        public async UniTask StartStopwatchAsync(float maxDuration = -1f)
        {
            CancelTick();
            _tickCts = new CancellationTokenSource();
            var token = _tickCts.Token;

            try
            {
                _engine.BeginStopwatch(maxDuration);
                await TickLoop(token);
            }
            catch (OperationCanceledException)
            {
                // 由 Stop/Skip/Destroy 触发
            }
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
            CancelTick();
            _engine?.Stop();
        }

        /// <inheritdoc />
        public void Skip()
        {
            if (_engine != null && (_engine.IsRunning || _engine.IsPaused))
            {
                CancelTick();
                _engine.SkipToEnd();
                OnUpdate?.Invoke(_engine.Elapsed, _engine.Remaining);
                OnComplete?.Invoke();
            }
        }

        private async UniTask TickLoop(CancellationToken token)
        {
            while (_engine.IsRunning || _engine.IsPaused)
            {
                await UniTask.Yield(token);

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
                    OnComplete?.Invoke();
                    return;
                }
            }
        }

        private void CancelTick()
        {
            if (_tickCts != null)
            {
                _tickCts.Cancel();
                _tickCts.Dispose();
                _tickCts = null;
            }
        }

        private void OnDestroy()
        {
            CancelTick();
        }
    }
}

#endif
