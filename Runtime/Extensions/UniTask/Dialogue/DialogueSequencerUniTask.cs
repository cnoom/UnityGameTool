// asmdef Version Defines，安装 UniTask 包后自动启用。

#if UNITASK_SUPPORT

using System;
using System.Collections.Generic;
using System.Threading;
using CNoom.UnityGameTool.Typewriter;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.Dialogue
{
    /// <summary>
    /// 基于 UniTask 的对话序列管理器组件。支持异步等待对话完成和 CancellationToken 取消。
    /// 需要安装 UniTask 包后自动编译。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class DialogueSequencerUniTask : MonoBehaviour, IDialogueSequencer
    {
        [Header("打字机引用")]
        [Tooltip("打字机组件，如果为空则自动获取同 GameObject 上的 ITypewriter")]
        [SerializeField]
        private MonoBehaviour _typewriterComponent;

        private ITypewriter _typewriter;
        private DialogueSequencerEngine _engine;
        private CancellationTokenSource _playCts;

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
                _typewriter = GetComponentInChildren<ITypewriter>();
            }
        }

        /// <inheritdoc />
        public void Play(DialogueData dialogueData)
        {
            PlayAsync(dialogueData, 0).Forget();
        }

        /// <inheritdoc />
        public void Play(DialogueData dialogueData, int startSegmentIndex)
        {
            PlayAsync(dialogueData, startSegmentIndex).Forget();
        }

        /// <summary>
        /// 异步播放对话，支持 await 等待完成。
        /// </summary>
        /// <param name="dialogueData">对话数据</param>
        /// <param name="startSegmentIndex">起始段落索引</param>
        /// <param name="token">取消令牌</param>
        public async UniTask PlayAsync(
            DialogueData dialogueData,
            int startSegmentIndex = 0,
            CancellationToken token = default)
        {
            CancelPlay();

            if (_typewriter == null)
            {
                Debug.LogError("[DialogueSequencer] 未找到 ITypewriter 组件，无法播放对话。");
                return;
            }

            _playCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var myCts = _playCts;
            var linkedToken = myCts.Token;

            try
            {
                var segment = _engine.Begin(dialogueData, startSegmentIndex);

                if (segment == null)
                {
                    OnDialogueComplete?.Invoke();
                    return;
                }

                // 循环处理每一段对话
                while (_engine.IsActive)
                {
                    // 播放当前段落
                    OnSegmentStart?.Invoke(_engine.CurrentSegmentIndex, segment);

                    // 等待打字机完成
                    _typewriter.Play(segment.Text ?? string.Empty);
                    await WaitForTypewriterComplete(linkedToken);

                    // 标记打字完成
                    var completedSegment = _engine.MarkTypingComplete();
                    if (completedSegment == null) break;

                    int index = _engine.CurrentSegmentIndex;
                    OnSegmentComplete?.Invoke(index, completedSegment);

                    // 有分支选项
                    if (_engine.State == DialogueState.WaitingForChoice && completedSegment.HasChoices)
                    {
                        OnChoicesPresented?.Invoke(completedSegment.Choices);
                        break; // 等待外部调用 Choose()
                    }

                    // 等待外部调用 Next()
                    if (_engine.State == DialogueState.WaitingForInput)
                    {
                        break; // 等待外部调用 Next()
                    }
                }

                // 对话自然完成
                if (_engine.State == DialogueState.Completed)
                {
                    Cleanup();
                    OnDialogueComplete?.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                // 仅当自己的 CTS 仍是当前活跃的才清理
                // （说明是外部取消，而非新 Play 覆盖）
                if (_playCts == myCts)
                {
                    Cleanup();
                }
            }
        }

        /// <summary>
        /// 异步等待用户选择后继续对话。
        /// </summary>
        /// <param name="choiceIndex">选项索引</param>
        /// <param name="token">取消令牌</param>
        public async UniTask ChooseAsync(int choiceIndex, CancellationToken token = default)
        {
            if (_engine.State != DialogueState.WaitingForChoice) return;

            var nextSegment = _engine.Choose(choiceIndex);

            if (nextSegment == null)
            {
                Cleanup();
                OnDialogueComplete?.Invoke();
                return;
            }

            // 继续播放
            await ContinueFromSegment(nextSegment, token);
        }

        /// <summary>
        /// 异步推进到下一段后继续对话。
        /// </summary>
        /// <param name="token">取消令牌</param>
        public async UniTask NextAsync(CancellationToken token = default)
        {
            if (_engine.State != DialogueState.WaitingForInput) return;

            var nextSegment = _engine.AdvanceToNext();

            if (nextSegment == null)
            {
                Cleanup();
                OnDialogueComplete?.Invoke();
                return;
            }

            await ContinueFromSegment(nextSegment, token);
        }

        /// <inheritdoc />
        public void Next()
        {
            NextAsync().Forget();
        }

        /// <inheritdoc />
        public void Choose(int choiceIndex)
        {
            ChooseAsync(choiceIndex).Forget();
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

        private async UniTask ContinueFromSegment(DialogueSegment segment, CancellationToken token)
        {
            while (_engine.IsActive)
            {
                OnSegmentStart?.Invoke(_engine.CurrentSegmentIndex, segment);

                _typewriter.Play(segment.Text ?? string.Empty);
                await WaitForTypewriterComplete(token);

                var completedSegment = _engine.MarkTypingComplete();
                if (completedSegment == null) break;

                int index = _engine.CurrentSegmentIndex;
                OnSegmentComplete?.Invoke(index, completedSegment);

                if (_engine.State == DialogueState.WaitingForChoice && completedSegment.HasChoices)
                {
                    OnChoicesPresented?.Invoke(completedSegment.Choices);
                    break;
                }

                if (_engine.State == DialogueState.WaitingForInput)
                {
                    break;
                }
            }

            if (_engine.State == DialogueState.Completed)
            {
                Cleanup();
                OnDialogueComplete?.Invoke();
            }
        }

        private async UniTask WaitForTypewriterComplete(CancellationToken token)
        {
            bool completed = false;
            TypewriterCompleteHandler handler = () => completed = true;
            _typewriter.OnComplete += handler;

            try
            {
                await UniTask.WaitUntil(() => completed, cancellationToken: token);
            }
            finally
            {
                _typewriter.OnComplete -= handler;
            }
        }

        private void CancelPlay()
        {
            if (_playCts != null)
            {
                _playCts.Cancel();
                _playCts.Dispose();
                _playCts = null;
            }
        }

        private void Cleanup()
        {
            CancelPlay();
            if (_typewriter != null)
            {
                _typewriter.Stop();
            }
            _engine.Stop();
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}

#endif
