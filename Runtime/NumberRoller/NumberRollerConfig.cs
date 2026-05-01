using System;
using UnityEngine;

namespace CNoom.UnityGameTool.NumberRoller
{
    /// <summary>
    /// 缓动曲线类型
    /// </summary>
    public enum RollerEaseType
    {
        /// <summary>线性</summary>
        Linear,
        /// <summary> ease in（慢→快）</summary>
        EaseIn,
        /// <summary> ease out（快→慢）</summary>
        EaseOut,
        /// <summary> ease in-out（慢→快→慢）</summary>
        EaseInOut,
        /// <summary>弹跳效果</summary>
        Bounce,
        /// <summary>先超过目标再回弹</summary>
        Overshoot
    }

    /// <summary>
    /// 数字格式化类型
    /// </summary>
    public enum RollerFormatType
    {
        /// <summary>整数（无小数位）</summary>
        Integer,
        /// <summary>固定小数位</summary>
        FixedDecimal,
        /// <summary>自定义格式字符串</summary>
        Custom
    }

    /// <summary>
    /// 数字滚动动画配置。支持内联序列化，可直接在 Inspector 中编辑。
    /// </summary>
    [Serializable]
    public class NumberRollerConfig
    {
        [Header("时间控制")]
        [Tooltip("滚动持续时间（秒）")]
        [Range(0.1f, 30f)]
        [SerializeField]
        private float _duration = 1f;

        [Tooltip("缓动曲线类型")]
        [SerializeField]
        private RollerEaseType _easeType = RollerEaseType.EaseOut;

        [Header("格式化")]
        [Tooltip("数字格式化类型")]
        [SerializeField]
        private RollerFormatType _formatType = RollerFormatType.Integer;

        [Tooltip("固定小数位数（仅 FixedDecimal 模式）")]
        [Range(0, 6)]
        [SerializeField]
        private int _decimalPlaces = 0;

        [Tooltip("是否启用千分位分隔符（如 1,000）")]
        [SerializeField]
        private bool _useThousandsSeparator = true;

        [Tooltip("自定义格式字符串（仅 Custom 模式），如 N2、C0、P0")]
        [SerializeField]
        private string _customFormat = "N2";

        [Tooltip("正数前缀（如 +）")]
        [SerializeField]
        private string _positivePrefix = "";

        [Tooltip("是否对负数显示减号前缀")]
        [SerializeField]
        private bool _showNegativeSign = true;

        [Header("高级")]
        [Tooltip("差值小于此阈值时直接跳到目标值，避免长时间微小滚动")]
        [SerializeField]
        private double _snapThreshold = 0.5;

        /// <summary>滚动持续时间（秒）</summary>
        public float Duration => _duration;

        /// <summary>缓动曲线类型</summary>
        public RollerEaseType EaseType => _easeType;

        /// <summary>格式化类型</summary>
        public RollerFormatType FormatType => _formatType;

        /// <summary>小数位数</summary>
        public int DecimalPlaces => _decimalPlaces;

        /// <summary>是否使用千分位分隔符</summary>
        public bool UseThousandsSeparator => _useThousandsSeparator;

        /// <summary>自定义格式字符串</summary>
        public string CustomFormat => _customFormat;

        /// <summary>正数前缀</summary>
        public string PositivePrefix => _positivePrefix;

        /// <summary>是否显示负号</summary>
        public bool ShowNegativeSign => _showNegativeSign;

        /// <summary>差值跳变阈值</summary>
        public double SnapThreshold => _snapThreshold;

        /// <summary>
        /// 将数值格式化为显示文本。
        /// </summary>
        /// <param name="value">当前值</param>
        /// <returns>格式化后的文本</returns>
        public string Format(double value)
        {
            string formatted;

            switch (_formatType)
            {
                case RollerFormatType.Integer:
                    formatted = ((long)Math.Round(value)).ToString(
                        _useThousandsSeparator ? "N0" : "F0");
                    break;

                case RollerFormatType.FixedDecimal:
                    string fixedPattern = _useThousandsSeparator
                        ? $"N{_decimalPlaces}"
                        : $"F{_decimalPlaces}";
                    formatted = value.ToString(fixedPattern);
                    break;

                case RollerFormatType.Custom:
                    formatted = value.ToString(_customFormat);
                    break;

                default:
                    formatted = value.ToString("N0");
                    break;
            }

            // 添加前缀
            if (value >= 0 && !string.IsNullOrEmpty(_positivePrefix))
            {
                formatted = _positivePrefix + formatted;
            }
            else if (value < 0 && !_showNegativeSign)
            {
                formatted = formatted.TrimStart('-');
            }

            return formatted;
        }
    }
}
