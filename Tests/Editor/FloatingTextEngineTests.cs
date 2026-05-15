using System;
using System.Collections.Generic;
using NUnit.Framework;
using CNoom.UnityGameTool.FloatingText;

namespace CNoom.UnityGameTool.Tests
{
    [TestFixture]
    public class FloatingTextEngineTests
    {
        private FloatingTextConfig _config;
        private FloatingTextEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _config = new FloatingTextConfig();
            _engine = new FloatingTextEngine(_config);
        }

        [Test]
        public void Add_IncreasesActiveCount()
        {
            Assert.AreEqual(0, _engine.ActiveCount);

            _engine.Add("-42", 100f, 200f);
            Assert.AreEqual(1, _engine.ActiveCount);

            _engine.Add("+50", 150f, 250f);
            Assert.AreEqual(2, _engine.ActiveCount);
        }

        [Test]
        public void Add_ReturnsIncrementingIds()
        {
            int id1 = _engine.Add("A", 0f, 0f);
            int id2 = _engine.Add("B", 0f, 0f);
            int id3 = _engine.Add("C", 0f, 0f);

            Assert.AreEqual(1, id2 - id1);
            Assert.AreEqual(1, id3 - id2);
        }

        [Test]
        public void Tick_PopIn_ScaleGrowsFromZero()
        {
            _engine.Add("-42", 100f, 200f);

            // 初始帧：scale 应该从 0 附近开始增长
            _engine.Tick(0.001f);

            Assert.IsTrue(_engine.TryGetAnimData(1, out var data));
            Assert.Greater(data.Scale, 0f, "PopIn 阶段缩放应从 0 增长");
        }

        [Test]
        public void Tick_PopIn_AlphaGrowsFromZero()
        {
            _engine.Add("-42", 100f, 200f);

            _engine.Tick(0.001f);

            Assert.IsTrue(_engine.TryGetAnimData(1, out var data));
            Assert.Greater(data.Alpha, 0f, "PopIn 阶段透明度应从 0 增长");
        }

        [Test]
        public void Tick_Hold_ScaleMatchesMultiplier()
        {
            _engine.Add("-42", 100f, 200f, scaleMultiplier: 1.5f);

            // 推进到 Hold 阶段（跳过 PopIn）
            float popIn = _config.PopInDuration;
            _engine.Tick(popIn + 0.01f);

            Assert.IsTrue(_engine.TryGetAnimData(1, out var data));
            Assert.AreEqual(1.5f, data.Scale, 0.01f, "Hold 阶段缩放应等于缩放倍率");
        }

        [Test]
        public void Tick_Hold_AlphaIsOne()
        {
            _engine.Add("-42", 100f, 200f);

            float popIn = _config.PopInDuration;
            _engine.Tick(popIn + 0.01f);

            Assert.IsTrue(_engine.TryGetAnimData(1, out var data));
            Assert.AreEqual(1f, data.Alpha, 0.01f, "Hold 阶段透明度应为 1");
        }

        [Test]
        public void Tick_FadeOut_AlphaDecreases()
        {
            _engine.Add("-42", 100f, 200f);

            float popIn = _config.PopInDuration;
            float hold = _config.HoldDuration;
            // 推进到 FadeOut 中间
            _engine.Tick(popIn + hold + _config.FadeOutDuration * 0.5f);

            Assert.IsTrue(_engine.TryGetAnimData(1, out var data));
            Assert.Less(data.Alpha, 1f, "FadeOut 阶段透明度应小于 1");
            Assert.Greater(data.Alpha, 0f, "FadeOut 中间透明度应大于 0");
        }

        [Test]
        public void Tick_ExceedsTotalDuration_MarksCompleted()
        {
            _engine.Add("-42", 100f, 200f);

            // 推进超过总时长
            float total = _config.TotalDuration;
            for (int i = 0; i < 100; i++)
            {
                _engine.Tick(0.016f);
            }

            Assert.IsFalse(_engine.IsActive(1), "超过总时长后应标记为完成");

            var completed = new List<int>();
            _engine.GetCompletedIds(completed);
            Assert.Contains(1, completed);
        }

        [Test]
        public void Tick_ShakeEnabled_HasXOffset()
        {
            _engine.Add("-128", 100f, 200f, enableShake: true);

            // 多帧推进让抖动产生偏移
            bool hasXOffset = false;
            for (int i = 0; i < 20; i++)
            {
                _engine.Tick(0.016f);
                if (_engine.TryGetAnimData(1, out var data) && data.XOffset != 0f)
                {
                    hasXOffset = true;
                    break;
                }
            }

            Assert.IsTrue(hasXOffset, "启用抖动后应有 X 偏移");
        }

