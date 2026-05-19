using System;
using System.Collections.Generic;

namespace CNoom.UnityGameTool.ScreenFlash
{
    /// <summary>
    /// 屏幕特效每帧计算结果，由 Engine 产出，供 Driver 应用到全屏覆盖层。
    /// </summary>
    public struct ScreenFlashFrameData
    {
        /// <summary>当前 Alpha 值 0~1（所有实例叠加后）</summary>
        public float Alpha;

        /// <summary>叠加颜色 R 分量</summary>
        public float R;

        /// <summary>叠加颜色 G 分量</summary>
        public float G;

        /// <summary>叠加颜色 B 分量</summary>
        public float B;
    }

    /// <summary>
    /// 单个闪烁实例的运行时状态。
    /// </summary>
    internal class FlashInstance
    {
        public int Id;
        public ScreenFlashConfig Config;
        public float Elapsed;
        public bool IsActive;
        public int PulseIndex;
        public float PulseElapsed;
    }

    /// <summary>
    /// 屏幕特效纯逻辑引擎。管理多实例的生命周期和 Alpha 计算，
    /// 使用颜色加权叠加混合多个特效，不依赖 Unity 组件，完全可单元测试。
    /// </summary>
    public class ScreenFlashEngine
    {
        private readonly ScreenFlashConfig _defaultConfig;
        private readonly List<FlashInstance> _instances = new List<FlashInstance>();
        private readonly List<int> _completedThisFrame = new List<int>();
        private int _nextId = 1;

        /// <summary>是否有活跃特效</summary>
        public bool IsPlaying => _instances.Count > 0;

        /// <summary>活跃特效数量</summary>
        public int ActiveCount => _instances.Count;

        /// <summary>
        /// 创建屏幕特效引擎。
        /// </summary>
        /// <param name="defaultConfig">默认闪烁配置</param>
        public ScreenFlashEngine(ScreenFlashConfig defaultConfig)
        {
            _defaultConfig = defaultConfig ?? throw new ArgumentNullException(nameof(defaultConfig));
        }

        /// <summary>
        /// 添加一个闪烁实例。
        /// </summary>
        /// <param name="config">闪烁配置，null 则使用默认配置</param>
        /// <returns>特效实例 ID</returns>
        public int AddFlash(ScreenFlashConfig config)
        {
            var cfg = config ?? _defaultConfig;
            int id = _nextId++;
            var instance = new FlashInstance
            {
                Id = id,
                Config = cfg,
                Elapsed = 0f,
                IsActive = true,
                PulseIndex = 0,
                PulseElapsed = 0f
            };
            _instances.Add(instance);
            return id;
        }

        /// <summary>
        /// 推进一帧，计算所有活跃特效叠加后的颜色和 Alpha。
        /// </summary>
        /// <param name="deltaTime">帧间隔（秒）</param>
        /// <returns>叠加后的帧数据</returns>
        public ScreenFlashFrameData Tick(float deltaTime)
        {
            _completedThisFrame.Clear();

            float totalAlpha = 0f;
            float weightedR = 0f;
            float weightedG = 0f;
            float weightedB = 0f;

            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                var inst = _instances[i];
                if (!inst.IsActive)
                {
                    _instances.RemoveAt(i);
                    continue;
                }

                float alpha = ComputeFlashAlpha(inst, deltaTime);

                // 检查单次闪烁是否完成
                if (inst.Config.Mode == ScreenFlashMode.Flash)
                {
                    if (inst.Elapsed >= inst.Config.SingleFlashDuration)
                    {
                        inst.IsActive = false;
                        _completedThisFrame.Add(inst.Id);
                        _instances.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    // Pulse 模式：检查是否完成所有脉冲
                    if (inst.Config.PulseCount > 0 && inst.PulseIndex >= inst.Config.PulseCount)
                    {
                        // 确保最后一个脉冲的淡出完成
                        float lastFadeOutEnd = inst.Config.SingleFlashDuration;
                        if (inst.Elapsed >= lastFadeOutEnd)
                        {
                            inst.IsActive = false;
                            _completedThisFrame.Add(inst.Id);
                            _instances.RemoveAt(i);
                            continue;
                        }
                    }
                }

                // 加权颜色叠加
                if (alpha > 0f)
                {
                    totalAlpha = Math.Min(1f, totalAlpha + alpha);
                    weightedR += inst.Config.R * alpha;
                    weightedG += inst.Config.G * alpha;
                    weightedB += inst.Config.B * alpha;
                }
            }

            // 归一化颜色
            var frameData = new ScreenFlashFrameData
            {
                Alpha = totalAlpha
            };

            if (totalAlpha > 0f)
            {
                frameData.R = Math.Min(1f, weightedR / totalAlpha);
                frameData.G = Math.Min(1f, weightedG / totalAlpha);
                frameData.B = Math.Min(1f, weightedB / totalAlpha);
            }

            return frameData;
        }

