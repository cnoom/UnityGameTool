using System;

namespace CNoom.UnityGameTool.Pulse
{
    /// <summary>
    /// 脉冲每帧计算结果，由 Engine 产出，供 Driver 应用到 UI。
    /// </summary>
    public struct PulseFrameData
    {
        /// <summary>缩放值</summary>
        public float Scale;

        /// <summary>Alpha 值</summary>
        public float Alpha;

        /// <summary>Y 轴偏移量</summary>
        public float YOffset;

        /// <summary>是否播放完成（非循环模式）</summary>
        public bool Completed;
    }

    /// <summary>
    /// 脉冲/呼吸效果纯逻辑引擎。负责周期性动画的插值计算，
    /// 不依赖任何 Unity 组件，完全可单元测试。
    /// </summary>
    public class PulseEngine
    {
        private PulseConfig _config;

        private float _elapsed;
        private bool _isPlaying;
        private bool _isPaused;

        /// <summary>是否正在播放</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>是否已暂停</summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// 创建脉冲引擎。
        /// </summary>
        public PulseEngine()
        {
            _config = new PulseConfig();
        }

        /// <summary>
        /// 使用指定配置开始播放。
        /// </summary>
        /// <param name="config">脉冲配置</param>
        public void Begin(PulseConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _elapsed = 0f;
            _isPlaying = true;
            _isPaused = false;
        }

        /// <summary>
        /// 推进一帧，计算当前脉冲值。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒）</param>
        /// <returns>本帧计算结果</returns>
        public PulseFrameData Tick(float deltaTime)
        {
            if (!_isPlaying || _isPaused)
            {
                return default;
            }

            _elapsed += deltaTime * _config.Speed;

            // 非循环模式检查是否完成
            if (!_config.IsLooping && _elapsed >= _config.Duration)
            {
                _isPlaying = false;
                return new PulseFrameData
                {
                    Scale = 1f,
                    Alpha = _config.MaxAlpha,
                    YOffset = 0f,
                    Completed = true
                };
            }

            // 计算周期进度 0~1
            float period = _config.Period;
            float phase = period > 0f ? (_elapsed % period) / period : 0f;

            // 应用缓动得到 0~1~0 的 pingpong 值
            float pingpong = ApplyEasePingPong(phase, _config.EaseType);

            // 根据类型计算输出值
            var frameData = new PulseFrameData
            {
                Completed = false
            };

            switch (_config.Type)
            {
                case PulseType.Scale:
                    frameData.Scale = Lerp(_config.MinScale, _config.MaxScale, pingpong);
                    frameData.Alpha = _config.MaxAlpha;
                    frameData.YOffset = 0f;
                    break;

                case PulseType.Glow:
                    frameData.Scale = 1f;
                    frameData.Alpha = Lerp(_config.MinAlpha, _config.MaxAlpha, pingpong);
                    frameData.YOffset = 0f;
                    break;

                case PulseType.Float:
                    frameData.Scale = 1f;
                    frameData.Alpha = _config.MaxAlpha;
                    // 从 -1~1 映射到 -amplitude~amplitude
                    float normalizedOffset = pingpong * 2f - 1f;
                    frameData.YOffset = normalizedOffset * _config.FloatAmplitude;
                    break;

                default:
                    frameData.Scale = 1f;
                    frameData.Alpha = 1f;
                    frameData.YOffset = 0f;
                    break;
            }

            return frameData;
        }

        /// <summary>
        /// 暂停动画。
        /// </summary>
        public void Pause()
        {
            if (_isPlaying)
            {
                _isPaused = true;
            }
        }

        /// <summary>
        /// 恢复动画。
        /// </summary>
        public void Resume()
        {
            if (_isPaused)
            {
                _isPaused = false;
            }
        }

        /// <summary>
        /// 停止动画。
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _isPaused = false;
        }

        /// <summary>
        /// 跳到动画结束。
        /// </summary>
        public void SkipToEnd()
        {
            _isPlaying = false;
            _isPaused = false;
        }

        /// <summary>
        /// 完全重置引擎。
        /// </summary>
        public void Reset()
        {
            _elapsed = 0f;
            _isPlaying = false;
            _isPaused = false;
        }

        #region 内部计算

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// 将 0~1 的线性相位转换为 0~1~0 的 pingpong 值，并应用缓动。
        /// </summary>
        private static float ApplyEasePingPong(float phase, PulseEaseType easeType)
        {
            // 转换为 0→1→0 的三角波
            float triangle = phase < 0.5f ? phase * 2f : 2f - phase * 2f;

            switch (easeType)
            {
                case PulseEaseType.Sine:
                    // 使用 sin 曲线使过渡更平滑
                    return (float)Math.Sin(triangle * Math.PI * 0.5);

                case PulseEaseType.Linear:
                    return triangle;

                case PulseEaseType.Exponential:
                    // 指数缓动
                    return triangle < 0.5f
                        ? (float)Math.Pow(triangle * 2f, 2) * 0.5f
                        : 1f - (float)Math.Pow((1f - triangle) * 2f, 2) * 0.5f;

                default:
                    return triangle;
            }
        }

        #endregion
    }
}
