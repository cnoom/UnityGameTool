using System;

namespace CNoom.UnityGameTool.Typewriter
{
    /// <summary>
    /// 打字机完成事件处理器
    /// </summary>
    public delegate void TypewriterCompleteHandler();

    /// <summary>
    /// 打字机字符显示事件处理器
    /// </summary>
    /// <param name="visibleIndex">可见字符索引</param>
    /// <param name="character">当前显示的字符</param>
    public delegate void TypewriterCharacterHandler(int visibleIndex, char character);

    /// <summary>
    /// 打字机接口，定义打字机效果的核心行为契约。
    /// </summary>
    public interface ITypewriter
    {
        /// <summary>是否正在播放中</summary>
        bool IsPlaying { get; }
        

        /// <summary>打字完成时触发</summary>
        event TypewriterCompleteHandler OnComplete;

        /// <summary>每显示一个可见字符时触发</summary>
        event TypewriterCharacterHandler OnCharacterTyped;

        /// <summary>
        /// 开始播放打字机效果。
        /// </summary>
        /// <param name="text">要逐字显示的文本</param>
        void Play(string text);

        /// <summary>
        /// 停止播放，保持当前显示状态。
        /// </summary>
        void Stop();

        /// <summary>
        /// 跳过动画，立即显示全部文本。
        /// </summary>
        void Skip();
    }
}