        /// <summary>
        /// 获取本帧已完成的特效 ID 列表。
        /// </summary>
        public void GetCompletedIds(List<int> completedIds)
        {
            if (completedIds == null)
                throw new ArgumentNullException(nameof(completedIds));
            completedIds.AddRange(_completedThisFrame);
        }

        /// <summary>
        /// 查询指定 ID 的特效是否仍在活跃。
        /// </summary>
        public bool IsActive(int flashId)
        {
            for (int i = 0; i < _instances.Count; i++)
            {
                if (_instances[i].Id == flashId)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 停止所有特效。
        /// </summary>
        public void StopAll()
        {
            _instances.Clear();
        }

        /// <summary>
        /// 停止指定 ID 的特效。
        /// </summary>
        public bool Stop(int flashId)
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                if (_instances[i].Id == flashId)
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
        /// 计算单个特效实例的当前 Alpha。
        /// </summary>
        private float ComputeFlashAlpha(FlashInstance inst, float deltaTime)
        {
            inst.Elapsed += deltaTime;
            var cfg = inst.Config;

            if (cfg.Mode == ScreenFlashMode.Flash)
            {
                return ComputeSingleFlashAlpha(inst.Elapsed, cfg);
            }

            // Pulse 模式
            float singleDur = cfg.SingleFlashDuration;
            float totalCycleDur = singleDur + cfg.PulseInterval;

            // 当前脉冲内的局部时间
            float localTime = inst.Elapsed - inst.PulseIndex * totalCycleDur;

            // 是否需要推进到下一个脉冲
            if (localTime >= totalCycleDur)
            {
                inst.PulseIndex++;
                localTime = inst.Elapsed - inst.PulseIndex * totalCycleDur;

                // 检查是否超过最大脉冲次数
                if (cfg.PulseCount > 0 && inst.PulseIndex >= cfg.PulseCount)
                {
                    return 0f;
                }
            }

            // 在当前脉冲的闪烁时间内
            if (localTime < singleDur)
            {
                return ComputeSingleFlashAlpha(localTime, cfg);
            }

            // 在脉冲间隔期间
            return 0f;
        }

        /// <summary>
        /// 计算单次闪烁周期内的 Alpha 值。
        /// </summary>
        private static float ComputeSingleFlashAlpha(float elapsed, ScreenFlashConfig cfg)
        {
            float fadeIn = cfg.FadeInDuration;
            float hold = cfg.HoldDuration;
            float fadeOut = cfg.FadeOutDuration;
            float maxAlpha = cfg.MaxAlpha;

            if (elapsed < fadeIn)
            {
                // 淡入阶段
                float t = fadeIn > 0f ? elapsed / fadeIn : 1f;
                return maxAlpha * t;
            }

            if (elapsed < fadeIn + hold)
            {
                // 停留阶段
                return maxAlpha;
            }

            if (elapsed < fadeIn + hold + fadeOut)
            {
                // 淡出阶段
                float fadeT = fadeOut > 0f ? (elapsed - fadeIn - hold) / fadeOut : 1f;
                return maxAlpha * (1f - fadeT);
            }

            return 0f;
        }

        #endregion
    }
}
