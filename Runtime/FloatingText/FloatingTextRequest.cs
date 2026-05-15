using UnityEngine;

namespace CNoom.UnityGameTool.FloatingText
{
    /// <summary>
    /// 飘字请求参数，由调用者构造传入。
    /// </summary>
    public struct FloatingTextRequest
    {
        /// <summary>显示文本（"-128"、"+50"、"MISS" 等）</summary>
        public string Text;

        /// <summary>世界坐标 X</summary>
        public float WorldX;

        /// <summary>世界坐标 Y</summary>
        public float WorldZ;

        /// <summary>缩放倍率（1.0=普通，1.5=暴击放大等）</summary>
        public float ScaleMultiplier;

        /// <summary>是否启用水平抖动效果</summary>
        public bool EnableShake;

        /// <summary>飘字颜色，由调用者决定</summary>
        public Color Color;

        /// <summary>
        /// 创建飘字请求。
        /// </summary>
        /// <param name="text">显示文本</param>
        /// <param name="worldX">世界坐标 X</param>
        /// <param name="worldZ">世界坐标 Z</param>
        /// <param name="color">飘字颜色</param>
        /// <param name="scaleMultiplier">缩放倍率（默认 1.0）</param>
        /// <param name="enableShake">是否启用抖动（默认 false）</param>
        public FloatingTextRequest(
            string text,
            float worldX,
            float worldZ,
            Color color,
            float scaleMultiplier = 1f,
            bool enableShake = false)
        {
            Text = text;
            WorldX = worldX;
            WorldZ = worldZ;
            Color = color;
            ScaleMultiplier = scaleMultiplier;
            EnableShake = enableShake;
        }
    }
}
