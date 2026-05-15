using System;
using NUnit.Framework;
using CNoom.UnityGameTool.Typewriter;

namespace CNoom.UnityGameTool.Tests
{
    [TestFixture]
    public class TypewriterEngineTests
    {
        private TypewriterConfig _config;
        private TypewriterEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _config = new TypewriterConfig();
            _engine = new TypewriterEngine(_config);
        }

        [Test]
        public void Begin_WithZeroCharacters_NotPlaying()
        {
            _engine.Begin(0);
            Assert.IsFalse(_engine.IsPlaying);
            Assert.AreEqual(0, _engine.TotalCharacters);
        }

        [Test]
        public void Begin_WithPositiveCharacters_IsPlaying()
        {
            _engine.Begin(10);
            Assert.IsTrue(_engine.IsPlaying);
            Assert.AreEqual(10, _engine.TotalCharacters);
            Assert.AreEqual(0, _engine.CurrentIndex);
        }

        [Test]
        public void Advance_IncrementsIndex()
        {
            _engine.Begin(3);
            var (hasMore, _) = _engine.Advance('a');

            Assert.AreEqual(1, _engine.CurrentIndex);
            Assert.IsTrue(hasMore);
        }

        [Test]
        public void Advance_LastChar_ReturnsHasMoreFalse()
        {
            _engine.Begin(1);
            var (hasMore, _) = _engine.Advance('a');

            Assert.IsFalse(hasMore);
            Assert.IsFalse(_engine.IsPlaying);
        }

        [Test]
        public void Advance_AllChars_CompletesSequence()
        {
            const int total = 5;
            _engine.Begin(total);

            for (int i = 0; i < total; i++)
            {
                var (hasMore, _) = _engine.Advance('x');
                if (i < total - 1)
                {
                    Assert.IsTrue(hasMore);
                }
            }

            Assert.IsFalse(_engine.IsPlaying);
            Assert.AreEqual(total, _engine.CurrentIndex);
        }

        [Test]
        public void SkipToEnd_SetsCurrentIndexToEnd()
        {
            _engine.Begin(100);
            _engine.SkipToEnd();

            Assert.AreEqual(100, _engine.CurrentIndex);
            Assert.IsFalse(_engine.IsPlaying);
        }

        [Test]
        public void Stop_KeepsCurrentProgress()
        {
            _engine.Begin(10);
            _engine.Advance('a');
            _engine.Advance('b');

            int indexBeforeStop = _engine.CurrentIndex;
            _engine.Stop();

            Assert.IsFalse(_engine.IsPlaying);
            Assert.AreEqual(indexBeforeStop, _engine.CurrentIndex);
        }

        [Test]
        public void Reset_ClearsAllState()
        {
            _engine.Begin(10);
            _engine.Reset();

            Assert.IsFalse(_engine.IsPlaying);
            Assert.AreEqual(0, _engine.CurrentIndex);
            Assert.AreEqual(0, _engine.TotalCharacters);
        }

        [Test]
        public void Progress_ReturnsCorrectRatio()
        {
            _engine.Begin(4);
            Assert.AreEqual(0f, _engine.Progress, 0.001f);

            _engine.Advance('a');
            Assert.AreEqual(0.25f, _engine.Progress, 0.001f);

            _engine.Advance('b');
            Assert.AreEqual(0.5f, _engine.Progress, 0.001f);
        }
    }
}
