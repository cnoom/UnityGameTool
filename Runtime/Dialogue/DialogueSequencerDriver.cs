using System;
using System.Collections.Generic;
using CNoom.UnityGameTool.Typewriter;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.Dialogue
{
    /// <summary>
    /// 对话序列管理器驱动组件。协调 ITypewriter 完成多段对话的自动播放、
    /// 等待输入和分支选择。零外部依赖（除同包 Typewriter）。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class DialogueSequencerDriver : MonoBehaviour, IDialogueSequencer
    {
        [Header("打字机引用")]
        [Tooltip("打字机组件，如果为空则自动获取同 GameObject 上的 ITypewriter")]
        [SerializeField]
        private MonoBehaviour _typewriterComponent;

        private ITypewriter _typewriter;
        private DialogueSequencerEngine _engine;

        /// <inheritdoc />
        public DialogueState State => _engine?.State ?? DialogueState.Idle;

        /// <inheritdoc />
        public int CurrentSegmentIndex => _engine?.CurrentSegmentIndex ?? -1;

        /// <inheritdoc />
        public DialogueSegment CurrentSegment => _engine?.CurrentSegment;

        /// <inheritdoc />
        public event SegmentStartHandler OnSegmentStart;

        /// <inheritdoc />
        public event SegmentCompleteHandler OnSegmentComplete;

        /// <inheritdoc />
        public event DialogueCompleteHandler OnDialogueComplete;

        /// <inheritdoc />
        public event ChoicesPresentedHandler OnChoicesPresented;

        private void Awake()
        {
            _engine = new DialogueSequencerEngine();
            ResolveTypewriter();
        }

        private void ResolveTypewriter()
        {
            if (_typewriterComponent != null)
            {
                _typewriter = _typewriterComponent as ITypewriter;
            }

            if (_typewriter == null)
            {
                _typewriter = GetComponent<ITypewriter>();
            }

            if (_typewriter == null)
            {
                // 尝试在子对象中查找
                _typewriter = GetComponentInChildren<ITypewriter>();
            }
        }

        /// <inheritdoc />
        public void Play(DialogueData dialogueData)
        {
            Play(dialogueData, 0);
        }

        /// <inheritdoc />
        public void Play(DialogueData dialogueData, int startSegmentIndex)
        {
            if (_typewriter == null)
            {
                Debug.LogError("[DialogueSequencer] 未找到 ITypewriter 组件，无法播放对话。");
                return;
            }

            Stop();
            _typewriter.OnComplete += OnTypewriterComplete;

            var segment = _engine.Begin(dialogueData, startSegmentIndex);

            if (segment == null)
            {
                OnDialogueComplete?.Invoke();
                return;
            }

            PlaySegment(segment);
        }

        /// <inheritdoc />
        public void Next()
        {
            if (_engine.State != DialogueState.WaitingForInput) return;

            var nextSegment = _engine.AdvanceToNext();

            if (nextSegment == null)
            {
                Cleanup();
                OnDialogueComplete?.Invoke();
                return;
            }

            PlaySegment(nextSegment);
        }

        /// <inheritdoc />
        public void Choose(int choiceIndex)
        {
            if (_engine.State != DialogueState.WaitingForChoice) return;

            var nextSegment = _engine.Choose(choiceIndex);

            if (nextSegment == null)
            {
                Cleanup();
                OnDialogueComplete?.Invoke();
                return;
            }

            PlaySegment(nextSegment);
        }

        /// <inheritdoc />
        public void SkipTyping()
        {
            if (_engine.State == DialogueState.Typing && _typewriter != null)
            {
                _typewriter.Skip();
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (_engine.IsActive)
            {
                Cleanup();
            }
        }

        private void PlaySegment(DialogueSegment segment)
        {
            OnSegmentStart?.Invoke(_engine.CurrentSegmentIndex, segment);
            _typewriter.Play(segment.Text ?? string.Empty);
        }

        private void OnTypewriterComplete()
        {
            var completedSegment = _engine.MarkTypingComplete();
            if (completedSegment == null) return;

            int index = _engine.CurrentSegmentIndex;
            OnSegmentComplete?.Invoke(index, completedSegment);

            // 如果有分支选项，触发选项展示事件
            if (_engine.State == DialogueState.WaitingForChoice && completedSegment.HasChoices)
            {
                OnChoicesPresented?.Invoke(completedSegment.Choices);
            }
        }

        private void Cleanup()
        {
            if (_typewriter != null)
            {
                _typewriter.Stop();
                _typewriter.OnComplete -= OnTypewriterComplete;
            }

            _engine.Stop();
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
