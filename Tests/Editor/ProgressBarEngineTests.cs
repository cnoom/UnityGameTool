using System.Collections.Generic;
using NUnit.Framework;
using CNoom.UnityGameTool.ProgressBar;

namespace CNoom.UnityGameTool.Tests
{
    [TestFixture]
    public class ProgressBarEngineTests
    {
        private ProgressBarConfig _config;
        private ProgressBarEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _config = new ProgressBarConfig(
                transitionDuration: 0.3f,
                easeType: ProgressEaseType.Linear,
                enableDelayedBar: true,
                delayedWaitTime: 0.2f,
                delayedCatchUpDuration: 0.4f,
                delayedEaseType: ProgressEaseType.Linear);
            _engine = new ProgressBarEngine(_config);
        }

        [Test]
        public void SetValue_Immediate()
        {
            _engine.SetValue(0.7f);
            Assert.AreEqual(0.7f, _engine.Value, 0.001f);
            Assert.AreEqual(0.7f, _engine.DelayedValue, 0.001f);
            Assert.IsFalse(_engine.IsTransitioning);
        }

        [Test]
        public void SetValue_ClampsTo01()
        {
            _engine.SetValue(-0.5f);
            Assert.AreEqual(0f, _engine.Value, 0.001f);

            _engine.SetValue(1.5f);
            Assert.AreEqual(1f, _engine.Value, 0.001f);
        }

        [Test]
        public void BeginTransition_Increase()
        {
            _engine.SetValue(0.3f);
            _engine.BeginTransition(0.8f);

            Assert.IsTrue(_engine.IsTransitioning);
            Assert.AreEqual(0.3f, _engine.Value, 0.001f);
        }

        [Test]
        public void Tick_TransitionsToTarget()
        {
            _engine.SetValue(0f);
            _engine.BeginTransition(1f);

            // 模拟足够多帧超过过渡时间
            for (int i = 0; i < 60; i++)
            {
                _engine.Tick(0.016f);
            }

            Assert.AreEqual(1f, _engine.Value, 0.01f);
            Assert.IsFalse(_engine.IsTransitioning);
        }

        [Test]
        public void Tick_DelayedBar_WaitsThenCatchesUp()
        {
            _engine.SetValue(1f);
            _engine.BeginTransition(0.5f);

            // 前 0.2 秒延迟条应保持不动
            for (int i = 0; i < 12; i++)
            {
                _engine.Tick(0.016f);
            }

            // 延迟条应仍接近原始值
            Assert.Greater(_engine.DelayedValue, 0.8f, "延迟条应仍在等待");

            // 继续推进，延迟条应开始追赶
            for (int i = 0; i < 60; i++)
            {
                _engine.Tick(0.016f);
            }

            Assert.AreEqual(0.5f, _engine.DelayedValue, 0.05f, "延迟条应追赶到位");
        }

        [Test]
        public void Tick_DelayedBar_FollowsOnIncrease()
        {
            _engine.SetValue(0.3f);
            _engine.BeginTransition(0.8f);

            // 增加时延迟条直接跟随
            Assert.AreEqual(0.8f, _engine.DelayedValue, 0.001f);
        }

        [Test]
        public void Stop_KeepsCurrentValue()
        {
            _engine.SetValue(0f);
            _engine.BeginTransition(1f);

            _engine.Tick(0.05f);
            _engine.Stop();

            Assert.IsFalse(_engine.IsTransitioning);
            float valueAfterStop = _engine.Value;
            Assert.Greater(valueAfterStop, 0f);
            Assert.Less(valueAfterStop, 1f);
        }

        [Test]
        public void Reset_ClearsAll()
        {
            _engine.SetValue(0.8f);
            _engine.BeginTransition(0.2f);
            _engine.Reset();

            Assert.AreEqual(0f, _engine.Value, 0.001f);
            Assert.AreEqual(0f, _engine.DelayedValue, 0.001f);
            Assert.IsFalse(_engine.IsTransitioning);
        }

        [Test]
        public void Tick_CompletesTransition()
        {
            _engine.SetValue(0f);
            _engine.BeginTransition(1f);

            bool valueCompleted = false;
            for (int i = 0; i < 100; i++)
            {
                var frameData = _engine.Tick(0.016f);
                if (frameData.ValueTransitionComplete)
                {
                    valueCompleted = true;
                }
            }

            Assert.IsTrue(valueCompleted, "主进度条应完成过渡");
        }
    }
}
