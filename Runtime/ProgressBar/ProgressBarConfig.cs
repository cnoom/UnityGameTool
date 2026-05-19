using System;
using UnityEngine;

namespace CNoom.UnityGameTool.ProgressBar
{
    /// <summary>
    /// 进度条过渡缓动类型
    /// </summary>
    public enum ProgressEaseType
    {
        /// <summary>线性</summary>
        Linear,
        /// <summary>ease out（快→慢）</summary>
        EaseOut,
        /// <summary>ease in-out（慢→快→慢）</summary>
        EaseInOut
    }

    /// <summary>
    /// 进度条配置。支持内联序列化，可直接在 Inspector 中编辑。
    /// </summary>
    [Serializable]
    public class ProgressBarConfig
    {
        [Header("主进度条")]
        [Tooltip("主进度条过渡持续时间（秒）")]
        [Range(0.01f, 5f)]
        [SerializeField]
        private float _transitionDuration = 0.3f;

        [Tooltip("主进度条缓动类型")]
        [SerializeField]
        private ProgressEaseType _easeType = ProgressEaseType.EaseOut;

        [Header("延迟条")]
        [Tooltip("是否启用延迟扣减条效果")]
        [SerializeField]
        private bool _enableDelayedBar = true;

        [Tooltip("延迟条开始追赶前的等待时间（秒）")]
        [Range(0f, 3f)]
        [SerializeField]
        private float _delayedWaitTime = 0.5f;

        [Tooltip("延迟条追赶持续时间（秒）")]
        [Range(0.01f, 5f)]
        [SerializeField]
        private float _delayedCatchUpDuration = 0.8f;

        [Tooltip("延迟条缓动类型")]
        [SerializeField]
        private ProgressEaseType _delayedEaseType = ProgressEaseType.EaseInOut;

        /// <summary>主进度条过渡持续时间（秒）</summary>
        public float TransitionDuration => _transitionDuration;

        /// <summary>主进度条缓动类型</summary>
        public ProgressEaseType EaseType => _easeType;

        /// <summary>是否启用延迟扣减条</summary>
        public bool EnableDelayedBar => _enableDelayedBar;

        /// <summary>延迟条等待时间（秒）</summary>
        public float DelayedWaitTime => _delayedWaitTime;

        /// <summary>延迟条追赶持续时间（秒）</summary>
        public float DelayedCatchUpDuration => _delayedCatchUpDuration;

        /// <summary>延迟条缓动类型</summary>
        public ProgressEaseType DelayedEaseType => _delayedEaseType;

        /// <summary>
        /// 创建默认配置。
        /// </summary>
        public ProgressBarConfig() { }

        /// <summary>
        /// 创建自定义配置。
        /// </summary>
        public ProgressBarConfig(
            float transitionDuration = 0.3f,
            ProgressEaseType easeType = ProgressEaseType.EaseOut,
            bool enableDelayedBar = true,
            float delayedWaitTime = 0.5f,
            float delayedCatchUpDuration = 0.8f,
            ProgressEaseType delayedEaseType = ProgressEaseType.EaseInOut)
        {
            _transitionDuration = transitionDuration;
            _easeType = easeType;
            _enableDelayedBar = enableDelayedBar;
            _delayedWaitTime = delayedWaitTime;
            _delayedCatchUpDuration = delayedCatchUpDuration;
            _delayedEaseType = delayedEaseType;
        }
    }
}
