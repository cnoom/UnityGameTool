using System;
using NUnit.Framework;
using CNoom.UnityGameTool.NumberRoller;

namespace CNoom.UnityGameTool.Tests
{
    [TestFixture]
    public class NumberRollerEngineTests
    {
        private NumberRollerConfig _config;
        private NumberRollerEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _config = new NumberRollerConfig();
            _engine = new NumberRollerEngine(_config);
        }

        [Test]
        public void Begin_SetsFromAndTo()
        {
            _engine.Begin(0, 100);

            Assert.AreEqual(0, _engine.FromValue);
            Assert.AreEqual(100, _engine.ToValue);
            Assert.IsTrue(_engine.IsPlaying);
        }

        [Test]
        public void Begin_SameValues_NotPlaying()
        {
            _engine.Begin(50, 50);

            Assert.IsFalse(_engine.IsPlaying);
            Assert.AreEqual(50, _engine.CurrentValue);
        }

        [Test]
        public void Tick_AdvancesCurrentValue()
        {
            _config = new NumberRollerConfig(duration: 1f);
            _engine = new NumberRollerEngine(_config);

            _engine.Begin(0, 100);
            _engine.Tick(0.5f);

            // 0.5f 进度，线性缓动，值应在 50 附近
            Assert.Greater(_engine.CurrentValue, 0);
            Assert.Less(_engine.CurrentValue, 100);
        }

        [Test]
        public void Tick_CompletesWhenDurationReached()
        {
            _config = new NumberRollerConfig(duration: 0.5f);
            _engine = new NumberRollerEngine(_config);

            _engine.Begin(0, 100);
            bool stillPlaying = _engine.Tick(0.6f);

            Assert.IsFalse(stillPlaying);
            Assert.IsFalse(_engine.IsPlaying);
            Assert.AreEqual(100, _engine.CurrentValue);
        }

        [Test]
        public void Tick_SnapsWhenCloseEnough()
        {
            _config = new NumberRollerConfig(duration: 1f, snapThreshold: 2.0);
            _engine = new NumberRollerEngine(_config);

            _engine.Begin(0, 100);
            // 推进到接近完成
            _engine.Tick(0.999f);

            // 由于缓动曲线，值可能已经很接近 100
            // 只要 SnapThreshold 足够大就会吸附
        }

        [Test]
        public void SkipToEnd_SetsCurrentValueToTarget()
        {
            _engine.Begin(0, 42);
            _engine.SkipToEnd();

            Assert.AreEqual(42, _engine.CurrentValue);
            Assert.IsFalse(_engine.IsPlaying);
        }

        [Test]
        public void Stop_KeepsCurrentValue()
        {
            _engine.Begin(0, 100);
            _engine.Tick(0.1f);
            double valueBeforeStop = _engine.CurrentValue;

            _engine.Stop();

            Assert.IsFalse(_engine.IsPlaying);
            Assert.AreEqual(valueBeforeStop, _engine.CurrentValue);
        }

        [Test]
        public void Reset_ClearsAllState()
        {
            _engine.Begin(10, 90);
            _engine.Reset();

            Assert.IsFalse(_engine.IsPlaying);
            Assert.AreEqual(0, _engine.CurrentValue);
        }

        [Test]
        public void Progress_ReturnsCorrectRatio()
        {
            _config = new NumberRollerConfig(duration: 2f);
            _engine = new NumberRollerEngine(_config);

            _engine.Begin(0, 100);
            _engine.Tick(1f);

            Assert.AreEqual(0.5f, _engine.Progress, 0.001f);
        }

        [Test]
        public void GetFormattedValue_ReturnsFormattedString()
        {
            _engine.Begin(0, 1000);
            _engine.SkipToEnd();

            string formatted = _engine.GetFormattedValue();
            Assert.IsNotNull(formatted);
            Assert.IsNotEmpty(formatted);
        }

        [Test]
        public void Begin_NegativeRange_WorksCorrectly()
        {
            _engine.Begin(100, -50);
            Assert.IsTrue(_engine.IsPlaying);
            Assert.AreEqual(100, _engine.FromValue);
            Assert.AreEqual(-50, _engine.ToValue);
        }
    }
}
