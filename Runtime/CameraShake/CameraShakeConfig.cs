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
        public CameraShakeConfig(float intensity, float duration)
        {
            _intensity = intensity;
            _duration = duration;
        }
    }
}
