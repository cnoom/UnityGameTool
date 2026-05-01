using System;

namespace CNoom.UnityGameTool.Timer
{
    /// <summary>
    /// 计时器状态
    /// </summary>
    public enum TimerState
    {
        /// <summary>已停止/未开始</summary>
        Stopped,
        /// <summary>运行中</summary>
        Running,
        /// <summary>已暂停</summary>
        Paused,
        /// <summary>已完成（倒计时到0）</summary>
        Completed
    }

    /// <summary>
    /// 计时器模式
    /// </summary>
    public enum TimerMode
    {
        /// <summary>倒计时：从指定时间倒数到 0</summary>
        Countdown,
        /// <summary>正计时：从 0 开始累加</summary>
        Stopwatch
    }

    /// <summary>
    /// 计时器完成事件处理器（倒计时到 0 或手动停止时触发）
    /// </summary>
    public delegate void TimerCompleteHandler();

    /// <summary>
    /// 计时器每帧更新事件处理器
    /// </summary>
    /// <param name="elapsed">已用时间（秒）</param>
    /// <param name="remaining">剩余时间（秒，正计时模式下为负值）</param>
    public delegate void TimerUpdateHandler(float elapsed, float remaining);

    /// <summary>
    /// 警告阈值触发事件处理器
    /// </summary>
    /// <param name="remaining">触发时的剩余时间</param>
    public delegate void TimerWarningHandler(float remaining);

    /// <summary>
    /// 游戏计时器接口，定义倒计时/正计时的核心行为契约。
    /// </summary>
    public interface ITimer
    {
        /// <summary>当前状态</summary>
        TimerState State { get; }

        /// <summary>计时器模式</summary>
        TimerMode Mode { get; }

        /// <summary>已用时间（秒）</summary>
        float Elapsed { get; }

        /// <summary>剩余时间（秒，正计时模式下无意义）</summary>
        float Remaining { get; }

        /// <summary>归一化进度 0~1（倒计时：剩余比例；正计时：elapsed/duration）</summary>
        float Progress { get; }

        /// <summary>是否正在运行</summary>
        bool IsRunning { get; }

        /// <summary>是否已暂停</summary>
        bool IsPaused { get; }

        /// <summary>计时器完成时触发（倒计时到 0）</summary>
        event TimerCompleteHandler OnComplete;

        /// <summary>每帧更新时触发</summary>
        event TimerUpdateHandler OnUpdate;

        /// <summary>剩余时间到达警告阈值时触发一次</summary>
        event TimerWarningHandler OnWarning;

        /// <summary>
        /// 开始倒计时。
        /// </summary>
        /// <param name="duration">倒计时时长（秒）</param>
        void StartCountdown(float duration);

        /// <summary>
        /// 开始正计时。
        /// </summary>
        /// <param name="maxDuration">最大时长（秒），超过后自动完成。设为 -1 无限计时。</param>
        void StartStopwatch(float maxDuration = -1f);

        /// <summary>
        /// 暂停计时。
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复计时。
        /// </summary>
        void Resume();

        /// <summary>
        /// 停止计时并重置。
        /// </summary>
        void Stop();

        /// <summary>
        /// 跳到完成（倒计时置 0，正计时置最大）。
        /// </summary>
        void Skip();
    }
}
