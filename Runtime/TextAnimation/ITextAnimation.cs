using System;

namespace CNoom.UnityGameTool.TextAnimation
{
    /// <summary>
    /// 文字动画完成事件处理器
    /// </summary>
    public delegate void TextAnimationCompleteHandler();

    /// <summary>
    /// 文字动画接口，定义逐字动画效果的核心行为契约。
    /// </summary>
    public interface ITextAnimation
    {
        /// <summary>是否正在播放中</summary>
        bool IsPlaying { get; }

        /// <summary>动画完成时触发（仅非循环模式）</summary>
        event TextAnimationCompleteHandler OnComplete;

        /// <summary>
        /// 开始播放文字动画。
        /// </summary>
        /// <param name="visibleCharacterCount">当前可见字符总数</param>
        void Play(int visibleCharacterCount);

        /// <summary>
        /// 停止播放并恢复原始顶点状态。
        /// </summary>
        void Stop();

        /// <summary>
        /// 跳过动画，恢复原始状态。
        /// </summary>
        void Skip();

        /// <summary>
        /// 当文本内容变化时由外部调用，更新可见字符数。
        /// 用于打字机逐字显示时同步更新动画范围。
        /// </summary>
        /// <param name="visibleCharacterCount">当前可见字符总数</param>
        void UpdateVisibleCount(int visibleCharacterCount);
    }
}
