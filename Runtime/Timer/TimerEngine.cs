using System;

namespace CNoom.UnityGameTool.Timer
{
    /// <summary>
    /// 游戏计时器纯逻辑引擎。负责倒计时/正计时的推进、暂停恢复、警告检测，
    /// 不依赖任何 Unity 组件，完全可单元测试。
    /// </summary>
    public class TimerEngine
    {
        private readonly TimerConfig _config;

        private TimerMode _mode;
        private TimerState _state;
        private float _totalDuration;
        private float _elapsed;
        private bool _warningFired;

        /// <summary>当前状态</summary>
        public TimerState State => _state;

        /// <summary>计时器模式</summary>
        public TimerMode Mode => _mode;

        /// <summary>已用时间（秒）</summary>
        public float Elapsed => _elapsed;

        /// <summary>剩余时间（秒）</summary>
        public float Remaining => _mode == TimerMode.Countdown
            ? Math.Max(0f, _totalDuration - _elapsed)
            : -1f;

        /// <summary>总时长</summary>
        public float TotalDuration => _totalDuration;

        /// <summary>
        /// 归一化进度 0~1。
        /// <para>倒计时：elapsed / duration</para>
        /// <para>正计时：有上限时 elapsed / maxDuration，无上限时始终 0</para>
        /// </summary>
        public float Progress
        {
            get
            {
                if (_totalDuration <= 0f) return 0f;
                return Math.Min(1f, _elapsed / _totalDuration);
            }
        }

        /// <summary>是否正在运行</summary>
        public bool IsRunning => _state == TimerState.Running;

        /// <summary>是否已暂停</summary>
        public bool IsPaused => _state == TimerState.Paused;

        /// <summary>
        /// 创建计时器引擎。
        /// </summary>
        /// <param name="config">计时器配置，不可为 null</param>
        public TimerEngine(TimerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _state = TimerState.Stopped;
        }

        /// <summary>
        /// 开始倒计时。
        /// </summary>
        /// <param name="duration">倒计时时长（秒）</param>
        public void BeginCountdown(float duration)
        {
            if (duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), "倒计时时长必须大于 0");

            _mode = TimerMode.Countdown;
            _totalDuration = duration;
            _elapsed = 0f;
            _warningFired = false;
            _state = TimerState.Running;
        }

        /// <summary>
        /// 开始正计时。
        /// </summary>
        /// <param name="maxDuration">最大时长（秒），-1 表示无限</param>
        public void BeginStopwatch(float maxDuration = -1f)
        {
            _mode = TimerMode.Stopwatch;
            _totalDuration = maxDuration;
            _elapsed = 0f;
            _warningFired = false;
            _state = TimerState.Running;
        }

        /// <summary>
        /// 推进一帧。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒），Driver 层应根据配置决定传入 deltaTime 或 unscaledDeltaTime</param>
        /// <returns>当前 TickResult，指示事件触发情况</returns>
        public TickResult Tick(float deltaTime)
        {
            if (_state != TimerState.Running) return default;

            _elapsed += deltaTime * _config.TimeScale;

            var result = new TickResult();

            // 检查警告阈值（仅倒计时）
            if (_mode == TimerMode.Countdown && _config.EnableWarning && !_warningFired)
            {
                if (Remaining <= _config.WarningThreshold)
                {
                    _warningFired = true;
                    result.WarningTriggered = true;
                }
            }

            // 检查完成
            if (_mode == TimerMode.Countdown && _elapsed >= _totalDuration)
            {
                _elapsed = _totalDuration;
                _state = TimerState.Completed;
                result.Completed = true;
                return result;
            }

            if (_mode == TimerMode.Stopwatch && _totalDuration > 0f && _elapsed >= _totalDuration)
            {
                _elapsed = _totalDuration;
                _state = TimerState.Completed;
                result.Completed = true;
                return result;
            }

            result.Ticked = true;
            return result;
        }

        /// <summary>
        /// 暂停计时。
        /// </summary>
        public void Pause()
        {
            if (_state == TimerState.Running)
            {
                _state = TimerState.Paused;
            }
        }

        /// <summary>
        /// 恢复计时。
        /// </summary>
        public void Resume()
        {
            if (_state == TimerState.Paused)
            {
                _state = TimerState.Running;
            }
        }

        /// <summary>
        /// 停止并重置。
        /// </summary>
        public void Stop()
        {
            _state = TimerState.Stopped;
            _elapsed = 0f;
            _warningFired = false;
        }

        /// <summary>
        /// 跳到完成状态。
        /// </summary>
        public void SkipToEnd()
        {
            if (_mode == TimerMode.Countdown)
            {
                _elapsed = _totalDuration;
            }
            else if (_mode == TimerMode.Stopwatch && _totalDuration > 0f)
            {
                _elapsed = _totalDuration;
            }

            _state = TimerState.Completed;
        }

        /// <summary>
        /// 完全重置引擎。
        /// </summary>
        public void Reset()
        {
            _state = TimerState.Stopped;
            _mode = TimerMode.Countdown;
            _totalDuration = 0f;
            _elapsed = 0f;
            _warningFired = false;
        }
    }

    /// <summary>
    /// Tick 一次返回的结果，指示哪些事件应触发。
    /// </summary>
    public struct TickResult
    {
        /// <summary>正常推进了一帧</summary>
        public bool Ticked;

        /// <summary>倒计时到达警告阈值</summary>
        public bool WarningTriggered;

        /// <summary>计时完成</summary>
        public bool Completed;
    }
}
