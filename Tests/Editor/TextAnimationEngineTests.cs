using System;
using NUnit.Framework;
using CNoom.UnityGameTool.TextAnimation;

namespace CNoom.UnityGameTool.Tests
{
    [TestFixture]
    public class TextAnimationEngineTests
    {
        private TextAnimationConfig _config;
        private TextAnimationEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _config = new TextAnimationConfig();
            _engine = new TextAnimationEngine(_config);
        }

        [Test]
        public void Begin_SetsCharCount()
        {
            _engine.Begin(10);

            Assert.IsTrue(_engine.IsPlaying);
            Assert.AreEqual(10, _engine.CharCount);
        }

        [Test]
        public void Begin_ZeroCount_NotPlaying()
        {
            _engine.Begin(0);

            Assert.IsFalse(_engine.IsPlaying);
            Assert.AreEqual(0, _engine.CharCount);
        }

        [Test]
        public void Tick_NotPlaying_ReturnsFalse()
        {
            bool result = _engine.Tick(0.016f);
            Assert.IsFalse(result);
        }

        [Test]
        public void Tick_Wave_ProducesYOffset()
        {
            _config = new TextAnimationConfig(type: TextAnimationType.Wave);
            _engine = new TextAnimationEngine(_config);
            _engine.Begin(5);

            _engine.Tick(0.1f);

            // 至少有一个字符应该有 Y 偏移
            bool hasOffset = false;
            for (int i = 0; i < _engine.CharCount; i++)
            {
                ref readonly var data = ref _engine.GetCharData(i);
                if (data.YOffset != 0f) hasOffset = true;
            }
            Assert.IsTrue(hasOffset, "Wave 动画应产生 Y 偏移");
        }

        [Test]
        public void Tick_Shake_ProducesXYOffset()
        {
            _config = new TextAnimationConfig(type: TextAnimationType.Shake);
            _engine = new TextAnimationEngine(_config);
            _engine.Begin(5);

            _engine.Tick(0.016f);
            _engine.Tick(0.016f); // 多帧让 Shake 产生随机偏移

            bool hasOffset = false;
            for (int i = 0; i < _engine.CharCount; i++)
            {
                ref readonly var data = ref _engine.GetCharData(i);
                if (data.XOffset != 0f || data.YOffset != 0f) hasOffset = true;
            }
            Assert.IsTrue(hasOffset, "Shake 动画应产生偏移");
        }

        [Test]
        public void Tick_Bounce_ProducesScale()
        {
            _config = new TextAnimationConfig(type: TextAnimationType.Bounce);
            _engine = new TextAnimationEngine(_config);
            _engine.Begin(3);

            _engine.Tick(0.1f);

            bool hasScaleChange = false;
            for (int i = 0; i < _engine.CharCount; i++)
            {
                ref readonly var data = ref _engine.GetCharData(i);
                if (data.Scale != 1f) hasScaleChange = true;
            }
            Assert.IsTrue(hasScaleChange, "Bounce 动画应改变缩放");
        }

        [Test]
        public void Tick_Fade_ProducesAlphaChange()
        {
            _config = new TextAnimationConfig(
                type: TextAnimationType.Fade,
                fadeDuration: 0.5f,
                duration: 1f);
            _engine = new TextAnimationEngine(_config);
            _engine.Begin(5);

            _engine.Tick(0.01f);

            // 早期帧应有透明度变化
            bool hasAlphaChange = false;
            for (int i = 0; i < _engine.CharCount; i++)
            {
                ref readonly var data = ref _engine.GetCharData(i);
                if (data.Alpha < 1f) hasAlphaChange = true;
            }
            Assert.IsTrue(hasAlphaChange, "Fade 动画早期帧应有透明度 < 1");
        }

        [Test]
        public void Tick_NonLooping_StopsAfterDuration()
        {
            _config = new TextAnimationConfig(duration: 0.1f);
            _engine = new TextAnimationEngine(_config);
            _engine.Begin(3);

            // 推进超过持续时间
            for (int i = 0; i < 20; i++)
            {
                _engine.Tick(0.016f);
            }

            Assert.IsFalse(_engine.IsPlaying);
        }

        [Test]
        public void Tick_Looping_KeepsPlaying()
        {
            _config = new TextAnimationConfig(duration: -1f);
            _engine = new TextAnimationEngine(_config);
            _engine.Begin(3);

            // 推进超过持续时间
            for (int i = 0; i < 20; i++)
            {
                _engine.Tick(0.016f);
            }

            Assert.IsTrue(_engine.IsPlaying, "循环模式应持续播放");
        }

        [Test]
        public void GetCharData_OutOfRange_Throws()
        {
            _engine.Begin(5);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => _engine.GetCharData(-1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _engine.GetCharData(5));
        }

        [Test]
        public void Stop_SetsNotPlaying()
        {
            _engine.Begin(5);
            _engine.Stop();

            Assert.IsFalse(_engine.IsPlaying);
        }

        [Test]
        public void SkipToEnd_ResetsCharData()
        {
            _config = new TextAnimationConfig(type: TextAnimationType.Wave);
            _engine = new TextAnimationEngine(_config);
            _engine.Begin(5);

            _engine.Tick(0.1f);
            _engine.SkipToEnd();

            // SkipToEnd 后所有字符数据应重置
            for (int i = 0; i < _engine.CharCount; i++)
            {
                ref readonly var data = ref _engine.GetCharData(i);
                Assert.AreEqual(0f, data.XOffset);
                Assert.AreEqual(0f, data.YOffset);
                Assert.AreEqual(1f, data.Scale);
                Assert.AreEqual(1f, data.Alpha);
            }
        }
    }
}
