using CNoom.UnityGameTool.TextAnimation;
using UnityEditor;
using UnityEngine;

namespace CNoom.UnityGameTool.Editor.TextAnimation
{
    /// <summary>
    /// TextAnimationConfig 的自定义 Inspector 绘制器。
    /// 根据动画类型动态显示/隐藏相关字段，并附带中文 Tooltip 说明。
    /// </summary>
    [CustomPropertyDrawer(typeof(TextAnimationConfig))]
    public class TextAnimationConfigDrawer : PropertyDrawer
    {
        // 字段属性名常量
        private const string PropType = "_type";
        private const string PropDuration = "_duration";
        private const string PropSpeed = "_speed";
        private const string PropAmplitude = "_amplitude";
        private const string PropFrequency = "_frequency";
        private const string PropCharDelay = "_charDelay";
        private const string PropFadeDuration = "_fadeDuration";
        private const string PropFadeOutDuration = "_fadeOutDuration";

        // 中文 Tooltip
        private const string TipType =
            "动画效果类型\n• Wave：正弦波上下浮动\n• Shake：随机位置抖动\n• Bounce：缩放弹跳\n• Fade：逐字渐显";
        private const string TipDuration =
            "动画总时长（秒）。设为 -1 表示无限循环播放";
        private const string TipSpeed =
            "播放速度倍率。1 为正常速度，大于 1 加速";
        private const string TipAmplitudeWave =
            "波浪浮动幅度（像素）。值越大上下浮动范围越大";
        private const string TipAmplitudeShake =
            "抖动幅度（像素）。值越大抖动越剧烈";
        private const string TipAmplitudeBounce =
            "弹跳缩放幅度。值越大弹跳越明显（内部除以 50）";
        private const string TipFrequencyWave =
            "波浪频率。值越大波浪越密集";
        private const string TipFrequencyBounce =
            "弹跳频率。值越大弹跳越快";
        private const string TipCharDelay =
            "相邻字符之间的动画延迟（秒）。0 为同时运动，大于 0 产生波浪扩散效果";
        private const string TipFadeDuration =
            "每个字符从完全透明到完全显示的渐变时长（秒）";
        private const string TipFadeOutDuration =
            "动画结束时的平滑过渡时长（秒）。0 表示立即停止，大于 0 则动画数据渐变归零";

        // 中文标签
        private const string LabelType = "动画类型";
        private const string LabelDuration = "持续时间";
        private const string LabelSpeed = "播放速度";
        private const string LabelAmplitude = "幅度";
        private const string LabelFrequency = "频率";
        private const string LabelCharDelay = "字符延迟";
        private const string LabelFadeDuration = "渐显时长";
        private const string LabelFadeOutDuration = "过渡时长";

        // 分组标题
        private const string HeaderTime = "时间控制";
        private const string HeaderEffect = "效果参数";
        private const string HeaderCharSpacing = "字符间延迟";

        // Range 限制
        private const float SpeedMin = 0.1f;
        private const float SpeedMax = 10f;
        private const float AmplitudeMin = 0f;
        private const float AmplitudeMax = 100f;
        private const float FrequencyMin = 0.1f;
        private const float FrequencyMax = 20f;
        private const float CharDelayMin = 0f;
        private const float CharDelayMax = 0.5f;
        private const float FadeDurationMin = 0.05f;
        private const float FadeDurationMax = 2f;
        private const float FadeOutDurationMin = 0f;
        private const float FadeOutDurationMax = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var animType = (TextAnimationType)property.FindPropertyRelative(PropType).enumValueIndex;
            float height = SingleLine();

            // 分组标题：时间控制
            height += HeaderHeight();

            // 持续时间（非 Fade）
            if (animType != TextAnimationType.Fade)
                height += Step();

            // 播放速度（所有类型）
            height += Step();

            // 过渡时长（非 Fade 且非循环）
            if (animType != TextAnimationType.Fade)
                height += Step();

            // 分组标题：效果参数
            height += HeaderHeight();

            // 幅度（非 Fade）
            if (animType != TextAnimationType.Fade)
                height += Step();

            // 频率（Wave、Bounce）
            if (animType == TextAnimationType.Wave || animType == TextAnimationType.Bounce)
                height += Step();

            // 分组标题：字符间延迟
            height += HeaderHeight();

            // 字符延迟（所有类型）
            height += Step();

