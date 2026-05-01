using System;

namespace CNoom.UnityGameTool.Dialogue
{
    /// <summary>
    /// 对话序列管理器纯逻辑引擎。负责对话流程状态机、段落推进和分支跳转，
    /// 不依赖任何 Unity 组件或打字机实现，完全可单元测试。
    /// </summary>
    public class DialogueSequencerEngine
    {
        private DialogueData _data;
        private int _currentSegmentIndex;
        private DialogueState _state;

        /// <summary>当前对话状态</summary>
        public DialogueState State => _state;

        /// <summary>当前段落索引</summary>
        public int CurrentSegmentIndex => _currentSegmentIndex;

        /// <summary>当前段落数据</summary>
        public DialogueSegment CurrentSegment => _data?.GetSegment(_currentSegmentIndex);

        /// <summary>是否正在播放对话</summary>
        public bool IsActive => _state != DialogueState.Idle;

        /// <summary>
        /// 开始播放对话。
        /// </summary>
        /// <param name="dialogueData">对话数据</param>
        /// <param name="startSegmentIndex">起始段落索引</param>
        /// <returns>起始段落，如果对话为空返回 null</returns>
        public DialogueSegment Begin(DialogueData dialogueData, int startSegmentIndex = 0)
        {
            _data = dialogueData ?? throw new ArgumentNullException(nameof(dialogueData));

            if (_data.Count == 0)
            {
                _state = DialogueState.Completed;
                return null;
            }

            _currentSegmentIndex = startSegmentIndex;
            var segment = _data.GetSegment(_currentSegmentIndex);

            if (segment == null)
            {
                _state = DialogueState.Completed;
                return null;
            }

            _state = DialogueState.Typing;
            return segment;
        }

        /// <summary>
        /// 标记当前段落打字完成。由 Driver 在打字机完成时调用。
        /// </summary>
        /// <returns>当前段落（用于触发 OnSegmentComplete 事件）</returns>
        public DialogueSegment MarkTypingComplete()
        {
            if (_state != DialogueState.Typing) return null;

            var segment = CurrentSegment;

            if (segment != null && segment.HasChoices)
            {
                _state = DialogueState.WaitingForChoice;
            }
            else
            {
                _state = DialogueState.WaitingForInput;
            }

            return segment;
        }

        /// <summary>
        /// 推进到下一段。仅当状态为 WaitingForInput 时有效。
        /// </summary>
        /// <returns>下一段落，对话结束返回 null</returns>
        public DialogueSegment AdvanceToNext()
        {
            if (_state != DialogueState.WaitingForInput) return null;

            var segment = CurrentSegment;
            if (segment == null) return null;

            // 确定下一段索引
            int nextIndex = segment.NextSegmentIndex;

            // NextSegmentIndex 为 -1 时，尝试顺序递增
            if (nextIndex < 0)
            {
                nextIndex = _currentSegmentIndex + 1;
            }

            return GoToSegment(nextIndex);
        }

        /// <summary>
        /// 选择分支选项。仅当状态为 WaitingForChoice 时有效。
        /// </summary>
        /// <param name="choiceIndex">选项索引</param>
        /// <returns>跳转后的段落，对话结束返回 null</returns>
        public DialogueSegment Choose(int choiceIndex)
        {
            if (_state != DialogueState.WaitingForChoice) return null;

            var segment = CurrentSegment;
            if (segment == null || !segment.HasChoices) return null;
            if (choiceIndex < 0 || choiceIndex >= segment.Choices.Count) return null;

            int nextIndex = segment.Choices[choiceIndex].NextSegmentIndex;
            return GoToSegment(nextIndex);
        }

        /// <summary>
        /// 停止对话。
        /// </summary>
        public void Stop()
        {
            _state = DialogueState.Idle;
            _data = null;
            _currentSegmentIndex = -1;
        }

        /// <summary>
        /// 重置引擎。
        /// </summary>
        public void Reset()
        {
            _state = DialogueState.Idle;
            _data = null;
            _currentSegmentIndex = -1;
        }

        /// <summary>
        /// 跳转到指定段落索引。
        /// </summary>
        private DialogueSegment GoToSegment(int index)
        {
            // -1 表示对话结束
            if (index < 0)
            {
                _state = DialogueState.Completed;
                return null;
            }

            var segment = _data.GetSegment(index);
            if (segment == null)
            {
                _state = DialogueState.Completed;
                return null;
            }

            _currentSegmentIndex = index;
            _state = DialogueState.Typing;
            return segment;
        }
    }
}
