using System;

namespace CNoom.UnityGameTool.Pulse
{
    /// <summary>
    /// 脉冲动画完成事件处理器（非循环模式播放结束时触发）
    /// </summary>
    public delegate void PulseCompleteHandler();

    /// <summary>
    /// 脉冲/呼吸效果接口，定义周期性动画的核心行为契约。
    /// 支持 Scale、Glow、Float 三种动画类型。
    /// </summary>
    public interface IPulse
    {
        /// <summary>是否正在播放</summary>
        bool IsPlaying { get; }

        /// <summary>当前缩放值</summary>
        float CurrentScale { get; }

        /// <summary>当前 Alpha 值</summary>
        float CurrentAlpha { get; }

        /// <summary>当前 Y 轴偏移量</summary>
        float CurrentYOffset { get; }

        /// <summary>非循环模式播放完成时触发</summary>
        event PulseCompleteHandler OnComplete;

        /// <summary>
        /// 开始播放脉冲动画。
        /// </summary>
        void Play();

        /// <summary>
        /// 使用自定义配置开始播放。
        /// </summary>
        /// <param name="config">脉冲配置</param>
        void Play(PulseConfig config);

        /// <summary>
        /// 暂停动画。
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复动画。
        /// </summary>
        void Resume();

        /// <summary>
        /// 停止动画，重置到初始状态。
        /// </summary>
        void Stop();

        /// <summary>
        /// 跳到动画结束状态。
        /// </summary>
        void Skip();
    }
}
