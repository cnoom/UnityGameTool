using System;
using System.Collections.Generic;
using NUnit.Framework;
using CNoom.UnityGameTool.Dialogue;

namespace CNoom.UnityGameTool.Tests
{
    [TestFixture]
    public class DialogueSequencerEngineTests
    {
        private DialogueSequencerEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _engine = new DialogueSequencerEngine();
        }

        [Test]
        public void Begin_NullData_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.Begin(null));
        }

        [Test]
        public void Begin_EmptyData_ReturnsNull()
        {
            var data = new DialogueData();
            var segment = _engine.Begin(data);

            Assert.IsNull(segment);
            Assert.AreEqual(DialogueState.Completed, _engine.State);
        }

        [Test]
        public void Begin_ValidData_ReturnsFirstSegment()
        {
            var data = CreateSimpleDialogue(3);
            var segment = _engine.Begin(data);

            Assert.IsNotNull(segment);
            Assert.AreEqual("Speaker_0", segment.SpeakerName);
            Assert.AreEqual(DialogueState.Typing, _engine.State);
            Assert.AreEqual(0, _engine.CurrentSegmentIndex);
        }

        [Test]
        public void MarkTypingComplete_NoChoices_EntersWaitingForInput()
        {
            var data = CreateSimpleDialogue(3);
            _engine.Begin(data);

            var segment = _engine.MarkTypingComplete();

            Assert.IsNotNull(segment);
            Assert.AreEqual(DialogueState.WaitingForInput, _engine.State);
        }

        [Test]
        public void MarkTypingComplete_WithChoices_EntersWaitingForChoice()
        {
            var data = CreateDialogueWithChoices();
            _engine.Begin(data);

            var segment = _engine.MarkTypingComplete();

            Assert.IsNotNull(segment);
            Assert.AreEqual(DialogueState.WaitingForChoice, _engine.State);
        }

        [Test]
        public void AdvanceToNext_MovesToNextSegment()
        {
            var data = CreateSimpleDialogue(3);
            _engine.Begin(data);
            _engine.MarkTypingComplete();

            var next = _engine.AdvanceToNext();

            Assert.IsNotNull(next);
            Assert.AreEqual(1, _engine.CurrentSegmentIndex);
            Assert.AreEqual(DialogueState.Typing, _engine.State);
        }

        [Test]
        public void AdvanceToNext_LastSegment_Completes()
        {
            var data = CreateSimpleDialogue(1);
            _engine.Begin(data);
            _engine.MarkTypingComplete();

            var next = _engine.AdvanceToNext();

            Assert.IsNull(next);
            Assert.AreEqual(DialogueState.Completed, _engine.State);
        }

        [Test]
        public void Choose_ValidIndex_JumpsToCorrectSegment()
        {
            var data = CreateDialogueWithChoices();
            _engine.Begin(data);
            _engine.MarkTypingComplete();

            // 选择第一个选项（跳转到索引 2）
            var segment = _engine.Choose(0);

            Assert.IsNotNull(segment);
            Assert.AreEqual(2, _engine.CurrentSegmentIndex);
            Assert.AreEqual(DialogueState.Typing, _engine.State);
        }

        [Test]
        public void Choose_InvalidIndex_ReturnsNull()
        {
            var data = CreateDialogueWithChoices();
            _engine.Begin(data);
            _engine.MarkTypingComplete();

            var segment = _engine.Choose(99);

            Assert.IsNull(segment);
        }

        [Test]
        public void AdvanceToNext_WrongState_ReturnsNull()
        {
            var data = CreateSimpleDialogue(3);
            _engine.Begin(data);

            // 状态是 Typing，不是 WaitingForInput
            var result = _engine.AdvanceToNext();
            Assert.IsNull(result);
        }

        [Test]
        public void Stop_ResetsToIdle()
        {
            var data = CreateSimpleDialogue(3);
            _engine.Begin(data);

            _engine.Stop();

            Assert.AreEqual(DialogueState.Idle, _engine.State);
            Assert.IsFalse(_engine.IsActive);
        }

        #region 测试辅助方法

        private static DialogueData CreateSimpleDialogue(int segmentCount)
        {
            var data = new DialogueData();
            for (int i = 0; i < segmentCount; i++)
            {
                data.Segments.Add(new DialogueSegment
                {
                    SpeakerName = $"Speaker_{i}",
                    Text = $"Text_{i}",
                    NextSegmentIndex = -1 // 顺序推进
                });
            }
            return data;
        }

        private static DialogueData CreateDialogueWithChoices()
        {
            var data = new DialogueData();
            // 段落 0：带分支选项
            data.Segments.Add(new DialogueSegment
            {
                SpeakerName = "NPC",
                Text = "你要选择什么？",
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice("选项A", 2),
                    new DialogueChoice("选项B", -1) // 结束对话
                }
            });
            // 段落 1：不会直接到达
            data.Segments.Add(new DialogueSegment
            {
                SpeakerName = "Hidden",
                Text = "隐藏段落"
            });
            // 段落 2：选项A 跳转到这里
            data.Segments.Add(new DialogueSegment
            {
                SpeakerName = "NPC",
                Text = "你选了A"
            });
            return data;
        }

        #endregion
    }
}
