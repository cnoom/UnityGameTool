using System;
using UnityEngine;

namespace CNoom.UnityGameTool.TextAnimation
{
    /// <summary>
    /// 文字动画类型
    /// </summary>
    public enum TextAnimationType
    {
        /// <summary>正弦波上下浮动</summary>
        Wave,
        /// <summary>随机位置抖动</summary>
        Shake,
        /// <summary>缩放弹跳</summary>
        Bounce,
        /// <summary>逐字渐显</summary>
        Fade
    }

    /// <summary>
    /// 文字动画效果配置。支持内联序列化，可直接在 Inspector 中编辑。
    /// </summary>
    [Serializable]
    public class TextAnimationConfig
    {
        [Header("动画类型")]
        [Tooltip("文字动画效果类型")]
        [SerializeField]
        private TextAnimationType _type = TextAnimationType.Wave;

        [Header("时间控制")]
        [Tooltip("动画持续时间（秒），设为 -1 表示无限循环")]
        [SerializeField]
        private float _duration = -1f;

        [Tooltip("动画播放速度倍率")]
        [Range(0.1f, 10f)]
        [SerializeField]
        private float _speed = 1f;

        [Header("幅度与频率")]
        [Tooltip("动画幅度（像素），控制偏移或缩放强度")]
        [Range(0f, 100f)]
        [SerializeField]
        private float _amplitude = 10f;

        [Tooltip("动画频率，控制波浪密度或抖动速度")]
        [Range(0.1f, 20f)]
        [SerializeField]
        private float _frequency = 2f;

        [Header("字符间延迟")]
        [Tooltip("相邻字符之间的动画延迟（秒），产生波浪扩散效果")]
        [Range(0f, 0.5f)]
        [SerializeField]
        private float _charDelay = 0.05f;

        [Header("Fade 专用")]
        [Tooltip("Fade 模式下每个字符的渐显持续时间（秒）")]
        [Range(0.05f, 2f)]
        [SerializeField]
        private float _fadeDuration = 0.3f;

        /// <summary>动画类型</summary>
        public TextAnimationType Type => _type;

        /// <summary>动画持续时间（秒），-1 表示无限循环</summary>
        public float Duration => _duration;

        /// <summary>播放速度倍率</summary>
        public float Speed => _speed;

        /// <summary>动画幅度</summary>
        public float Amplitude => _amplitude;

        /// <summary>动画频率</summary>
        public float Frequency => _frequency;

        /// <summary>相邻字符间的动画延迟（秒）</summary>
        public float CharDelay => _charDelay;

        /// <summary>Fade 模式下每个字符的渐显时长（秒）</summary>
        public float FadeDuration => _fadeDuration;

        /// <summary>是否为循环模式</summary>
        public bool IsLooping => _duration < 0f;
    }
}
