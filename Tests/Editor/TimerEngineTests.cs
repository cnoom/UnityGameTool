using System;
using NUnit.Framework;
using CNoom.UnityGameTool.Timer;

namespace CNoom.UnityGameTool.Tests
{
    [TestFixture]
    public class TimerEngineTests
    {
        private TimerConfig _config;
        private TimerEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _config = new TimerConfig();
            _engine = new TimerEngine(_config);
        }

        [Test]
        public void BeginCountdown_ZeroDuration_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _engine.BeginCountdown(0f));
        }

        [Test]
        public void BeginCountdown_NegativeDuration_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _engine.BeginCountdown(-1f));
        }

        [Test]
        public void BeginCountdown_Valid_SetsRunningState()
        {
            _engine.BeginCountdown(10f);

            Assert.IsTrue(_engine.IsRunning);
            Assert.AreEqual(TimerState.Running, _engine.State);
            Assert.AreEqual(TimerMode.Countdown, _engine.Mode);
            Assert.AreEqual(10f, _engine.Remaining, 0.001f);
        }

        [Test]
        public void Tick_Countdown_DecreasesRemaining()
        {
            _engine.BeginCountdown(5f);
            _engine.Tick(1f);

            Assert.AreEqual(4f, _engine.Remaining, 0.001f);
            Assert.IsTrue(_engine.IsRunning);
        }

        [Test]
        public void Tick_Countdown_CompletesWhenTimeIsUp()
        {
            _engine.BeginCountdown(1f);
            var result = _engine.Tick(1f);

            Assert.IsTrue(result.Completed);
            Assert.AreEqual(TimerState.Completed, _engine.State);
            Assert.AreEqual(0f, _engine.Remaining, 0.001f);
        }

        [Test]
        public void Tick_NotRunning_ReturnsDefault()
        {
            var result = _engine.Tick(1f);
            Assert.IsFalse(result.Ticked);
            Assert.IsFalse(result.Completed);
        }

        [Test]
        public void Pause_WhenRunning_SetsPausedState()
        {
            _engine.BeginCountdown(10f);
            _engine.Pause();

            Assert.IsTrue(_engine.IsPaused);
            Assert.IsFalse(_engine.IsRunning);
        }

        [Test]
        public void Resume_WhenPaused_SetsRunningState()
        {
            _engine.BeginCountdown(10f);
            _engine.Pause();
            _engine.Resume();

            Assert.IsTrue(_engine.IsRunning);
            Assert.IsFalse(_engine.IsPaused);
        }

        [Test]
        public void BeginStopwatch_SetsStopwatchMode()
        {
            _engine.BeginStopwatch();

            Assert.IsTrue(_engine.IsRunning);
            Assert.AreEqual(TimerMode.Stopwatch, _engine.Mode);
            Assert.AreEqual(-1f, _engine.Remaining);
        }

        [Test]
        public void Tick_Stopwatch_IncreasesElapsed()
        {
            _engine.BeginStopwatch();
            _engine.Tick(2.5f);

            Assert.AreEqual(2.5f, _engine.Elapsed, 0.001f);
        }

        [Test]
        public void Tick_Stopwatch_WithMax_CompletesAtMax()
        {
            _engine.BeginStopwatch(5f);
            var result = _engine.Tick(5f);

            Assert.IsTrue(result.Completed);
            Assert.AreEqual(TimerState.Completed, _engine.State);
        }

        [Test]
        public void Progress_Countdown_ReturnsCorrectRatio()
        {
            _engine.BeginCountdown(10f);
            _engine.Tick(3f);

            Assert.AreEqual(0.3f, _engine.Progress, 0.001f);
        }

        [Test]
        public void SkipToEnd_SetsCompleted()
        {
            _engine.BeginCountdown(100f);
            _engine.SkipToEnd();

            Assert.AreEqual(TimerState.Completed, _engine.State);
            Assert.AreEqual(100f, _engine.Elapsed, 0.001f);
        }

        [Test]
        public void Reset_ClearsAllState()
        {
            _engine.BeginCountdown(100f);
            _engine.Reset();

            Assert.AreEqual(TimerState.Stopped, _engine.State);
            Assert.AreEqual(0f, _engine.Elapsed, 0.001f);
        }
    }
}
