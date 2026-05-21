using System;
using CNoom.UnityGameTool.TextAnimation;

namespace CNoom.UnityGameTool.Typewriter
{
    /// <summary>
    /// 打字动画纯逻辑引擎。内部组合 TypewriterEngine（逐字节奏）和
    /// TextAnimationEngine（逐字动画），对外提供统一的帧数据接口。
    /// 不依赖任何 Unity 组件，完全可单元测试。
    /// </summary>
    public class TypewriterAnimatorEngine
    {
        private readonly TypewriterEngine _typewriterEngine;
        private readonly TextAnimationEngine _animationEngine;

        /// <summary>是否正在打字（逐字显示阶段）</summary>
        public bool IsTyping => _typewriterEngine.IsPlaying;

        /// <summary>是否仍在动画中（打字阶段 + 动画尾段）</summary>
        public bool IsPlaying =>
            _typewriterEngine.IsPlaying || _animationEngine.IsPlaying;

        /// <summary>当前已揭示的可见字符索引</summary>
        public int CurrentIndex => _typewriterEngine.CurrentIndex;

        /// <summary>总可见字符数</summary>
        public int TotalCharacters => _typewriterEngine.TotalCharacters;

        /// <summary>当前动画管理的字符总数</summary>
        public int CharCount => _animationEngine.CharCount;

        /// <summary>
        /// 创建打字动画引擎实例。
        /// </summary>
        /// <param name="config">打字动画配置，不可为 null</param>
        public TypewriterAnimatorEngine(TypewriterAnimatorConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _typewriterEngine = new TypewriterEngine(config.ToTypewriterConfig());
            _animationEngine = new TextAnimationEngine(config.ToAnimationConfig());
        }

        /// <summary>
        /// 开始新的打字动画会话。
        /// </summary>
        /// <param name="totalCharacters">文本的可见字符总数</param>
        public void Begin(int totalCharacters)
        {
            _typewriterEngine.Begin(totalCharacters);
            // 动画引擎仅启动第一个字符，后续由 RevealChar 逐步扩展
            if (totalCharacters > 0)
            {
                _animationEngine.Begin(1);
            }
        }

        /// <summary>
        /// 推进一个可见字符。返回是否还有更多字符和当前字符的延迟。
        /// 调用后应通过 UpdateCharCount 同步动画引擎。
        /// </summary>
        /// <param name="currentChar">当前要显示的字符，用于计算延迟</param>
        /// <returns>hasMore: 是否还有更多字符；delay: 延迟秒数</returns>
        public (bool hasMore, float delay) Advance(char currentChar)
        {
            return _typewriterEngine.Advance(currentChar);
        }

        /// <summary>
        /// 同步动画引擎的可见字符数。在每次 Advance 后调用。
        /// 不重置动画时间轴，保持已有字符的动画连续性。
        /// </summary>
        /// <param name="visibleCount">当前可见字符总数</param>
        public void UpdateCharCount(int visibleCount)
        {
            _animationEngine.UpdateCharCount(visibleCount);
        }

        /// <summary>
        /// 推进一帧动画。在打字阶段和动画尾段都应持续调用。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间（秒）</param>
        /// <returns>是否仍在播放中</returns>
        public bool Tick(float deltaTime)
        {
            return _animationEngine.Tick(deltaTime);
        }

        /// <summary>
        /// 获取指定字符的动画数据。
        /// </summary>
        /// <param name="index">字符索引</param>
        /// <returns>该字符的动画偏移/缩放/透明度数据</returns>
        public ref readonly CharAnimationData GetCharData(int index)
        {
            return ref _animationEngine.GetCharData(index);
        }

        /// <summary>
        /// 跳到文本末尾，停止所有动画。
        /// </summary>
        public void SkipToEnd()
        {
            _typewriterEngine.SkipToEnd();
            _animationEngine.SkipToEnd();
        }

        /// <summary>
        /// 停止播放，保持当前状态。
        /// </summary>
        public void Stop()
        {
            _typewriterEngine.Stop();
            _animationEngine.Stop();
        }

        /// <summary>
        /// 重置引擎状态到初始。
        /// </summary>
        public void Reset()
        {
            _typewriterEngine.Reset();
            _animationEngine.Reset();
        }
    }
}
