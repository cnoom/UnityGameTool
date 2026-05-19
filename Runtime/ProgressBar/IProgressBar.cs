using System;

namespace CNoom.UnityGameTool.ProgressBar
{
    /// <summary>
    /// 进度条值变更事件处理器
    /// </summary>
    /// <param name="normalizedValue">归一化进度值 0~1</param>
    public delegate void ProgressBarUpdateHandler(float normalizedValue);

    /// <summary>
    /// 进度条完成事件处理器（进度到达 1 时触发）
    /// </summary>
    public delegate void ProgressBarCompleteHandler();

    /// <summary>
    /// 进度条接口，定义进度平滑过渡和延迟扣减条的核心行为契约。
    /// </summary>
    public interface IProgressBar
    {
        /// <summary>当前归一化进度值 0~1</summary>
        float Value { get; }

        /// <summary>延迟条归一化进度值 0~1（扣减时先显示延迟条，再平滑追赶）</summary>
        float DelayedValue { get; }

        /// <summary>是否正在过渡中</summary>
        bool IsTransitioning { get; }

        /// <summary>进度值变更时触发</summary>
        event ProgressBarUpdateHandler OnValueChanged;

        /// <summary>进度到达 1（满）时触发</summary>
        event ProgressBarCompleteHandler OnComplete;

        /// <summary>
        /// 设置进度值（0~1），立即跳转无动画。
        /// </summary>
        /// <param name="value">归一化进度值</param>
        void Set(float value);

        /// <summary>
        /// 平滑过渡到目标进度值。
        /// </summary>
        /// <param name="targetValue">目标归一化进度值 0~1</param>
        void TransitionTo(float targetValue);

        /// <summary>
        /// 增加进度值（平滑过渡）。
        /// </summary>
        /// <param name="delta">增量（归一化值）</param>
        void Add(float delta);

        /// <summary>
        /// 减少进度值（带延迟条效果）。
        /// </summary>
        /// <param name="delta">减量（归一化值）</param>
        void Subtract(float delta);

        /// <summary>
        /// 立即停止过渡动画，保持当前值。
        /// </summary>
        void Stop();

        /// <summary>
        /// 重置进度条到 0。
        /// </summary>
        void Reset();
    }
}
