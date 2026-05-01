using System;

namespace CNoom.UnityGameTool.NumberRoller
{
    /// <summary>
    /// 数字滚动完成事件处理器
    /// </summary>
    public delegate void NumberRollerCompleteHandler();

    /// <summary>
    /// 数字滚动值更新事件处理器
    /// </summary>
    /// <param name="currentValue">当前显示值</param>
    /// <param name="formattedText">格式化后的文本</param>
    public delegate void NumberRollerUpdateHandler(double currentValue, string formattedText);

    /// <summary>
    /// 数字滚动接口，定义数字从起始值平滑过渡到目标值的核心行为契约。
    /// </summary>
    public interface INumberRoller
    {
        /// <summary>是否正在播放中</summary>
        bool IsPlaying { get; }

        /// <summary>当前显示值</summary>
        double CurrentValue { get; }

        /// <summary>目标值</summary>
        double TargetValue { get; }

        /// <summary>数字滚动完成时触发</summary>
        event NumberRollerCompleteHandler OnComplete;

        /// <summary>每帧数值更新时触发</summary>
        event NumberRollerUpdateHandler OnUpdate;

        /// <summary>
        /// 从当前值滚动到目标值。
        /// </summary>
        /// <param name="from">起始值</param>
        /// <param name="to">目标值</param>
        void Play(double from, double to);

        /// <summary>
        /// 从当前显示值继续滚动到新的目标值。
        /// </summary>
        /// <param name="to">新目标值</param>
        void Play(double to);

        /// <summary>
        /// 停止滚动，保持当前显示值。
        /// </summary>
        void Stop();

        /// <summary>
        /// 跳过动画，立即显示目标值。
        /// </summary>
        void Skip();
    }
}
