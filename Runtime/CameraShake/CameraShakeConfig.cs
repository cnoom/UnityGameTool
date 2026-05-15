using System;
using UnityEngine;

namespace CNoom.UnityGameTool.CameraShake
{
    /// <summary>
    /// 震动衰减曲线类型
    /// </summary>
    public enum ShakeDecayType
    {
        /// <summary>线性衰减</summary>
        Linear,
        /// <summary>指数衰减（快速减弱）</summary>
        Exponential,
        /// <summary>无衰减（全程等强）</summary>
        None
    }

    /// <summary>
    /// 屏幕震动配置。支持内联序列化，可直接在 Inspector 中编辑。
    /// 该配置也可作为运行时参数通过代码构造。
    /// </summary>
    [Serializable]
    public class CameraShakeConfig
    {
        [Header("强度与时间")]
        [Tooltip("震动强度（像素/单位偏移量）")]
        [Range(0.01f, 50f)]
        [SerializeField]
        private float _intensity = 1f;

        [Tooltip("震动持续时间（秒）")]
        [Range(0.01f, 5f)]
        [SerializeField]
        private float _duration = 0.3f;

        [Header("频率")]
        [Tooltip("震动频率（每秒振动次数）")]
        [Range(1f, 50f)]
        [SerializeField]
        private float _frequency = 15f;

        [Header("方向控制")]
        [Tooltip("X 轴震动强度倍率，0 为禁用 X 轴震动")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _xInfluence = 1f;

        [Tooltip("Y 轴震动强度倍率，0 为禁用 Y 轴震动")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _yInfluence = 1f;

        [Tooltip("Z 轴震动强度倍率（旋转震动）")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _zRotationInfluence;

        [Header("衰减")]
        [Tooltip("衰减曲线类型")]
        [SerializeField]
        private ShakeDecayType _decayType = ShakeDecayType.Exponential;

        /// <summary>震动强度</summary>
        public float Intensity => _intensity;

        /// <summary>震动持续时间（秒）</summary>
        public float Duration => _duration;

        /// <summary>震动频率</summary>
        public float Frequency => _frequency;

        /// <summary>X 轴强度倍率</summary>
        public float XInfluence => _xInfluence;

        /// <summary>Y 轴强度倍率</summary>
        public float YInfluence => _yInfluence;

        /// <summary>Z 轴旋转强度倍率</summary>
        public float ZRotationInfluence => _zRotationInfluence;

        /// <summary>衰减类型</summary>
        public ShakeDecayType DecayType => _decayType;

        /// <summary>
        /// 创建默认配置。
        /// </summary>
        public CameraShakeConfig() { }

        /// <summary>
        /// 便捷构造：指定强度和持续时间。
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">参数不合法时抛出</exception>
        public CameraShakeConfig(float intensity, float duration)
        {
            if (intensity < 0f)
                throw new ArgumentOutOfRangeException(nameof(intensity), "震动强度不能为负数");
            if (duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), "震动持续时间必须大于 0");
            _intensity = intensity;
            _duration = duration;
        }

        /// <summary>
        /// 校验配置参数是否合法。由 Engine 在 AddShake 时调用。
        /// </summary>
        /// <returns>校验结果，包含是否合法和错误信息</returns>
        public bool Validate(out string errorMessage)
        {
            if (_intensity < 0f)
            {
                errorMessage = "震动强度不能为负数";
                return false;
            }
            if (_duration <= 0f)
            {
                errorMessage = "震动持续时间必须大于 0";
                return false;
            }
            if (_frequency <= 0f)
            {
                errorMessage = "震动频率必须大于 0";
                return false;
            }
            errorMessage = null;
            return true;
        }
    }
}
