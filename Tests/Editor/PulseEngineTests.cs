using NUnit.Framework;
using CNoom.UnityGameTool.Pulse;

namespace CNoom.UnityGameTool.Tests
{
    [TestFixture]
    public class PulseEngineTests
    {
        private PulseEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _engine = new PulseEngine();
        }

        [Test]
        public void Begin_SetsIsPlaying()
        {
            var config = new PulseConfig(PulseType.Scale, PulseEaseType.Sine, period: 1f);
            _engine.Begin(config);
            Assert.IsTrue(_engine.IsPlaying);
            Assert.IsFalse(_engine.IsPaused);
        }

        [Test]
        public void Tick_ScaleType_ReturnsScaleValues()
        {
            var config = new PulseConfig(
                type: PulseType.Scale,
                easeType: PulseEaseType.Linear,
                period: 1f,
                minScale: 0.8f,
                maxScale: 1.2f);
            _engine.Begin(config);

            // 在周期中点应返回最大缩放
            var data = _engine.Tick(0.5f);
            Assert.AreEqual(1.2f, data.Scale, 0.01f);

            // 在周期结束应返回最小缩放
            data = _engine.Tick(0.5f);
            Assert.AreEqual(0.8f, data.Scale, 0.01f);
        }

        [Test]
        public void Tick_GlowType_ReturnsAlphaValues()
        {
            var config = new PulseConfig(
                type: PulseType.Glow,
                easeType: PulseEaseType.Linear,
                period: 1f,
                minAlpha: 0.2f,
                maxAlpha: 1f);
            _engine.Begin(config);

            // 在周期中点（phase=0.5）应返回最大 Alpha
            var data = _engine.Tick(0.5f);
            Assert.AreEqual(1f, data.Alpha, 0.01f);
        }

        [Test]
        public void Tick_FloatType_ReturnsYOffset()
        {
            var config = new PulseConfig(
                type: PulseType.Float,
                easeType: PulseEaseType.Linear,
                period: 1f,
                floatAmplitude: 10f);
            _engine.Begin(config);

            // phase=0.125 → pingpong=0.25 → normalizedOffset=-0.5 → offset=-5
            var data = _engine.Tick(0.125f);
            Assert.AreNotEqual(0f, data.YOffset, "浮动类型应产生非零偏移");
        }

        [Test]
        public void Tick_NonLooping_CompletesAfterDuration()
        {
            var config = new PulseConfig(
                type: PulseType.Scale,
                period: 1f,
                isLooping: false,
                duration: 0.5f);
            _engine.Begin(config);

            var data = _engine.Tick(0.6f);

            Assert.IsTrue(data.Completed);
            Assert.IsFalse(_engine.IsPlaying);
        }

        [Test]
        public void Tick_Looping_NeverCompletes()
        {
            var config = new PulseConfig(
                type: PulseType.Scale,
                period: 0.5f,
                isLooping: true);
            _engine.Begin(config);

            for (int i = 0; i < 200; i++)
            {
                var data = _engine.Tick(0.016f);
                Assert.IsFalse(data.Completed, "循环模式不应完成");
            }

            Assert.IsTrue(_engine.IsPlaying);
        }

        [Test]
        public void Pause_StopsTickUpdate()
        {
            var config = new PulseConfig(PulseType.Scale, period: 1f);
            _engine.Begin(config);

            _engine.Pause();
            Assert.IsTrue(_engine.IsPaused);

            // Tick 在暂停时不应更新
            var data = _engine.Tick(0.5f);
            Assert.AreEqual(0f, data.Scale);
        }

        [Test]
        public void Resume_ContinuesPlayback()
        {
            var config = new PulseConfig(PulseType.Scale, period: 1f);
            _engine.Begin(config);
            _engine.Pause();
            _engine.Resume();

            Assert.IsFalse(_engine.IsPaused);
            var data = _engine.Tick(0.016f);
            Assert.IsTrue(_engine.IsPlaying);
        }

        [Test]
        public void Stop_StopsPlayback()
        {
            var config = new PulseConfig(PulseType.Scale, period: 1f);
            _engine.Begin(config);
            _engine.Stop();

            Assert.IsFalse(_engine.IsPlaying);
        }

        [Test]
        public void Reset_ClearsState()
        {
            var config = new PulseConfig(PulseType.Scale, period: 1f);
            _engine.Begin(config);
            _engine.Tick(0.5f);
            _engine.Reset();

            Assert.IsFalse(_engine.IsPlaying);
            Assert.IsFalse(_engine.IsPaused);
        }

        [Test]
        public void SkipToEnd_StopsAndCompletes()
        {
            var config = new PulseConfig(PulseType.Scale, period: 1f);
            _engine.Begin(config);
            _engine.SkipToEnd();

            Assert.IsFalse(_engine.IsPlaying);
        }
    }
}
