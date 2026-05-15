using System;

namespace CNoom.UnityGameTool.FloatingText
{
    /// <summary>
    /// 飘字播放完成事件处理器
    /// </summary>
    /// <param name="instanceId">飘字实例 ID</param>
    public delegate void FloatingTextCompleteHandler(int instanceId);

    /// <summary>
    /// 飘字管理器接口，定义飘字生成的核心行为契约。
    /// 支持多实例同时飘浮。
    /// </summary>
    public interface IFloatingTextSpawner
    {
        /// <summary>当前活跃飘字数量</summary>
        int ActiveCount { get; }

        /// <summary>飘字播放完成事件</summary>
        event FloatingTextCompleteHandler OnComplete;

        /// <summary>
        /// 生成一条飘字。
        /// </summary>
        /// <param name="request">飘字请求参数</param>
        /// <returns>飘字实例 ID，用于跟踪</returns>
        int Spawn(FloatingTextRequest request);

        /// <summary>
        /// 立即清除所有飘字。
        /// </summary>
        void Clear();
    }
}
