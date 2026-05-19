using System;
using UnityEngine;

namespace CNoom.UnityGameTool.ScreenFlash
{
    /// <summary>
    /// 屏幕闪烁模式
    /// </summary>
    public enum ScreenFlashMode
    {
        /// <summary>单次闪烁：淡入 → 停留 → 淡出</summary>
        Flash,
        /// <summary>持续脉冲：周期性重复闪烁</summary>
        Pulse
    }

    /// <summary>
    /// 屏幕特效配置。支持内联序列化，可直接在 Inspector 中编辑。
    /// </summary>
    [Serializable]
    public class ScreenFlashConfig
    {
        [Header("颜色")]
        [Tooltip("闪烁颜色")]
        [SerializeField]
        private Color _color = Color.red;

        [Tooltip("最大不透明度（0~1）")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _maxAlpha = 0.5f;

        [Header("时间控制")]
        [Tooltip("闪烁模式")]
        [SerializeField]
        private ScreenFlashMode _mode = ScreenFlashMode.Flash;

        [Tooltip("淡入持续时间（秒）")]
        [Range(0.01f, 2f)]
        [SerializeField]
        private float _fadeInDuration = 0.05f;

        [Tooltip("停留时间（秒），仅 Flash 模式")]
        [Range(0f, 2f)]
        [SerializeField]
        private float _holdDuration = 0.1f;

        [Tooltip("淡出持续时间（秒）")]
        [Range(0.01f, 3f)]
        [SerializeField]
        private float _fadeOutDuration = 0.3f;

        [Header("脉冲模式")]
        [Tooltip("脉冲重复次数，0 为无限循环")]
        [SerializeField]
        private int _pulseCount = 0;

        [Tooltip("脉冲间隔时间（秒）")]
        [Range(0.01f, 5f)]
        [SerializeField]
        private float _pulseInterval = 0.5f;

        /// <summary>闪烁颜色</summary>
        public Color Color => _color;

        /// <summary>颜色 R 分量</summary>
        public float R => _color.r;

        /// <summary>颜色 G 分量</summary>
        public float G => _color.g;

        /// <summary>颜色 B 分量</summary>
        public float B => _color.b;

        /// <summary>最大不透明度</summary>
        public float MaxAlpha => _maxAlpha;

        /// <summary>闪烁模式</summary>
        public ScreenFlashMode Mode => _mode;

        /// <summary>淡入持续时间（秒）</summary>
        public float FadeInDuration => _fadeInDuration;

        /// <summary>停留时间（秒）</summary>
        public float HoldDuration => _holdDuration;

        /// <summary>淡出持续时间（秒）</summary>
        public float FadeOutDuration => _fadeOutDuration;

        /// <summary>脉冲重复次数（0=无限）</summary>
        public int PulseCount => _pulseCount;

        /// <summary>脉冲间隔时间（秒）</summary>
        public float PulseInterval => _pulseInterval;

        /// <summary>单次闪烁总时长（秒）</summary>
        public float SingleFlashDuration => _fadeInDuration + _holdDuration + _fadeOutDuration;

        /// <summary>
        /// 创建默认配置。
        /// </summary>
        public ScreenFlashConfig() { }

        /// <summary>
        /// 便捷构造：指定颜色和持续时间。
        /// </summary>
        public ScreenFlashConfig(Color color, float duration)
        {
            _color = color;
            _fadeInDuration = duration * 0.15f;
            _holdDuration = duration * 0.2f;
            _fadeOutDuration = duration * 0.65f;
            _maxAlpha = 0.5f;
        }
    }
}
