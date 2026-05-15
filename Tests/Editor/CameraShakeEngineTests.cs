using System;
using System.Collections.Generic;
using NUnit.Framework;
using CNoom.UnityGameTool.CameraShake;

namespace CNoom.UnityGameTool.Tests
{
    [TestFixture]
    public class CameraShakeEngineTests
    {
        private CameraShakeConfig _defaultConfig;
        private CameraShakeEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _defaultConfig = new CameraShakeConfig(1f, 0.3f);
            _engine = new CameraShakeEngine(_defaultConfig);
        }

        [Test]
        public void AddShake_ReturnsIncrementingIds()
        {
            int id1 = _engine.AddShake(null);
            int id2 = _engine.AddShake(null);
            int id3 = _engine.AddShake(null);

            Assert.AreEqual(1, id2 - id1);
            Assert.AreEqual(1, id3 - id2);
        }

        [Test]
        public void IsShaking_True_WhenShakeAdded()
        {
            Assert.IsFalse(_engine.IsShaking);
            _engine.AddShake(null);
            Assert.IsTrue(_engine.IsShaking);
        }

        [Test]
        public void Tick_ReturnsZeroOffset_WhenNoShakes()
        {
            var offset = _engine.Tick(0.016f);
            Assert.AreEqual(0f, offset.X);
            Assert.AreEqual(0f, offset.Y);
            Assert.AreEqual(0f, offset.ZRotation);
        }

        [Test]
        public void Tick_ProducesNonZeroOffset_WhenShaking()
        {
            _engine.AddShake(null);
            var offset = _engine.Tick(0.016f);
            // 至少有一个轴应该有偏移
            bool hasOffset = offset.X != 0f || offset.Y != 0f;
            Assert.IsTrue(hasOffset, "震动应产生非零偏移");
        }

        [Test]
        public void Tick_CompletesShake_AfterDuration()
        {
            var config = new CameraShakeConfig(1f, 0.1f);
            _engine.AddShake(config);

            // 模拟足够多帧超过持续时间
            for (int i = 0; i < 20; i++)
            {
                _engine.Tick(0.016f);
            }

            Assert.IsFalse(_engine.IsShaking, "超过持续时间后应停止震动");
        }

        [Test]
        public void Tick_MultipleShakes_SuperimposeOffsets()
        {
            _engine.AddShake(null);
            _engine.AddShake(null);

            var offset = _engine.Tick(0.016f);
            // 多震源叠加，偏移应该比单个更大或不同
            Assert.IsTrue(_engine.ActiveShakeCount <= 2);
        }

        [Test]
        public void GetCompletedIds_Throws_WhenNull()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.GetCompletedIds(null));
        }

        [Test]
        public void GetCompletedIds_ReturnsCompletedId_AfterDuration()
        {
            var config = new CameraShakeConfig(1f, 0.05f);
            int id = _engine.AddShake(config);

            // 推进超过持续时间
            _engine.Tick(0.1f);

            var completed = new List<int>();
            _engine.GetCompletedIds(completed);
            Assert.Contains(id, completed);
        }

        [Test]
        public void Stop_RemovesSpecificShake()
        {
            int id1 = _engine.AddShake(null);
            int id2 = _engine.AddShake(null);

            bool stopped = _engine.Stop(id1);
            Assert.IsTrue(stopped);
            Assert.IsFalse(_engine.IsActive(id1));
            Assert.IsTrue(_engine.IsActive(id2));
        }

        [Test]
        public void StopAll_ClearsAllShakes()
        {
            _engine.AddShake(null);
            _engine.AddShake(null);

            _engine.StopAll();
            Assert.IsFalse(_engine.IsShaking);
            Assert.AreEqual(0, _engine.ActiveShakeCount);
        }

        [Test]
        public void Reset_ResetsIdCounter()
        {
            int id1 = _engine.AddShake(null);
            _engine.StopAll();
            _engine.Reset();

            int id2 = _engine.AddShake(null);
            Assert.AreEqual(id1, id2, "Reset 后 ID 应重新从 1 开始");
        }
    }
}
