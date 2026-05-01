using System;

namespace CNoom.UnityGameTool.TextAnimation
{
    /// <summary>
    /// 单个字符的动画计算结果，由 Engine 每帧产出，供 Driver 应用到 TMP 顶点。
    /// </summary>
    public struct CharAnimationData
    {
        /// <summary>X 轴偏移量（像素）</summary>
        public float XOffset;

        /// <summary>Y 轴偏移量（像素）</summary>
        public float YOffset;

        /// <summary>缩放系数（1.0 = 原始大小）</summary>
        public float Scale;

        /// <summary>Alpha 值（0~1，1 = 完全不透明）</summary>
        public float Alpha;
    }

    /// <summary>
    /// 文字动画纯逻辑引擎。负责逐帧计算每个字符的位移、缩放、透明度，
    /// 不依赖任何 Unity 组件，完全可单元测试。
    /// </summary>
    public class TextAnimationEngine
    {
        private readonly TextAnimationConfig _config;

        // 状态
        private int _charCount;
        private float _elapsed;
        private bool _isPlaying;

        // 每字符动画数据（预分配，避免 GC）
        private CharAnimationData[] _charData = Array.Empty<CharAnimationData>();

        // Shake 模式的每轴随机种子（固定种子保证帧间一致性）
        private float[] _shakeSeedX = Array.Empty<float>();
        private float[] _shakeSeedY = Array.Empty<float>();
        private int _shakeFrameIndex;

        /// <summary>是否正在播放</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>已播放时间</summary>
        public float Elapsed => _elapsed;

        /// <summary>当前字符总数</summary>
        public int CharCount => _charCount;

        /// <summary>
        /// 创建文字动画引擎实例。
        /// </summary>
        /// <param name="config">动画配置，不可为 null</param>
        public TextAnimationEngine(TextAnimationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 获取指定字符的动画数据。
        /// </summary>
        /// <param name="index">字符索引</param>
        /// <returns>该字符的动画偏移/缩放/透明度数据</returns>
        public ref readonly CharAnimationData GetCharData(int index)
        {
            return ref _charData[index];
        }

        /// <summary>
        /// 开始播放动画。
        /// </summary>
        /// <param name="charCount">可见字符总数</param>
        public void Begin(int charCount)
        {
            _charCount = charCount;
            _elapsed = 0f;
            _shakeFrameIndex = 0;
            _isPlaying = charCount > 0;

            EnsureCapacity(charCount);

            // 初始化每字符数据
            for (int i = 0; i < charCount; i++)
            {
                _charData[i] = new CharAnimationData
                {
                    XOffset = 0f,
                    YOffset = 0f,
                    Scale = 1f,
                    Alpha = 1f
                };
            }

            // Shake 模式预生成随机种子
            if (_config.Type == TextAnimationType.Shake)
            {
                GenerateShakeSeeds(charCount);
            }
        }

        /// <summary>
        /// 推进一帧，计算所有字符的动画数据。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间（秒）</param>
        /// <returns>是否仍在播放中</returns>
        public bool Tick(float deltaTime)
        {
            if (!_isPlaying || _charCount == 0) return false;

            _elapsed += deltaTime * _config.Speed;

            // 非循环模式下检查是否完成
            if (!_config.IsLooping && _elapsed >= _config.Duration)
            {
                _isPlaying = false;
                ResetCharData();
                return false;
            }

            switch (_config.Type)
            {
                case TextAnimationType.Wave:
                    ComputeWave();
                    break;
                case TextAnimationType.Shake:
                    ComputeShake();
                    break;
                case TextAnimationType.Bounce:
                    ComputeBounce();
                    break;
                case TextAnimationType.Fade:
                    ComputeFade();
                    break;
            }

            return _isPlaying;
        }

        /// <summary>
        /// 停止播放，保持当前状态。
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
        }

        /// <summary>
        /// 跳到动画结束，重置所有数据。
        /// </summary>
        public void SkipToEnd()
        {
            _isPlaying = false;
            ResetCharData();
        }

        /// <summary>
        /// 重置引擎状态到初始。
        /// </summary>
        public void Reset()
        {
            _charCount = 0;
            _elapsed = 0f;
            _isPlaying = false;
            _shakeFrameIndex = 0;
        }

