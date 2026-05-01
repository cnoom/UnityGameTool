using System;

namespace CNoom.UnityGameTool.Typewriter
{
    /// <summary>
    /// 打字机纯逻辑引擎。负责进度管理和延迟计算，
    /// 不依赖任何 Unity 组件或异步方案，完全可单元测试。
    /// </summary>
    public class TypewriterEngine
    {
        private readonly TypewriterConfig _config;

        private int _totalVisibleCharacters;
        private int _currentVisibleIndex;
        private bool _isPlaying;

        /// <summary>是否正在播放中</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>当前可见字符索引（即将要显示的位置）</summary>
        public int CurrentIndex => _currentVisibleIndex;

        /// <summary>总可见字符数</summary>
        public int TotalCharacters => _totalVisibleCharacters;

        /// <summary>播放进度（0~1）</summary>
        public float Progress => _totalVisibleCharacters > 0
            ? (float)_currentVisibleIndex / _totalVisibleCharacters
            : 0f;

        /// <summary>
        /// 创建打字机引擎实例。
        /// </summary>
        /// <param name="config">打字机配置，不可为 null</param>
        public TypewriterEngine(TypewriterConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 开始新的播放会话。
        /// </summary>
        /// <param name="totalVisibleCharacters">文本的可见字符总数</param>
        public void Begin(int totalVisibleCharacters)
        {
            _totalVisibleCharacters = totalVisibleCharacters;
            _currentVisibleIndex = 0;
            _isPlaying = totalVisibleCharacters > 0;
        }

        /// <summary>
        /// 推进一个可见字符。
        /// </summary>
        /// <param name="currentChar">当前要显示的字符，用于计算延迟</param>
        /// <returns>
        /// hasMore: 是否还有更多字符需要显示；
        /// delay: 当前字符的显示延迟（秒）
        /// </returns>
        public (bool hasMore, float delay) Advance(char currentChar)
        {
            float delay = _config.GetDelay(currentChar);
            _currentVisibleIndex++;

            bool hasMore = _currentVisibleIndex < _totalVisibleCharacters;
            if (!hasMore)
            {
                _isPlaying = false;
            }

            return (hasMore, delay);
        }

        /// <summary>
        /// 跳到文本末尾，标记播放完成。
        /// </summary>
        public void SkipToEnd()
        {
            _currentVisibleIndex = _totalVisibleCharacters;
            _isPlaying = false;
        }

        /// <summary>
        /// 停止播放，保持当前进度不变。
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
        }

        /// <summary>
        /// 重置引擎状态到初始。
        /// </summary>
        public void Reset()
        {
            _totalVisibleCharacters = 0;
            _currentVisibleIndex = 0;
            _isPlaying = false;
        }
    }
}