            // 渐显时长（仅 Fade）
            if (animType == TextAnimationType.Fade)
                height += Step();

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var typeProp = property.FindPropertyRelative(PropType);
            var animType = (TextAnimationType)typeProp.enumValueIndex;

            float y = position.y;
            float x = position.x;
            float w = position.width;

            // --- 动画类型 ---
            DrawEnum(ref y, x, w, typeProp, LabelType, TipType);

            // --- 时间控制 ---
            DrawGroupHeader(ref y, x, w, HeaderTime);

            if (animType != TextAnimationType.Fade)
            {
                DrawFloat(ref y, x, w, property.FindPropertyRelative(PropDuration),
                    LabelDuration, TipDuration);
            }

            DrawSlider(ref y, x, w, property.FindPropertyRelative(PropSpeed),
                LabelSpeed, TipSpeed, SpeedMin, SpeedMax);

            if (animType != TextAnimationType.Fade)
            {
                DrawSlider(ref y, x, w, property.FindPropertyRelative(PropFadeOutDuration),
                    LabelFadeOutDuration, TipFadeOutDuration, FadeOutDurationMin, FadeOutDurationMax);
            }

            // --- 效果参数 ---
            DrawGroupHeader(ref y, x, w, HeaderEffect);

            if (animType != TextAnimationType.Fade)
            {
                string ampTip = animType switch
                {
                    TextAnimationType.Wave => TipAmplitudeWave,
                    TextAnimationType.Shake => TipAmplitudeShake,
                    TextAnimationType.Bounce => TipAmplitudeBounce,
                    _ => TipAmplitudeWave
                };
                DrawSlider(ref y, x, w, property.FindPropertyRelative(PropAmplitude),
                    LabelAmplitude, ampTip, AmplitudeMin, AmplitudeMax);
            }

            if (animType == TextAnimationType.Wave || animType == TextAnimationType.Bounce)
            {
                string freqTip = animType == TextAnimationType.Wave
                    ? TipFrequencyWave
                    : TipFrequencyBounce;
                DrawSlider(ref y, x, w, property.FindPropertyRelative(PropFrequency),
                    LabelFrequency, freqTip, FrequencyMin, FrequencyMax);
            }

            // --- 字符间延迟 ---
            DrawGroupHeader(ref y, x, w, HeaderCharSpacing);

            DrawSlider(ref y, x, w, property.FindPropertyRelative(PropCharDelay),
                LabelCharDelay, TipCharDelay, CharDelayMin, CharDelayMax);

            if (animType == TextAnimationType.Fade)
            {
                DrawSlider(ref y, x, w, property.FindPropertyRelative(PropFadeDuration),
                    LabelFadeDuration, TipFadeDuration, FadeDurationMin, FadeDurationMax);
            }

            EditorGUI.EndProperty();
        }

        #region 绘制辅助

        private void DrawEnum(ref float y, float x, float w,
            SerializedProperty prop, string label, string tooltip)
        {
            var rect = new Rect(x, y, w, SingleLine());
            EditorGUI.PropertyField(rect, prop, new GUIContent(label, tooltip));
            y += Step();
        }

        private void DrawFloat(ref float y, float x, float w,
            SerializedProperty prop, string label, string tooltip)
        {
            var rect = new Rect(x, y, w, SingleLine());
            EditorGUI.PropertyField(rect, prop, new GUIContent(label, tooltip));
            y += Step();
        }

        private void DrawSlider(ref float y, float x, float w,
            SerializedProperty prop, string label, string tooltip,
            float min, float max)
        {
            var rect = new Rect(x, y, w, SingleLine());
            EditorGUI.Slider(rect, prop, min, max, new GUIContent(label, tooltip));
            y += Step();
        }

        private void DrawGroupHeader(ref float y, float x, float w, string title)
        {
            var rect = new Rect(x, y, w, SingleLine());

            // 绘制分组标题和分割线
            var style = EditorStyles.miniBoldLabel;
            EditorGUI.LabelField(rect, title, style);

            y += HeaderHeight();
        }

        private static float SingleLine()
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private static float Spacing()
        {
            return EditorGUIUtility.standardVerticalSpacing;
        }

        private static float Step()
        {
            return SingleLine() + Spacing();
        }

        private static float HeaderHeight()
        {
            return SingleLine() + 4f;
        }

        #endregion
    }
}
