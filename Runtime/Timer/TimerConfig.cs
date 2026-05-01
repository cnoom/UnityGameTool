using System;
using UnityEngine;

namespace CNoom.UnityGameTool.Timer
{
    /// <summary>
    /// 游戏计时器配置。支持内联序列化，可直接在 Inspector 中编辑。
    /// </summary>
    [Serializable]
    public class TimerConfig
    {
        [Header("时间缩放")]
        [Tooltip("时间缩放因子，1.0 为正常速度，2.0 为两倍速")]
        [Range(0.01f, 10f)]
        [SerializeField]
        private float _timeScale = 1f;

        [Tooltip("是否使用 unscaledTime（不受 Time.timeScale 影响）")]
        [SerializeField]
        private bool _useUnscaledTime = false;

        [Header("警告阈值")]
        [Tooltip("是否启用警告阈值（倒计时模式下剩余时间到达此值时触发 OnWarning）")]
        [SerializeField]
        private bool _enableWarning = true;

        [Tooltip("警告阈值（秒），倒计时剩余时间 <= 此值时触发")]
        [Range(0f, 600f)]
        [SerializeField]
        private float _warningThreshold = 10f;

        /// <summary>时间缩放因子</summary>
        public float TimeScale => _timeScale;

        /// <summary>是否使用 unscaledTime</summary>
        public bool UseUnscaledTime => _useUnscaledTime;

        /// <summary>是否启用警告阈值</summary>
        public bool EnableWarning => _enableWarning;

        /// <summary>警告阈值（秒）</summary>
        public float WarningThreshold => _warningThreshold;
    }
}
