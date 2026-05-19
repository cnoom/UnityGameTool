using System;
using UnityEngine;

namespace CNoom.UnityGameTool.Pulse
{
    /// <summary>
    /// 脉冲动画类型
    /// </summary>
    public enum PulseType
    {
        /// <summary>缩放脉冲（大小周期性变化）</summary>
        Scale,
        /// <summary>发光脉冲（Alpha/亮度周期性变化）</summary>
        Glow,
        /// <summary>位移脉冲（Y 轴周期性上下浮动）</summary>
        Float
    }

    /// <summary>
    /// 脉冲缓动类型
    /// </summary>
    public enum PulseEaseType
    {
        /// <summary>正弦波（平滑过渡）</summary>
        Sine,
        /// <summary>线性（匀速）</summary>
        Linear,
        /// <summary>指数（快速变化后缓慢）</summary>
        Exponential
    }

    /// <summary>
    /// 脉冲/呼吸效果配置。支持内联序列化，可直接在 Inspector 中编辑。
    /// </summary>
    [Serializable]
    public class PulseConfig
    {
        [Header("动画类型")]
        [Tooltip("脉冲动画类型")]
        [SerializeField]
        private PulseType _type = PulseType.Scale;

        [Tooltip("缓动类型")]
        [SerializeField]
        private PulseEaseType _easeType = PulseEaseType.Sine;

        [Header("时间控制")]
        [Tooltip("一个完整周期的时长（秒）")]
        [Range(0.1f, 10f)]
        [SerializeField]
        private float _period = 1.5f;

        [Tooltip("播放速度倍率")]
        [Range(0.1f, 5f)]
        [SerializeField]
        private float _speed = 1f;

        [Tooltip("是否无限循环")]
        [SerializeField]
        private bool _isLooping = true;

        [Tooltip("总播放时长（秒），仅非循环模式有效")]
        [Range(0.1f, 30f)]
        [SerializeField]
        private float _duration = 3f;

        [Header("幅度")]
        [Tooltip("缩放脉冲的最小缩放值")]
        [Range(0.1f, 2f)]
        [SerializeField]
        private float _minScale = 0.9f;

        [Tooltip("缩放脉冲的最大缩放值")]
        [Range(0.1f, 3f)]
        [SerializeField]
        private float _maxScale = 1.1f;

        [Tooltip("发光脉冲的最小 Alpha")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _minAlpha = 0.3f;

        [Tooltip("发光脉冲的最大 Alpha")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _maxAlpha = 1f;

        [Tooltip("浮动脉冲的幅度（像素）")]
        [Range(0f, 50f)]
        [SerializeField]
        private float _floatAmplitude = 5f;

        /// <summary>动画类型</summary>
        public PulseType Type => _type;

        /// <summary>缓动类型</summary>
        public PulseEaseType EaseType => _easeType;

        /// <summary>周期时长（秒）</summary>
        public float Period => _period;

        /// <summary>播放速度倍率</summary>
        public float Speed => _speed;

        /// <summary>是否无限循环</summary>
        public bool IsLooping => _isLooping;

        /// <summary>总播放时长（秒）</summary>
        public float Duration => _duration;

        /// <summary>最小缩放值</summary>
        public float MinScale => _minScale;

        /// <summary>最大缩放值</summary>
        public float MaxScale => _maxScale;

        /// <summary>最小 Alpha</summary>
        public float MinAlpha => _minAlpha;

        /// <summary>最大 Alpha</summary>
        public float MaxAlpha => _maxAlpha;

        /// <summary>浮动幅度（像素）</summary>
        public float FloatAmplitude => _floatAmplitude;

        /// <summary>
        /// 创建默认配置。
        /// </summary>
        public PulseConfig() { }

        /// <summary>
        /// 创建自定义配置。
        /// </summary>
        public PulseConfig(
            PulseType type = PulseType.Scale,
            PulseEaseType easeType = PulseEaseType.Sine,
            float period = 1.5f,
            float speed = 1f,
            bool isLooping = true,
            float duration = 3f,
            float minScale = 0.9f,
            float maxScale = 1.1f,
            float minAlpha = 0.3f,
            float maxAlpha = 1f,
            float floatAmplitude = 5f)
        {
            _type = type;
            _easeType = easeType;
            _period = period;
            _speed = speed;
            _isLooping = isLooping;
            _duration = duration;
            _minScale = minScale;
            _maxScale = maxScale;
            _minAlpha = minAlpha;
            _maxAlpha = maxAlpha;
            _floatAmplitude = floatAmplitude;
        }
    }
}
