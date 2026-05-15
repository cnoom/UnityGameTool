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

        #region Once 模式测试

        [Test]
        public void Once_Wave_UnactivatedCharsAreInvisible()
        {
            _config = new TextAnimationConfig(
                type: TextAnimationType.Wave,
                playMode: TextAnimationPlayMode.Once,
                duration: 0.5f,
                charDelay: 0.1f);
            _engine = new TextAnimationEngine(_config);
            _engine.Begin(5);

            // 第 1 帧，只有字符 0 激活
            _engine.Tick(0.01f);

            // 字符 1~4 应不可见
            for (int i = 1; i < _engine.CharCount; i++)
            {
                ref readonly var data = ref _engine.GetCharData(i);
                Assert.AreEqual(0f, data.Alpha, $"字符 {i} 未激活时应 Alpha=0");
            }
        }

        [Test]
        public void Once_StopsWhenAllCharsComplete()
        {
            _config = new TextAnimationConfig(
                type: TextAnimationType.Wave,
                playMode: TextAnimationPlayMode.Once,
                duration: 0.2f,
                charDelay: 0.05f);
            _engine = new TextAnimationEngine(_config);
            _engine.Begin(3);

            // 全局结束时间 = 0.05*2 + 0.2 = 0.3
            // 推进到超过这个时间
            for (int i = 0; i < 50; i++)
            {
                _engine.Tick(0.016f);
            }

            Assert.IsFalse(_engine.IsPlaying, "Once 模式所有字符完成后应停止");

            // 所有字符应归位
            for (int i = 0; i < _engine.CharCount; i++)
            {
                ref readonly var data = ref _engine.GetCharData(i);
                Assert.AreEqual(0f, data.XOffset);
                Assert.AreEqual(0f, data.YOffset);
                Assert.AreEqual(1f, data.Scale);
                Assert.AreEqual(1f, data.Alpha);
            }
        }

        [Test]
        public void Once_Fade_SameBehaviorAsContinuous()
        {
            _config = new TextAnimationConfig(
                type: TextAnimationType.Fade,
                playMode: TextAnimationPlayMode.Once,
                duration: 1f,
                fadeDuration: 0.5f);
            _engine = new TextAnimationEngine(_config);
            _engine.Begin(3);

            _engine.Tick(0.1f);

            // 第一个字符应该有部分透明度
            ref readonly var data0 = ref _engine.GetCharData(0);
            Assert.Greater(data0.Alpha, 0f, "Fade Once 模式已激活字符应有透明度变化");
            Assert.Less(data0.Alpha, 1f, "渐显未完成时 Alpha < 1");
        }

        [Test]
        public void Once_Wave_HasEnvelopeDecay()
        {
            _config = new TextAnimationConfig(
                type: TextAnimationType.Wave,
                playMode: TextAnimationPlayMode.Once,
                duration: 1f,
                amplitude: 20f,
                frequency: 5f);
            _engine = new TextAnimationEngine(_config);
            _engine.Begin(1);

            // 早期帧：偏移量较大
            _engine.Tick(0.05f);
            ref readonly var earlyData = ref _engine.GetCharData(0);
            float earlyOffset = Math.Abs(earlyData.YOffset);

            // 接近结束帧：偏移量应该因包络衰减而减小
            for (int i = 0; i < 80; i++)
            {
                _engine.Tick(0.016f);
            }
            ref readonly var lateData = ref _engine.GetCharData(0);
            float lateOffset = Math.Abs(lateData.YOffset);

            Assert.Less(lateOffset, earlyOffset,
                "Once 模式包络衰减应使偏移量随时间减小");
        }

        #endregion
    }
}
