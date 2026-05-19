using System;

namespace CNoom.UnityGameTool.ProgressBar
{
    /// <summary>
    /// 进度条每帧计算结果，由 Engine 产出，供 Driver 应用到 UI。
    /// </summary>
    public struct ProgressBarFrameData
    {
        /// <summary>主进度条归一化值 0~1</summary>
        public float Value;

        /// <summary>延迟条归一化值 0~1</summary>
        public float DelayedValue;

        /// <summary>主进度条是否完成过渡</summary>
        public bool ValueTransitionComplete;

        /// <summary>延迟条是否完成追赶</summary>
        public bool DelayedTransitionComplete;
    }

    /// <summary>
    /// 进度条纯逻辑引擎。负责主进度和延迟条的插值计算，
    /// 不依赖任何 Unity 组件，完全可单元测试。
    /// </summary>
    public class ProgressBarEngine
    {
        private readonly ProgressBarConfig _config;

        // 主进度条状态
        private float _currentValue;
        private float _targetValue;
        private float _valueFrom;
        private float _valueElapsed;
        private bool _isValueTransitioning;

        // 延迟条状态
        private float _delayedValue;
        private float _delayedTarget;
        private float _delayedFrom;
        private float _delayedWaitElapsed;
        private float _delayedCatchUpElapsed;
        private bool _isDelayedWaiting;
        private bool _isDelayedCatchingUp;

        /// <summary>当前主进度条值 0~1</summary>
        public float Value => _currentValue;

        /// <summary>当前延迟条值 0~1</summary>
        public float DelayedValue => _delayedValue;

        /// <summary>目标进度值</summary>
        public float TargetValue => _targetValue;

        /// <summary>是否正在过渡中（主进度条或延迟条任一在运动）</summary>
        public bool IsTransitioning => _isValueTransitioning || _isDelayedWaiting || _isDelayedCatchingUp;

        /// <summary>
        /// 创建进度条引擎。
        /// </summary>
        /// <param name="config">进度条配置，不可为 null</param>
        public ProgressBarEngine(ProgressBarConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 立即设置进度值，无过渡动画。
        /// </summary>
        /// <param name="value">归一化进度值 0~1</param>
        public void SetValue(float value)
        {
            _currentValue = Clamp01(value);
            _targetValue = _currentValue;
            _delayedValue = _currentValue;
            _delayedTarget = _currentValue;
            _isValueTransitioning = false;
            _isDelayedWaiting = false;
            _isDelayedCatchingUp = false;
        }

        /// <summary>
        /// 开始过渡到目标值（增加或减少）。
        /// </summary>
        /// <param name="targetValue">目标归一化进度值 0~1</param>
        public void BeginTransition(float targetValue)
        {
            targetValue = Clamp01(targetValue);

            // 如果值没有变化，不触发过渡
            if (Math.Abs(targetValue - _currentValue) < 0.0001f)
            {
                return;
            }

            bool isDecrease = targetValue < _currentValue;

            // 主进度条开始过渡
            _targetValue = targetValue;
            _valueFrom = _currentValue;
            _valueElapsed = 0f;
            _isValueTransitioning = true;

            // 延迟条：仅在减少时启用延迟效果
            if (isDecrease && _config.EnableDelayedBar)
            {
                // 延迟条保持当前位置，等待后追赶
                _delayedTarget = targetValue;
                _delayedFrom = _delayedValue;
                _delayedWaitElapsed = 0f;
                _delayedCatchUpElapsed = 0f;
                _isDelayedWaiting = true;
                _isDelayedCatchingUp = false;
            }
            else
            {
                // 增加时延迟条直接跟随
                _delayedValue = targetValue;
                _delayedTarget = targetValue;
                _isDelayedWaiting = false;
                _isDelayedCatchingUp = false;
            }
        }

        /// <summary>
        /// 推进一帧，计算主进度条和延迟条的当前值。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒）</param>
        /// <returns>本帧计算结果</returns>
        public ProgressBarFrameData Tick(float deltaTime)
        {
            var result = new ProgressBarFrameData();

            // 主进度条过渡
            if (_isValueTransitioning)
            {
                _valueElapsed += deltaTime;
                float duration = _config.TransitionDuration;
                float t = duration > 0f ? Math.Min(1f, _valueElapsed / duration) : 1f;
                float easedT = ApplyEase(t, _config.EaseType);

                _currentValue = Lerp(_valueFrom, _targetValue, easedT);

                if (t >= 1f)
                {
                    _currentValue = _targetValue;
                    _isValueTransitioning = false;
                    result.ValueTransitionComplete = true;
                }
            }

            // 延迟条等待阶段
            if (_isDelayedWaiting)
            {
                _delayedWaitElapsed += deltaTime;
                if (_delayedWaitElapsed >= _config.DelayedWaitTime)
                {
                    _isDelayedWaiting = false;
                    _isDelayedCatchingUp = true;
                    _delayedCatchUpElapsed = 0f;
                    _delayedFrom = _delayedValue;
                }
            }

            // 延迟条追赶阶段
            if (_isDelayedCatchingUp)
            {
                _delayedCatchUpElapsed += deltaTime;
                float duration = _config.DelayedCatchUpDuration;
                float t = duration > 0f ? Math.Min(1f, _delayedCatchUpElapsed / duration) : 1f;
                float easedT = ApplyEase(t, _config.DelayedEaseType);

                _delayedValue = Lerp(_delayedFrom, _delayedTarget, easedT);

                if (t >= 1f)
                {
                    _delayedValue = _delayedTarget;
                    _isDelayedCatchingUp = false;
                    result.DelayedTransitionComplete = true;
                }
            }

            result.Value = _currentValue;
            result.DelayedValue = _delayedValue;
            return result;
        }

        /// <summary>
        /// 停止所有过渡，保持当前值。
        /// </summary>
        public void Stop()
        {
            _isValueTransitioning = false;
            _isDelayedWaiting = false;
            _isDelayedCatchingUp = false;
        }

        /// <summary>
        /// 完全重置引擎。
        /// </summary>
        public void Reset()
        {
            _currentValue = 0f;
            _targetValue = 0f;
            _valueFrom = 0f;
            _valueElapsed = 0f;
            _isValueTransitioning = false;
            _delayedValue = 0f;
            _delayedTarget = 0f;
            _delayedFrom = 0f;
            _delayedWaitElapsed = 0f;
            _delayedCatchUpElapsed = 0f;
            _isDelayedWaiting = false;
            _isDelayedCatchingUp = false;
        }

        #region 内部计算

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// 根据 ease 类型计算缓动后的 t 值。
        /// </summary>
        private static float ApplyEase(float t, ProgressEaseType easeType)
        {
            switch (easeType)
            {
                case ProgressEaseType.Linear:
                    return t;

                case ProgressEaseType.EaseOut:
                    return 1f - (1f - t) * (1f - t);

                case ProgressEaseType.EaseInOut:
                    return t < 0.5f
                        ? 2f * t * t
                        : 1f - (float)Math.Pow(-2f * t + 2f, 2) / 2f;

                default:
                    return t;
            }
        }

        #endregion
    }
}
