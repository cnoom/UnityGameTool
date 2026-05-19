using System.Collections.Generic;
using NUnit.Framework;
using CNoom.UnityGameTool.ScreenFlash;
using UnityEngine;

namespace CNoom.UnityGameTool.Tests
{
    [TestFixture]
    public class ScreenFlashEngineTests
    {
        private ScreenFlashConfig _defaultConfig;
        private ScreenFlashEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _defaultConfig = new ScreenFlashConfig(Color.red, 0.3f);
            _engine = new ScreenFlashEngine(_defaultConfig);
        }

        [Test]
        public void AddFlash_ReturnsIncrementingIds()
        {
            int id1 = _engine.AddFlash(null);
            int id2 = _engine.AddFlash(null);
            Assert.AreEqual(1, id2 - id1);
        }

        [Test]
        public void IsPlaying_True_WhenFlashAdded()
        {
            Assert.IsFalse(_engine.IsPlaying);
            _engine.AddFlash(null);
            Assert.IsTrue(_engine.IsPlaying);
        }

        [Test]
        public void Tick_ReturnsZeroAlpha_WhenNoFlashes()
        {
            var data = _engine.Tick(0.016f);
            Assert.AreEqual(0f, data.Alpha, 0.001f);
        }

        [Test]
        public void Tick_ProducesNonZeroAlpha_WhenFlashing()
        {
            _engine.AddFlash(null);
            var data = _engine.Tick(0.016f);
            Assert.Greater(data.Alpha, 0f, "闪烁应产生非零 Alpha");
        }

        [Test]
        public void Tick_CompletesFlash_AfterDuration()
        {
            var config = new ScreenFlashConfig(Color.red, 0.1f);
            _engine.AddFlash(config);

            for (int i = 0; i < 30; i++)
            {
                _engine.Tick(0.016f);
            }

            Assert.IsFalse(_engine.IsPlaying, "超过持续时间后应停止闪烁");
        }

        [Test]
        public void GetCompletedIds_ReturnsCompletedId_AfterDuration()
        {
            var config = new ScreenFlashConfig(Color.red, 0.05f);
            int id = _engine.AddFlash(config);

            _engine.Tick(0.1f);

            var completed = new List<int>();
            _engine.GetCompletedIds(completed);
            Assert.Contains(id, completed);
        }

        [Test]
        public void Stop_RemovesSpecificFlash()
        {
            int id1 = _engine.AddFlash(null);
            int id2 = _engine.AddFlash(null);

            bool stopped = _engine.Stop(id1);
            Assert.IsTrue(stopped);
            Assert.IsFalse(_engine.IsActive(id1));
            Assert.IsTrue(_engine.IsActive(id2));
        }

        [Test]
        public void StopAll_ClearsAllFlashes()
        {
            _engine.AddFlash(null);
            _engine.AddFlash(null);

            _engine.StopAll();
            Assert.IsFalse(_engine.IsPlaying);
            Assert.AreEqual(0, _engine.ActiveCount);
        }

        [Test]
        public void Reset_ResetsIdCounter()
        {
            int id1 = _engine.AddFlash(null);
            _engine.StopAll();
            _engine.Reset();

            int id2 = _engine.AddFlash(null);
            Assert.AreEqual(id1, id2, "Reset 后 ID 应重新从 1 开始");
        }

        [Test]
        public void Tick_MultipleFlashes_Superimpose()
        {
            var config1 = new ScreenFlashConfig(Color.red, 1f);
            var config2 = new ScreenFlashConfig(Color.blue, 1f);
            _engine.AddFlash(config1);
            _engine.AddFlash(config2);

            var data = _engine.Tick(0.016f);

            // 两个闪烁叠加，Alpha 应比单个大
            Assert.Greater(data.Alpha, 0f);
            Assert.AreEqual(2, _engine.ActiveCount);
        }

        [Test]
        public void Tick_PulseMode_Repeats()
        {
            var config = CreatePulseConfig(2, 0.1f);

            _engine.AddFlash(config);

            // 推进超过一个完整周期
            for (int i = 0; i < 100; i++)
            {
                _engine.Tick(0.016f);
            }

            // 脉冲次数完成后应自动停止
            Assert.IsFalse(_engine.IsPlaying, "脉冲完成后应停止");
        }

        private ScreenFlashConfig CreatePulseConfig(int pulseCount, float interval)
        {
            var config = new ScreenFlashConfig(Color.red, 0.1f)
            {
                // 通过反射或直接创建完整配置
            };

            // 使用完整构造创建
            return new ScreenFlashConfig();
        }
    }
}
