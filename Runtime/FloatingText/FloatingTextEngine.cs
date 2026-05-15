using System;
using System.Collections.Generic;

namespace CNoom.UnityGameTool.FloatingText
{
    /// <summary>
    /// 单条飘字的动画计算结果，由 Engine 每帧产出，供 Driver 应用到 TMP 组件。
    /// </summary>
    public struct FloatingTextAnimData
    {
        /// <summary>X 轴偏移量（像素）</summary>
        public float XOffset;

        /// <summary>Y 轴偏移量（像素）</summary>
        public float YOffset;

        /// <summary>缩放系数（1.0 = 原始大小）</summary>
        public float Scale;

        /// <summary>Alpha 值（0~1，1 = 完全不透明）</summary>
        public float Alpha;
    }

    /// <summary>
    /// 单条飘字的运行时状态。
    /// </summary>
    internal class FloatingTextInstance
    {
        public int Id;
        public string Text;
        public float ScaleMultiplier;
        public bool EnableShake;
        public float StartX;
        public float StartY;
        public float Elapsed;
        public bool IsActive;
        public FloatingTextAnimData CurrentAnimData;
    }

    /// <summary>
    /// 飘字纯逻辑引擎。管理多实例的生命周期和动画计算，
    /// 不依赖任何 Unity 组件，完全可单元测试。
    /// </summary>
    public class FloatingTextEngine
    {
        private readonly FloatingTextConfig _config;
        private readonly List<FloatingTextInstance> _instances = new List<FloatingTextInstance>();
        private readonly List<int> _completedThisFrame = new List<int>();
        private int _nextId = 1;

        /// <summary>是否有活跃飘字</summary>
        public bool HasActive => _instances.Count > 0;

        /// <summary>当前活跃飘字数量</summary>
        public int ActiveCount => _instances.Count;

        /// <summary>
        /// 创建飘字引擎。
        /// </summary>
        /// <param name="config">飘字动画配置，不可为 null</param>
        public FloatingTextEngine(FloatingTextConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 添加一条飘字实例。
        /// </summary>
        /// <param name="text">显示文本</param>
        /// <param name="startX">初始 X 坐标（含随机偏移）</param>
        /// <param name="startY">初始 Y 坐标（含随机偏移）</param>
        /// <param name="scaleMultiplier">缩放倍率</param>
        /// <param name="enableShake">是否启用抖动</param>
        /// <returns>飘字实例 ID</returns>
        public int Add(string text, float startX, float startY,
            float scaleMultiplier = 1f, bool enableShake = false)
        {
            int id = _nextId++;
            var instance = new FloatingTextInstance
            {
                Id = id,
                Text = text,
                StartX = startX,
                StartY = startY,
                ScaleMultiplier = scaleMultiplier,
                EnableShake = enableShake,
                Elapsed = 0f,
                IsActive = true,
                CurrentAnimData = default
            };
            _instances.Add(instance);
            return id;
        }

        /// <summary>
        /// 推进一帧，更新所有活跃飘字实例的动画数据。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间（秒）</param>
        /// <returns>是否有活跃实例</returns>
        public bool Tick(float deltaTime)
        {
            _completedThisFrame.Clear();

            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                var inst = _instances[i];
                if (!inst.IsActive)
                {
                    continue;
                }

                inst.Elapsed += deltaTime;

                float total = _config.TotalDuration;
                if (inst.Elapsed >= total)
                {
                    inst.IsActive = false;
                    _completedThisFrame.Add(inst.Id);
                    continue;
                }

                inst.CurrentAnimData = ComputeAnimation(inst);
                _instances[i] = inst;
            }

            return _instances.Count > 0;
        }

        /// <summary>
        /// 获取本帧已完成的飘字实例 ID 列表。
        /// </summary>
        /// <param name="completedIds">填充已完成 ID 列表</param>
        public void GetCompletedIds(List<int> completedIds)
        {
            if (completedIds == null)
                throw new ArgumentNullException(nameof(completedIds));
            completedIds.AddRange(_completedThisFrame);
        }