        [Test]
        public void Tick_ShakeDisabled_NoXOffset()
        {
            _engine.Add("-42", 100f, 200f, enableShake: false);

            for (int i = 0; i < 20; i++)
            {
                _engine.Tick(0.016f);
                if (_engine.TryGetAnimData(1, out var data))
                {
                    Assert.AreEqual(0f, data.XOffset, "未启用抖动时 X 偏移应为 0");
                }
            }
        }

        [Test]
        public void Tick_ScaleMultiplier_AppliesToScale()
        {
            float mult = 2f;
            _engine.Add("BIG", 100f, 200f, scaleMultiplier: mult);

            // Hold 阶段缩放应等于倍率
            float popIn = _config.PopInDuration;
            _engine.Tick(popIn + 0.01f);

            Assert.IsTrue(_engine.TryGetAnimData(1, out var data));
            Assert.AreEqual(mult, data.Scale, 0.01f);
        }

        [Test]
        public void Tick_YOffset_GrowsOverTime()
        {
            _engine.Add("-42", 100f, 200f);

            _engine.Tick(0.05f);
            Assert.IsTrue(_engine.TryGetAnimData(1, out var early));
            float earlyY = early.YOffset;

            _engine.Tick(0.1f);
            Assert.IsTrue(_engine.TryGetAnimData(1, out var late));
            float lateY = late.YOffset;

            Assert.Greater(lateY, earlyY, "Y 偏移应随时间增长（上浮）");
        }

        [Test]
        public void Tick_MultipleInstances_IndependentTimelines()
        {
            _engine.Add("A", 0f, 0f);
            _engine.Add("B", 100f, 100f);

            _engine.Tick(0.1f);

            Assert.IsTrue(_engine.TryGetAnimData(1, out var dataA));
            Assert.IsTrue(_engine.TryGetAnimData(2, out var dataB));

            // 两个实例的动画数据可能不同，但都应有效
            Assert.Greater(dataA.Scale, 0f);
            Assert.Greater(dataB.Scale, 0f);
            Assert.AreEqual(2, _engine.ActiveCount);
        }

        [Test]
        public void RemoveCompleted_ClearsFinishedInstances()
        {
            _engine.Add("-42", 0f, 0f);

            // 推进到结束
            for (int i = 0; i < 100; i++)
            {
                _engine.Tick(0.016f);
            }

            // 已完成的实例仍在列表中（非活跃）
            Assert.AreEqual(1, _engine.ActiveCount); // 计算的是总数
            Assert.IsFalse(_engine.IsActive(1));

            _engine.RemoveCompleted();
            Assert.AreEqual(0, _engine.ActiveCount);
        }

        [Test]
        public void Stop_RemovesSpecificInstance()
        {
            int id1 = _engine.Add("A", 0f, 0f);
            int id2 = _engine.Add("B", 0f, 0f);

            bool stopped = _engine.Stop(id1);
            Assert.IsTrue(stopped);
            Assert.IsFalse(_engine.IsActive(id1));
            Assert.IsTrue(_engine.IsActive(id2));
        }

        [Test]
        public void StopAll_ClearsAllInstances()
        {
            _engine.Add("A", 0f, 0f);
            _engine.Add("B", 0f, 0f);

            _engine.StopAll();
            Assert.IsFalse(_engine.HasActive);
            Assert.AreEqual(0, _engine.ActiveCount);
        }

        [Test]
        public void Reset_ResetsIdCounter()
        {
            int id1 = _engine.Add("A", 0f, 0f);
            _engine.StopAll();
            _engine.Reset();

            int id2 = _engine.Add("B", 0f, 0f);
            Assert.AreEqual(id1, id2, "Reset 后 ID 应重新从 1 开始");
        }

        [Test]
        public void TryGetText_ReturnsCorrectText()
        {
            _engine.Add("Hello", 0f, 0f);
            _engine.Tick(0.01f);

            Assert.IsTrue(_engine.TryGetText(1, out var text));
            Assert.AreEqual("Hello", text);
        }

        [Test]
        public void TryGetAnimData_ReturnsFalseForUnknownId()
        {
            Assert.IsFalse(_engine.TryGetAnimData(999, out _));
        }

        [Test]
        public void GetCompletedIds_ThrowsWhenNull()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.GetCompletedIds(null));
        }
    }
}
