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
        // 字段由 TextAnimationConfigDrawer 自定义绘制，
        // Inspector 显示的标签和 Tooltip 由 Drawer 控制。

        [SerializeField]
        private TextAnimationType _type = TextAnimationType.Wave;

        [SerializeField]
        private float _duration = -1f;

        [SerializeField]
        private float _speed = 1f;

        [SerializeField]
        private float _amplitude = 10f;

        [SerializeField]
        private float _frequency = 2f;

        [SerializeField]
        private float _charDelay = 0.05f;

        [SerializeField]
        private float _fadeDuration = 0.3f;

        [SerializeField]
        private float _fadeOutDuration = 0.3f;

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

        /// <summary>动画结束时的过渡时长（秒），0 表示无过渡立即停止</summary>
        public float FadeOutDuration => _fadeOutDuration;

        /// <summary>是否为循环模式</summary>
        public bool IsLooping => _duration < 0f;

        /// <summary>
        /// 创建自定义配置。
        /// </summary>
        public TextAnimationConfig(
            TextAnimationType type = TextAnimationType.Wave,
            float duration = -1f,
            float speed = 1f,
            float amplitude = 10f,
            float frequency = 2f,
            float charDelay = 0.05f,
            float fadeDuration = 0.3f,
            float fadeOutDuration = 0.3f)
        {
            _type = type;
            _duration = duration;
            _speed = speed;
            _amplitude = amplitude;
            _frequency = frequency;
            _charDelay = charDelay;
            _fadeDuration = fadeDuration;
            _fadeOutDuration = fadeOutDuration;
        }
    }
}