        /// <summary>
        /// 移除所有已完成（非活跃）的实例。
        /// </summary>
        public void RemoveCompleted()
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                if (!_instances[i].IsActive)
                {
                    _instances.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 获取指定实例的当前动画数据。
        /// </summary>
        /// <param name="instanceId">飘字实例 ID</param>
        /// <param name="data">动画数据输出</param>
        /// <returns>是否找到活跃实例</returns>
        public bool TryGetAnimData(int instanceId, out FloatingTextAnimData data)
        {
            for (int i = 0; i < _instances.Count; i++)
            {
                if (_instances[i].Id == instanceId && _instances[i].IsActive)
                {
                    data = _instances[i].CurrentAnimData;
                    return true;
                }
            }

            data = default;
            return false;
        }

        /// <summary>
        /// 获取指定实例的文本内容。
        /// </summary>
        /// <param name="instanceId">飘字实例 ID</param>
        /// <param name="text">文本输出</param>
        /// <returns>是否找到活跃实例</returns>
        public bool TryGetText(int instanceId, out string text)
        {
            for (int i = 0; i < _instances.Count; i++)
            {
                if (_instances[i].Id == instanceId && _instances[i].IsActive)
                {
                    text = _instances[i].Text;
                    return true;
                }
            }

            text = null;
            return false;
        }

        /// <summary>
        /// 查询指定 ID 的飘字是否仍在活跃。
        /// </summary>
        /// <param name="instanceId">飘字实例 ID</param>
        /// <returns>是否仍活跃</returns>
        public bool IsActive(int instanceId)
        {
            for (int i = 0; i < _instances.Count; i++)
            {
                if (_instances[i].Id == instanceId && _instances[i].IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 停止所有飘字。
        /// </summary>
        public void StopAll()
        {
            _instances.Clear();
        }

        /// <summary>
        /// 停止指定 ID 的飘字。
        /// </summary>
        /// <param name="instanceId">飘字实例 ID</param>
        /// <returns>是否找到并停止</returns>
        public bool Stop(int instanceId)
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                if (_instances[i].Id == instanceId)
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
        /// 计算单条飘字的当前帧动画数据。
        /// </summary>
        private FloatingTextAnimData ComputeAnimation(FloatingTextInstance inst)
        {
            float t = inst.Elapsed;
            float popIn = _config.PopInDuration;
            float hold = _config.HoldDuration;
            float fadeOut = _config.FadeOutDuration;
            float total = popIn + hold + fadeOut;

            float yOffset, scale, alpha;

            if (t < popIn)
            {
                // 阶段 1: PopIn — 弹性缩放 + 渐显 + 加速上浮
                float p = t / popIn;
                scale = ElasticOut(p, _config.PopInOvershoot) * inst.ScaleMultiplier;
                alpha = Math.Min(1f, p * 3f);
                yOffset = _config.FloatSpeed * t * p;
            }
            else if (t < popIn + hold)
            {
                // 阶段 2: Hold — 保持缩放 + 匀速上浮（含重力）
                float holdT = t - popIn;
                scale = inst.ScaleMultiplier;
                alpha = 1f;
                yOffset = _config.FloatSpeed * t
                          + _config.Gravity * holdT * holdT * 0.5f;
            }
            else
            {
                // 阶段 3: FadeOut — 渐隐 + 轻微缩小 + 继续运动
                float fadeT = t - popIn - hold;
                float p = Math.Min(1f, fadeT / fadeOut);
                scale = inst.ScaleMultiplier * (1f - p * 0.3f);
                alpha = 1f - p;
                float holdT = t - popIn;
                yOffset = _config.FloatSpeed * t
                          + _config.Gravity * holdT * holdT * 0.5f;
            }

            // 抖动叠加
            float xOffset = 0f;
            if (inst.EnableShake && total > 0f)
            {
                float decay = 1f - t / total;
                xOffset = (float)Math.Sin(t * _config.ShakeFrequency * Math.PI * 2f)
                          * _config.ShakeAmplitude
                          * decay;
            }

            return new FloatingTextAnimData
            {
                XOffset = xOffset,
                YOffset = yOffset,
                Scale = scale,
                Alpha = alpha
            };
        }

        /// <summary>
        /// 弹性缓出曲线。从 0 到 overshoot 再回到 1。
        /// </summary>
        private static float ElasticOut(float t, float overshoot)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            // 简化弹性：在 t=0.5 附近达到 overshoot，然后回落到 1
            float p = 0.3f;
            float s = p / 4f;
            float inv = 1f / overshoot;
            return (float)(overshoot * Math.Pow(2, -10 * t)
                           * Math.Sin((t - s) * (2 * Math.PI) / p)
                           + 1f) * inv + (1f - inv);
        }

        #endregion
    }
}
