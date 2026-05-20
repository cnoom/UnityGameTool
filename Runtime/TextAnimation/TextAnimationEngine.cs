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
        // 伪随机 hash 系数
        private const float HashSeedFactor = 127.1f;
        private const float HashFrameFactor = 311.7f;
        private const float HashScale = 43758.5453f;

        // Shake 种子系数
        private const float ShakeFactorX = 1.37f;
        private const float ShakeOffsetX = 0.5f;
        private const float ShakeFactorY = 2.73f;
        private const float ShakeOffsetY = 1.3f;

        // Bounce 缩放因子
        private const float BounceAmplitudeDivisor = 50f;
        private readonly TextAnimationConfig _config;

        // 状态
        private int _charCount;
        private float _elapsed;
        private bool _isPlaying;

        // 过渡淡出状态
        private bool _isFadingOut;
        private float _fadeOutElapsed;

        // 每字符动画数据（预分配，避免 GC）
        private CharAnimationData[] _charData = Array.Empty<CharAnimationData>();

        // Shake 模式的每轴随机种子（固定种子保证帧间一致性）
        private float[] _shakeSeedX = Array.Empty<float>();
        private float[] _shakeSeedY = Array.Empty<float>();
        private int _shakeFrameIndex;

        /// <summary>是否正在播放（含过渡阶段）</summary>
        public bool IsPlaying => _isPlaying || _isFadingOut;

        /// <summary>是否处于过渡淡出阶段</summary>
        public bool IsFadingOut => _isFadingOut;

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
        /// 更新可见字符数量，不重置动画时间轴。
        /// 仅扩展新增字符的数据，保持已有字符的动画连续性。
        /// </summary>
        /// <param name="charCount">新的可见字符总数</param>
        public void UpdateCharCount(int charCount)
        {
            if (charCount <= 0) return;

            int oldCount = _charCount;
            _charCount = charCount;
            EnsureCapacity(charCount);

            // 仅初始化新增字符，保留已有字符的动画状态
            for (int i = oldCount; i < charCount; i++)
            {
                _charData[i] = new CharAnimationData
                {
                    XOffset = 0f, YOffset = 0f, Scale = 1f, Alpha = 1f
                };
            }

            // Shake 模式需要为新增字符生成种子
            if (_config.Type == TextAnimationType.Shake)
            {
                for (int i = oldCount; i < charCount; i++)
                {
                    _shakeSeedX[i] = i * ShakeFactorX + ShakeOffsetX;
                    _shakeSeedY[i] = i * ShakeFactorY + ShakeOffsetY;
                }
            }

            // 确保处于播放状态
            if (!_isPlaying && !_isFadingOut)
            {
                _isPlaying = true;
            }
        }

        /// <summary>
        /// 获取指定字符的动画数据。
        /// </summary>
        /// <param name="index">字符索引</param>
        /// <returns>该字符的动画偏移/缩放/透明度数据</returns>
        public ref readonly CharAnimationData GetCharData(int index)
        {
            if (index < 0 || index >= _charCount)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"索引 {index} 超出范围 [0, {_charCount})");
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
            _isFadingOut = false;
            _fadeOutElapsed = 0f;

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
            if (_charCount == 0) return false;

            // Once 模式使用独立逻辑
            if (_config.PlayMode == TextAnimationPlayMode.Once)
            {
                return TickOnce(deltaTime);
            }

            // Continuous 模式（原有逻辑）

            // 过渡淡出阶段
            if (_isFadingOut)
            {
                _fadeOutElapsed += deltaTime;
                float fadeDur = _config.FadeOutDuration;

                if (fadeDur <= 0f || _fadeOutElapsed >= fadeDur)
                {
                    _isFadingOut = false;
                    ResetCharData();
                    return false;
                }

                ComputeAnimation();
                ApplyFadeOut(_fadeOutElapsed / fadeDur);
                return true;
            }

            if (!_isPlaying) return false;

            _elapsed += deltaTime * _config.Speed;

            // 非循环模式下检查是否需要进入过渡
            if (!_config.IsLooping && _elapsed >= _config.Duration)
            {
                _isPlaying = false;

                if (_config.FadeOutDuration > 0f)
                {
                    _isFadingOut = true;
                    _fadeOutElapsed = 0f;
                    ComputeAnimation();
                    return true;
                }

                ResetCharData();
                return false;
            }

            ComputeAnimation();
            return _isPlaying;
        }

        /// <summary>
        /// Once 模式逐帧更新。每个字符有独立生命周期：未激活→动画中→已完成。
        /// </summary>
        private bool TickOnce(float deltaTime)
        {
            if (!_isPlaying) return false;

            _elapsed += deltaTime * _config.Speed;
            float charDur = _config.Duration;
            bool anyActive = false;

            for (int i = 0; i < _charCount; i++)
            {
                float charStart = i * _config.CharDelay;
                float charTime = _elapsed - charStart;

                if (charTime < 0f)
                {
                    // 未激活：不可见
                    _charData[i] = new CharAnimationData
                    {
                        XOffset = 0f, YOffset = 0f, Scale = 1f, Alpha = 0f
                    };
                    anyActive = true;
                }
                else if (charDur > 0f && charTime < charDur)
                {
                    // 动画中：计算动画值并应用衰减包络
                    ComputeCharOnce(i, charTime, charDur);
                    anyActive = true;
                }
                else
                {
                    // 已完成：归位
                    _charData[i] = new CharAnimationData
                    {
                        XOffset = 0f, YOffset = 0f, Scale = 1f, Alpha = 1f
                    };
                }
            }

            if (!anyActive)
            {
                _isPlaying = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Once 模式下计算单个字符的动画数据，应用衰减包络使动画平滑归零。
        /// </summary>
        private void ComputeCharOnce(int index, float charTime, float charDur)
        {
            // 衰减包络：从 1 线性衰减到 0
            float envelope = Math.Max(0f, 1f - charTime / charDur);

            // Fade 类型特殊处理：直接用 charTime 渐显
            if (_config.Type == TextAnimationType.Fade)
            {
                float fadeDur = _config.FadeDuration;
                float alpha = fadeDur > 0f ? Math.Min(1f, charTime / fadeDur) : 1f;
                _charData[index] = new CharAnimationData
                {
                    XOffset = 0f, YOffset = 0f, Scale = 1f, Alpha = alpha
                };
                return;
            }

            // 使用正常的动画计算，然后乘以包络
            float amp = _config.Amplitude;

            switch (_config.Type)
            {
                case TextAnimationType.Wave:
                {
                    float freq = _config.Frequency;
                    float offset = (float)Math.Sin(charTime * freq * Math.PI * 2f) * amp * envelope;
                    _charData[index] = new CharAnimationData
                    {
                        XOffset = 0f, YOffset = offset, Scale = 1f, Alpha = 1f
                    };
                    break;
                }
                case TextAnimationType.Shake:
                {
                    _shakeFrameIndex++;
                    float rx = PseudoRandom(_shakeSeedX[index], _shakeFrameIndex);
                    float ry = PseudoRandom(_shakeSeedY[index], _shakeFrameIndex);
                    _charData[index] = new CharAnimationData
                    {
                        XOffset = rx * amp * envelope,
                        YOffset = ry * amp * envelope,
                        Scale = 1f,
                        Alpha = 1f
                    };
                    break;
                }
                case TextAnimationType.Bounce:
                {
                    float freq = _config.Frequency;
                    float cycle = (float)Math.Abs(Math.Sin(charTime * freq * Math.PI));
                    float scale = 1f + cycle * (amp / BounceAmplitudeDivisor) * envelope;
                    _charData[index] = new CharAnimationData
                    {
                        XOffset = 0f, YOffset = 0f, Scale = scale, Alpha = 1f
                    };
                    break;
                }
            }
        }

        /// <summary>
        /// 根据当前类型计算动画数据。
        /// </summary>
        private void ComputeAnimation()
        {
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
        }

        /// <summary>
        /// 将过渡淡出系数应用到所有字符的动画数据上。
        /// </summary>
        /// <param name="progress">淡出进度 0~1（0=动画数据原值，1=全部归零）</param>
        private void ApplyFadeOut(float progress)
        {
            float factor = 1f - progress;
            for (int i = 0; i < _charCount; i++)
            {
                _charData[i].XOffset *= factor;
                _charData[i].YOffset *= factor;
                _charData[i].Scale = 1f + (_charData[i].Scale - 1f) * factor;
                _charData[i].Alpha = 1f - (1f - _charData[i].Alpha) * factor;
            }
        }

        /// <summary>
        /// 停止播放，保持当前状态。
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _isFadingOut = false;
        }

        /// <summary>
        /// 跳到动画结束，重置所有数据。
        /// </summary>
        public void SkipToEnd()
        {
            _isPlaying = false;
            _isFadingOut = false;
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
            _isFadingOut = false;
            _fadeOutElapsed = 0f;
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
            float x = (float)(Math.Sin(seed * HashSeedFactor + frame * HashFrameFactor) * HashScale);
            // 取小数部分映射到 [0, 1]，再转换到 [-1, 1]
            float frac = x - (float)Math.Floor(x);
            return frac * 2f - 1f;
        }

        private void GenerateShakeSeeds(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // 使用字符索引作为基础种子
                _shakeSeedX[i] = i * ShakeFactorX + ShakeOffsetX;
                _shakeSeedY[i] = i * ShakeFactorY + ShakeOffsetY;
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

                float scale = 1f + cycle * (amp / BounceAmplitudeDivisor) * decay;

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

                _charData[i].XOffset = 0f;
                _charData[i].YOffset = 0f;
                _charData[i].Scale = 1f;
                _charData[i].Alpha = alpha;
            }
        }

        #endregion
    }
}
