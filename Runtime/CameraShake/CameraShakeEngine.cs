using System;
using System.Collections.Generic;

namespace CNoom.UnityGameTool.CameraShake
{
    /// <summary>
    /// 单次震动的偏移结果，由 Engine 每帧产出，供 Driver 应用到 Transform。
    /// </summary>
    public struct ShakeOffset
    {
        /// <summary>X 轴偏移</summary>
        public float X;
        /// <summary>Y 轴偏移</summary>
        public float Y;
        /// <summary>Z 轴旋转偏移（度）</summary>
        public float ZRotation;
    }

    /// <summary>
    /// 单个震动源的运行时状态。
    /// </summary>
    internal class ShakeInstance
    {
        public int Id;
        public CameraShakeConfig Config;
        public float Elapsed;
        public float SeedX;
        public float SeedY;
        public float SeedZRot;
        public bool IsActive;
        public bool Completed;
    }

    /// <summary>
    /// 屏幕震动纯逻辑引擎。管理多震源的生命周期和叠加计算，
    /// 使用伪随机实现类似 Perlin 的平滑噪声，不依赖 Unity 组件，完全可单元测试。
    /// </summary>
    public class CameraShakeEngine
    {
        private readonly CameraShakeConfig _defaultConfig;
        private readonly List<ShakeInstance> _instances = new List<ShakeInstance>();
        private readonly List<int> _completedThisFrame = new List<int>();
        private int _nextId = 1;

        /// <summary>是否正在震动中</summary>
        public bool IsShaking => _instances.Count > 0;

        /// <summary>当前活跃震动源数量</summary>
        public int ActiveShakeCount => _instances.Count;

        /// <summary>
        /// 创建屏幕震动引擎。
        /// </summary>
        /// <param name="defaultConfig">默认震动配置（Shake() 无参时使用）</param>
        public CameraShakeEngine(CameraShakeConfig defaultConfig)
        {
            _defaultConfig = defaultConfig ?? throw new ArgumentNullException(nameof(defaultConfig));
        }

        /// <summary>
        /// 添加一个新震动源。
        /// </summary>
        /// <param name="config">震动配置，null 则使用默认配置</param>
        /// <returns>震动实例 ID</returns>
        public int AddShake(CameraShakeConfig config)
        {
            var cfg = config ?? _defaultConfig;
            var instance = new ShakeInstance
            {
                Id = _nextId++,
                Config = cfg,
                Elapsed = 0f,
                SeedX = _nextId * 1.37f + 0.5f,
                SeedY = _nextId * 2.73f + 1.3f,
                SeedZRot = _nextId * 3.91f + 2.1f,
                IsActive = true,
                Completed = false
            };
            _instances.Add(instance);
            return instance.Id;
        }

        /// <summary>
        /// 推进一帧，计算所有活跃震源叠加后的总偏移。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒）</param>
        /// <returns>叠加后的震动偏移</returns>
        public ShakeOffset Tick(float deltaTime)
        {
            var totalOffset = new ShakeOffset();
            _completedThisFrame.Clear();

            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                var inst = _instances[i];
                if (!inst.IsActive)
                {
                    _instances.RemoveAt(i);
                    continue;
                }

                inst.Elapsed += deltaTime;
                float t = inst.Config.Duration > 0f
                    ? inst.Elapsed / inst.Config.Duration
                    : 1f;

                // 超时则标记完成
                if (t >= 1f)
                {
                    inst.IsActive = false;
                    inst.Completed = true;
                    _completedThisFrame.Add(inst.Id);
                    _instances.RemoveAt(i);
                    continue;
                }

                // 衰减因子
                float decay = ComputeDecay(t, inst.Config.DecayType);

                // 计算当前帧的噪声偏移
                float freq = inst.Config.Frequency;
                float intensity = inst.Config.Intensity * decay;

                // 平滑伪随机（类似 Perlin）
                float phase = inst.Elapsed * freq;
                float nx = SmoothNoise(inst.SeedX, phase);
                float ny = SmoothNoise(inst.SeedY, phase);

                totalOffset.X += nx * intensity * inst.Config.XInfluence;
                totalOffset.Y += ny * intensity * inst.Config.YInfluence;

                if (inst.Config.ZRotationInfluence > 0f)
                {
                    float nz = SmoothNoise(inst.SeedZRot, phase);
                    totalOffset.ZRotation += nz * intensity * inst.Config.ZRotationInfluence;
                }
            }

            return totalOffset;
        }

        /// <summary>
        /// 获取本帧已完成的震动实例 ID 列表（由 Tick 填充）。
        /// </summary>
        /// <param name="completedIds">填充已完成 ID 列表</param>
        public void GetCompletedIds(List<int> completedIds)
        {
            if (completedIds == null) return;
            completedIds.AddRange(_completedThisFrame);
        }

        /// <summary>
        /// 查询指定 ID 的震动源是否仍在活跃列表中。
        /// </summary>
        /// <param name="shakeId">震动实例 ID</param>
        /// <returns>是否仍活跃</returns>
        public bool IsActive(int shakeId)
        {
            for (int i = 0; i < _instances.Count; i++)
            {
                if (_instances[i].Id == shakeId)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 停止所有震动。
        /// </summary>
        public void StopAll()
        {
            _instances.Clear();
        }

        /// <summary>
        /// 停止指定 ID 的震动。
        /// </summary>
        /// <param name="shakeId">震动实例 ID</param>
        /// <returns>是否找到并停止</returns>
        public bool Stop(int shakeId)
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                if (_instances[i].Id == shakeId)
                {
                    _instances.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 重置引擎。
        /// </summary>
        public void Reset()
        {
            _instances.Clear();
            _nextId = 1;
        }

        #region 内部计算

        /// <summary>
        /// 基于种子和相位的平滑噪声。返回 -1 ~ 1。
        /// 使用 sin 组合模拟 Perlin 噪声的平滑感。
        /// </summary>
        private static float SmoothNoise(float seed, float phase)
        {
            // 多频叠加产生自然感
            float n1 = (float)Math.Sin(seed * 127.1 + phase * 4.1) * 0.5f;
            float n2 = (float)Math.Sin(seed * 269.5 + phase * 2.3) * 0.3f;
            float n3 = (float)Math.Sin(seed * 419.2 + phase * 7.7) * 0.2f;
            return Clamp11(n1 + n2 + n3);
        }

        /// <summary>
        /// 将值限制在 -1 ~ 1 范围。
        /// </summary>
        private static float Clamp11(float v)
        {
            if (v < -1f) return -1f;
            if (v > 1f) return 1f;
            return v;
        }

        /// <summary>
        /// 计算衰减因子。
        /// </summary>
        private static float ComputeDecay(float t, ShakeDecayType decayType)
        {
            switch (decayType)
            {
                case ShakeDecayType.Linear:
                    return 1f - t;

                case ShakeDecayType.Exponential:
                    return (float)Math.Pow(1f - t, 2);

                case ShakeDecayType.None:
                    return 1f;

                default:
                    return 1f - t;
            }
        }

        #endregion
    }
}
