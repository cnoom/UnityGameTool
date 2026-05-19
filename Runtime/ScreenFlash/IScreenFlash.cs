using System;

namespace CNoom.UnityGameTool.ScreenFlash
{
    /// <summary>
    /// 屏幕特效完成事件处理器
    /// </summary>
    /// <param name="flashId">特效实例 ID</param>
    public delegate void ScreenFlashCompleteHandler(int flashId);

    /// <summary>
    /// 屏幕特效接口，定义全屏闪烁/颜色渐变的核心行为契约。
    /// 支持多特效实例叠加。
    /// </summary>
    public interface IScreenFlash
    {
        /// <summary>是否正在播放中</summary>
        bool IsPlaying { get; }

        /// <summary>当前活跃特效数量</summary>
        int ActiveCount { get; }

        /// <summary>特效完成时触发</summary>
        event ScreenFlashCompleteHandler OnComplete;

        /// <summary>
        /// 使用默认配置触发一次屏幕闪烁。
        /// </summary>
        /// <returns>特效实例 ID</returns>
        int Flash();

        /// <summary>
        /// 使用自定义配置触发一次屏幕闪烁。
        /// </summary>
        /// <param name="config">闪烁配置</param>
        /// <returns>特效实例 ID</returns>
        int Flash(ScreenFlashConfig config);

        /// <summary>
        /// 停止所有特效。
        /// </summary>
        void StopAll();

        /// <summary>
        /// 停止指定 ID 的特效。
        /// </summary>
        /// <param name="flashId">特效实例 ID</param>
        void Stop(int flashId);
    }
}
