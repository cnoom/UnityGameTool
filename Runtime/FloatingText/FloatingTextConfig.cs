using System;
using UnityEngine;

namespace CNoom.UnityGameTool.FloatingText
{
    /// <summary>
    /// 飘字动画配置。支持内联序列化，可直接在 Inspector 中编辑。
    /// </summary>
    [Serializable]
    public class FloatingTextConfig
    {
        [Header("时间控制")]
        [Tooltip("弹出动画时长（秒）")]
        [Range(0.01f, 1f)]
        [SerializeField]
        private float _popInDuration = 0.15f;

        [Tooltip("停留时长（秒）")]
        [Range(0.01f, 3f)]
        [SerializeField]
        private float _holdDuration = 0.6f;

        [Tooltip("渐隐时长（秒）")]
        [Range(0.01f, 2f)]
        [SerializeField]
        private float _fadeOutDuration = 0.3f;

        [Header("运动参数")]
        [Tooltip("上浮速度（像素/秒）")]
        [Range(0f, 300f)]
        [SerializeField]
        private float _floatSpeed = 80f;

        [Tooltip("重力加速度（负值向下，0=匀速上浮）")]
        [Range(-200f, 0f)]
        [SerializeField]
        private float _gravity = -20f;

        [Header("弹出效果")]
        [Tooltip("弹出过冲系数（1.0=无过冲，>1 越大弹性越强）")]
        [Range(1f, 2f)]
        [SerializeField]
        private float _popInOvershoot = 1.3f;

        [Header("位置随机")]
        [Tooltip("水平随机偏移范围（像素）")]
        [Range(0f, 100f)]
        [SerializeField]
        private float _randomSpreadX = 20f;

        [Tooltip("垂直随机偏移范围（像素）")]
        [Range(0f, 50f)]
        [SerializeField]
        private float _randomSpreadY = 10f;

        [Header("抖动效果")]
        [Tooltip("水平抖动幅度（像素）")]
        [Range(0f, 30f)]
        [SerializeField]
        private float _shakeAmplitude = 5f;

        [Tooltip("水平抖动频率")]
        [Range(1f, 50f)]
        [SerializeField]
        private float _shakeFrequency = 20f;

        [Header("对象池")]
        [Tooltip("对象池初始大小")]
        [Range(1, 64)]
        [SerializeField]
        private int _poolSize = 16;

        /// <summary>弹出动画时长（秒）</summary>
        public float PopInDuration => _popInDuration;

        /// <summary>停留时长（秒）</summary>
        public float HoldDuration => _holdDuration;

        /// <summary>渐隐时长（秒）</summary>
        public float FadeOutDuration => _fadeOutDuration;

        /// <summary>上浮速度（像素/秒）</summary>
        public float FloatSpeed => _floatSpeed;

        /// <summary>重力加速度</summary>
        public float Gravity => _gravity;

        /// <summary>弹出过冲系数</summary>
        public float PopInOvershoot => _popInOvershoot;

        /// <summary>水平随机偏移范围（像素）</summary>
        public float RandomSpreadX => _randomSpreadX;

        /// <summary>垂直随机偏移范围（像素）</summary>
        public float RandomSpreadY => _randomSpreadY;

        /// <summary>水平抖动幅度（像素）</summary>
        public float ShakeAmplitude => _shakeAmplitude;

        /// <summary>水平抖动频率</summary>
        public float ShakeFrequency => _shakeFrequency;

        /// <summary>对象池初始大小</summary>
        public int PoolSize => _poolSize;

        /// <summary>总动画时长</summary>
        public float TotalDuration => _popInDuration + _holdDuration + _fadeOutDuration;

        /// <summary>
        /// 创建默认配置。
        /// </summary>
        public FloatingTextConfig() { }

        /// <summary>
        /// 创建自定义配置。
        /// </summary>
        public FloatingTextConfig(
            float popInDuration = 0.15f,
            float holdDuration = 0.6f,
            float fadeOutDuration = 0.3f,
            float floatSpeed = 80f,
            float gravity = -20f,
            float popInOvershoot = 1.3f,
            float randomSpreadX = 20f,
            float randomSpreadY = 10f,
            float shakeAmplitude = 5f,
            float shakeFrequency = 20f,
            int poolSize = 16)
        {
            _popInDuration = popInDuration;
            _holdDuration = holdDuration;
            _fadeOutDuration = fadeOutDuration;
            _floatSpeed = floatSpeed;
            _gravity = gravity;
            _popInOvershoot = popInOvershoot;
            _randomSpreadX = randomSpreadX;
            _randomSpreadY = randomSpreadY;
            _shakeAmplitude = shakeAmplitude;
            _shakeFrequency = shakeFrequency;
            _poolSize = poolSize;
        }
    }
}
