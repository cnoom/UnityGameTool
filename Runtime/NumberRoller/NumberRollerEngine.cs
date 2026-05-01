using System;

namespace CNoom.UnityGameTool.NumberRoller
{
    /// <summary>
    /// 数字滚动纯逻辑引擎。负责插值计算、缓动曲线和格式化，
    /// 不依赖任何 Unity 组件，完全可单元测试。
    /// </summary>
    public class NumberRollerEngine
    {
        private readonly NumberRollerConfig _config;

        private double _fromValue;
        private double _toValue;
        private double _currentValue;
        private float _elapsed;
        private bool _isPlaying;

        /// <summary>是否正在播放</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>起始值</summary>
        public double FromValue => _fromValue;

        /// <summary>目标值</summary>
        public double ToValue => _toValue;

        /// <summary>当前插值</summary>
        public double CurrentValue => _currentValue;

        /// <summary>播放进度 0~1</summary>
        public float Progress => _config.Duration > 0f
            ? Math.Min(1f, _elapsed / _config.Duration)
            : 1f;

        /// <summary>
        /// 创建数字滚动引擎。
        /// </summary>
        /// <param name="config">滚动配置，不可为 null</param>
        public NumberRollerEngine(NumberRollerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 从指定值开始滚动到目标值。
        /// </summary>
        /// <param name="from">起始值</param>
        /// <param name="to">目标值</param>
        public void Begin(double from, double to)
        {
            _fromValue = from;
            _toValue = to;
            _currentValue = from;
            _elapsed = 0f;

            double diff = Math.Abs(to - from);
            _isPlaying = diff > _config.SnapThreshold;
        }

        /// <summary>
        /// 推进一帧，计算当前插值。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒）</param>
        /// <returns>是否仍在播放</returns>
        public bool Tick(float deltaTime)
        {
            if (!_isPlaying) return false;

            _elapsed += deltaTime;

            float t = Progress;
            double easedT = ApplyEase(t);

            _currentValue = Lerp(_fromValue, _toValue, easedT);

            // 检查是否完成
            if (t >= 1f)
            {
                _currentValue = _toValue;
                _isPlaying = false;
                return false;
            }

            // 检查是否足够接近目标，提前吸附
            if (Math.Abs(_currentValue - _toValue) <= _config.SnapThreshold)
            {
                _currentValue = _toValue;
                _isPlaying = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取当前值的格式化文本。
        /// </summary>
        /// <returns>格式化后的字符串</returns>
        public string GetFormattedValue()
        {
            return _config.Format(_currentValue);
        }

        /// <summary>
        /// 停止播放，保持当前值。
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
        }

        /// <summary>
        /// 跳到目标值。
        /// </summary>
        public void SkipToEnd()
        {
            _currentValue = _toValue;
            _isPlaying = false;
        }

        /// <summary>
        /// 重置引擎。
        /// </summary>
        public void Reset()
        {
            _fromValue = 0;
            _toValue = 0;
            _currentValue = 0;
            _elapsed = 0f;
            _isPlaying = false;
        }

        #region 缓动曲线

        /// <summary>
        /// 根据 ease 类型计算缓动后的 t 值。
        /// </summary>
        private double ApplyEase(float t)
        {
            switch (_config.EaseType)
            {
                case RollerEaseType.Linear:
                    return t;

                case RollerEaseType.EaseIn:
                    return t * t;

                case RollerEaseType.EaseOut:
                    return 1.0 - (1.0 - t) * (1.0 - t);

                case RollerEaseType.EaseInOut:
                    return t < 0.5f
                        ? 2.0 * t * t
                        : 1.0 - Math.Pow(-2.0 * t + 2.0, 2) / 2.0;

                case RollerEaseType.Bounce:
                    return BounceEase(t);

                case RollerEaseType.Overshoot:
                    return OvershootEase(t);

                default:
                    return t;
            }
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// 弹跳缓动：到达目标后反弹几次。
        /// </summary>
        private static double BounceEase(float t)
        {
            const double n1 = 7.5625;
            const double d1 = 2.75;

            if (t < 1.0 / d1)
            {
                return n1 * t * t;
            }

            if (t < 2.0 / d1)
            {
                double t2 = t - 1.5 / d1;
                return n1 * t2 * t2 + 0.75;
            }

            if (t < 2.5 / d1)
            {
                double t3 = t - 2.25 / d1;
                return n1 * t3 * t3 + 0.9375;
            }

            double t4 = t - 2.625 / d1;
            return n1 * t4 * t4 + 0.984375;
        }

        /// <summary>
        /// 过冲缓动：先超过目标再回弹。
        /// </summary>
        private static double OvershootEase(float t)
        {
            const double c1 = 1.70158;
            const double c3 = c1 + 1.0;

            double t2 = t;
            return 1.0 + c3 * Math.Pow(t2 - 1.0, 3) + c1 * Math.Pow(t2 - 1.0, 2);
        }

        #endregion
    }
}
