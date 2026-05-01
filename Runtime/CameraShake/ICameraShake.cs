using System;

namespace CNoom.UnityGameTool.CameraShake
{
    /// <summary>
    /// 屏幕震动完成事件处理器
    /// </summary>
    /// <param name="shakeId">震动实例 ID</param>
    public delegate void ShakeCompleteHandler(int shakeId);

    /// <summary>
    /// 屏幕震动接口，定义相机震动的核心行为契约。
    /// 支持多震源同时叠加。
    /// </summary>
    public interface ICameraShake
    {
        /// <summary>是否正在震动中</summary>
        bool IsShaking { get; }

        /// <summary>当前活跃震动源数量</summary>
        int ActiveShakeCount { get; }

        /// <summary>震动实例完成时触发</summary>
        event ShakeCompleteHandler OnShakeComplete;

        /// <summary>
        /// 使用默认配置触发一次震动。
        /// </summary>
        /// <returns>震动实例 ID</returns>
        int Shake();

        /// <summary>
        /// 使用自定义配置触发一次震动。
        /// </summary>
        /// <param name="config">震动配置</param>
        /// <returns>震动实例 ID</returns>
        int Shake(CameraShakeConfig config);

        /// <summary>
        /// 使用指定强度和持续时间触发一次简短震动。
        /// </summary>
        /// <param name="intensity">震动强度</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <returns>震动实例 ID</returns>
        int Shake(float intensity, float duration);

        /// <summary>
        /// 停止所有震动。
        /// </summary>
        void StopAll();

        /// <summary>
        /// 停止指定 ID 的震动。
        /// </summary>
        /// <param name="shakeId">震动实例 ID</param>
        void Stop(int shakeId);
    }
}