        #region 内部计算

        private void EnsureCapacity(int count)
        {
            if (_charData.Length < count)
            {
                _charData = new CharAnimationData[count];
                _shakeSeedX = new float[count];
                _shakeSeedY = new float[count];
            }
        }

        private void ResetCharData()
        {
            for (int i = 0; i < _charCount; i++)
            {
                _charData[i] = new CharAnimationData
                {
                    XOffset = 0f,
                    YOffset = 0f,
                    Scale = 1f,
                    Alpha = 1f
                };
            }
        }

        /// <summary>
        /// 获取指定字符的局部时间（扣除字符间延迟）。
        /// </summary>
        private float GetCharTime(int index)
        {
            return Math.Max(0f, _elapsed - index * _config.CharDelay);
        }

        /// <summary>
        /// 简单伪随机，基于种子和帧索引。返回 -1 ~ 1。
        /// </summary>
        private static float PseudoRandom(float seed, int frame)
        {
            // 简单的 hash 组合，产生看起来随机的值
            float x = (float)(Math.Sin(seed * 127.1f + frame * 311.7f) * 43758.5453);
            return x - (float)Math.Floor(x) * 2f - 1f;
        }

        private void GenerateShakeSeeds(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // 使用字符索引作为基础种子
                _shakeSeedX[i] = i * 1.37f + 0.5f;
                _shakeSeedY[i] = i * 2.73f + 1.3f;
            }
        }

        private void ComputeWave()
        {
            float amp = _config.Amplitude;
            float freq = _config.Frequency;

            for (int i = 0; i < _charCount; i++)
            {
                float t = GetCharTime(i);
                float offset = (float)Math.Sin(t * freq * Math.PI * 2f) * amp;
                _charData[i].XOffset = 0f;
                _charData[i].YOffset = offset;
                _charData[i].Scale = 1f;
                _charData[i].Alpha = 1f;
            }
        }

        private void ComputeShake()
        {
            float amp = _config.Amplitude;
            _shakeFrameIndex++;

            for (int i = 0; i < _charCount; i++)
            {
                float rx = PseudoRandom(_shakeSeedX[i], _shakeFrameIndex);
                float ry = PseudoRandom(_shakeSeedY[i], _shakeFrameIndex);

                // 使用平滑的 noise 而不是纯随机，减少抖动感
                float t = GetCharTime(i);
                float decay = _config.IsLooping ? 1f : Math.Max(0f, 1f - t / Math.Max(0.001f, _config.Duration));

                _charData[i].XOffset = rx * amp * decay;
                _charData[i].YOffset = ry * amp * decay;
                _charData[i].Scale = 1f;
                _charData[i].Alpha = 1f;
            }
        }

        private void ComputeBounce()
        {
            float amp = _config.Amplitude;
            float freq = _config.Frequency;

            for (int i = 0; i < _charCount; i++)
            {
                float t = GetCharTime(i);

                // 使用 abs(sin) 产生弹跳节奏，幅度从大到小衰减
                float cycle = (float)Math.Abs(Math.Sin(t * freq * Math.PI));
                float decay = _config.IsLooping ? 1f : Math.Max(0f, 1f - t / Math.Max(0.001f, _config.Duration));

                float scale = 1f + cycle * (amp / 50f) * decay;

                _charData[i].XOffset = 0f;
                _charData[i].YOffset = 0f;
                _charData[i].Scale = scale;
                _charData[i].Alpha = 1f;
            }
        }

        private void ComputeFade()
        {
            float fadeDur = _config.FadeDuration;

            for (int i = 0; i < _charCount; i++)
            {
                float t = GetCharTime(i);

                // 每个字符从 0 渐显到 1
                float alpha = fadeDur > 0f ? Math.Min(1f, t / fadeDur) : 1f;

                // 非循环模式：全部显示完后保持
                if (!_config.IsLooping)
                {
                    float totalDuration = _config.Duration;
                    if (totalDuration > 0f && _elapsed >= totalDuration)
                    {
                        alpha = 1f;
                    }
                }

                _charData[i].XOffset = 0f;
                _charData[i].YOffset = 0f;
                _charData[i].Scale = 1f;
                _charData[i].Alpha = alpha;
            }
        }

        #endregion
    }
}
