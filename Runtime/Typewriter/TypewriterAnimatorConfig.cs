using System;
using CNoom.UnityGameTool.TextAnimation;
using UnityEngine;

namespace CNoom.UnityGameTool.Typewriter
{
    /// <summary>
    /// 打字动画配置。将打字机速度参数与入场动画参数合并为单一配置，
    /// 支持内联序列化，可直接在 Inspector 中编辑。
    /// </summary>
    [Serializable]
    public class TypewriterAnimatorConfig
    {
        [Header("打字速度")]
        [Tooltip("每秒显示的字符数")]
        [Range(1f, 120f)]
        [SerializeField]
        private float _charactersPerSecond = 30f;

        [Header("标点延迟")]
        [Tooltip("是否启用标点符号延迟")]
        [SerializeField]
        private bool _enablePunctuationDelay = true;

        [Tooltip("标点符号延迟倍率（相对于基础延迟）")]
        [Range(1f, 20f)]
        [SerializeField]
        private float _punctuationDelayMultiplier = 5f;

        [Tooltip("需要额外延迟的标点符号字符集")]
        [SerializeField]
        private string _punctuationCharacters = "。，！？!?,.;：；…—~";

        [Header("特殊延迟")]
        [Tooltip("换行符延迟倍率（相对于基础延迟）")]
        [Range(1f, 20f)]
        [SerializeField]
        private float _newLineDelayMultiplier = 3f;

        [Tooltip("空格是否跳过延迟（即空格瞬间显示）")]
        [SerializeField]
        private bool _skipSpaceDelay = true;

        [Header("入场动画")]
        [Tooltip("入场动画类型")]
        [SerializeField]
        private TextAnimationType _animationType = TextAnimationType.Bounce;

        [Tooltip("动画播放模式（推荐 Once）")]
        [SerializeField]
        private TextAnimationPlayMode _playMode = TextAnimationPlayMode.Once;

        [Tooltip("单个字符的入场动画持续时间（秒）")]
        [Range(0.05f, 5f)]
        [SerializeField]
        private float _duration = 0.35f;

        [Tooltip("播放速度倍率")]
        [Range(0.1f, 10f)]
        [SerializeField]
        private float _speed = 1f;

        [Tooltip("动画幅度")]
        [Range(0f, 100f)]
        [SerializeField]
        private float _amplitude = 18f;

        [Tooltip("动画频率")]
        [Range(0.1f, 20f)]
        [SerializeField]
        private float _frequency = 2f;

        [Tooltip("相邻字符间的动画延迟（秒）")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _charDelay = 0.03f;

        [Tooltip("Fade 模式下每个字符的渐显时长（秒）")]
        [Range(0f, 2f)]
        [SerializeField]
        private float _fadeDuration = 0.3f;

        [Tooltip("动画结束时的过渡时长（秒），0 表示无过渡")]
        [Range(0f, 2f)]
        [SerializeField]
        private float _fadeOutDuration = 0.3f;

        /// <summary>每秒显示的字符数</summary>
        public float CharactersPerSecond => _charactersPerSecond;

        /// <summary>是否启用标点延迟</summary>
        public bool EnablePunctuationDelay => _enablePunctuationDelay;

        /// <summary>标点延迟倍率</summary>
        public float PunctuationDelayMultiplier => _punctuationDelayMultiplier;

        /// <summary>标点字符集</summary>
        public string PunctuationCharacters => _punctuationCharacters;

        /// <summary>换行延迟倍率</summary>
        public float NewLineDelayMultiplier => _newLineDelayMultiplier;

        /// <summary>是否跳过空格延迟</summary>
        public bool SkipSpaceDelay => _skipSpaceDelay;

        /// <summary>动画类型</summary>
        public TextAnimationType AnimationType => _animationType;

        /// <summary>动画播放模式</summary>
        public TextAnimationPlayMode PlayMode => _playMode;

        /// <summary>入场动画持续时间</summary>
        public float Duration => _duration;

        /// <summary>播放速度倍率</summary>
        public float Speed => _speed;

        /// <summary>动画幅度</summary>
        public float Amplitude => _amplitude;

        /// <summary>动画频率</summary>
        public float Frequency => _frequency;

        /// <summary>相邻字符间动画延迟</summary>
        public float CharDelay => _charDelay;

        /// <summary>Fade 渐显时长</summary>
        public float FadeDuration => _fadeDuration;

        /// <summary>过渡淡出时长</summary>
        public float FadeOutDuration => _fadeOutDuration;

        /// <summary>
        /// 从此配置派生 TypewriterConfig（打字机节奏参数）。
        /// </summary>
        public TypewriterConfig ToTypewriterConfig()
        {
            return new TypewriterConfig(
                _charactersPerSecond,
                _enablePunctuationDelay,
                _punctuationDelayMultiplier,
                _punctuationCharacters,
                _newLineDelayMultiplier,
                _skipSpaceDelay);
        }

        /// <summary>
        /// 从此配置派生 TextAnimationConfig（入场动画参数）。
        /// </summary>
        public TextAnimationConfig ToAnimationConfig()
        {
            return new TextAnimationConfig(
                _animationType,
                _playMode,
                _duration,
                _speed,
                _amplitude,
                _frequency,
                _charDelay,
                _fadeDuration,
                _fadeOutDuration);
        }

        /// <summary>
        /// 创建自定义配置。
        /// </summary>
        public TypewriterAnimatorConfig(
            float charactersPerSecond = 30f,
            bool enablePunctuationDelay = true,
            float punctuationDelayMultiplier = 5f,
            string punctuationCharacters = "。，！？!?,.;：；…—~",
            float newLineDelayMultiplier = 3f,
            bool skipSpaceDelay = true,
            TextAnimationType animationType = TextAnimationType.Bounce,
            TextAnimationPlayMode playMode = TextAnimationPlayMode.Once,
            float duration = 0.35f,
            float speed = 1f,
            float amplitude = 18f,
            float frequency = 2f,
            float charDelay = 0.03f,
            float fadeDuration = 0.3f,
            float fadeOutDuration = 0.3f)
        {
            _charactersPerSecond = charactersPerSecond;
            _enablePunctuationDelay = enablePunctuationDelay;
            _punctuationDelayMultiplier = punctuationDelayMultiplier;
            _punctuationCharacters = punctuationCharacters;
            _newLineDelayMultiplier = newLineDelayMultiplier;
            _skipSpaceDelay = skipSpaceDelay;
            _animationType = animationType;
            _playMode = playMode;
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
