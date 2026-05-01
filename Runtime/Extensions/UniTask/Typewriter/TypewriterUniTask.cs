// asmdef Version Defines，安装 UniTask 包后自动启用。

#if UNITASK_SUPPORT

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.Typewriter
{
    /// <summary>
    /// 基于 UniTask 的打字机组件。支持异步等待和 CancellationToken 取消。
    /// 需要安装 UniTask 包后自动编译。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterUniTask : MonoBehaviour, ITypewriter
    {
        [Header("打字机配置")]
        [Tooltip("打字机效果配置，可直接在 Inspector 中编辑")]
        [SerializeField]
        private TypewriterConfig _config = new TypewriterConfig();

        private TypewriterEngine _engine;
        private TMP_Text _textComponent;
        private CancellationTokenSource _playCts;

        /// <inheritdoc />
        public bool IsPlaying => _engine != null && _engine.IsPlaying;

        /// <inheritdoc />
        public event TypewriterCompleteHandler OnComplete;

        /// <inheritdoc />
        public event TypewriterCharacterHandler OnCharacterTyped;

        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
            _engine = new TypewriterEngine(_config);
        }

        /// <inheritdoc />
        public void Play(string text)
        {
            PlayAsync(text).Forget();
        }

        /// <summary>
        /// 异步播放打字机效果，支持 await 等待完成。
        /// </summary>
        /// <param name="text">要逐字显示的文本</param>
        public async UniTask PlayAsync(string text)
        {
            CancelPlay();
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            try
            {
                _textComponent.text = text ?? string.Empty;
                _textComponent.ForceMeshUpdate(true);

                int total = _textComponent.textInfo.characterCount;
                _engine.Begin(total);
                _textComponent.maxVisibleCharacters = 0;

                if (total == 0)
                {
                    OnComplete?.Invoke();
                    return;
                }

                while (_engine.IsPlaying)
                {
                    int index = _engine.CurrentIndex;
                    char c = _textComponent.textInfo.characterInfo[index].character;
                    var (hasMore, delay) = _engine.Advance(c);

                    _textComponent.maxVisibleCharacters = _engine.CurrentIndex;
                    OnCharacterTyped?.Invoke(index, c);

                    if (!hasMore)
                    {
                        break;
                    }

                    if (delay > 0f)
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(delay),
                            cancellationToken: token);
                    }
                }

                OnComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // 由 Skip/Stop/Destroy 触发取消，Engine 状态已设置
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (_engine != null && _engine.IsPlaying)
            {
                CancelPlay();
                _engine.Stop();
            }
        }

        /// <inheritdoc />
        public void Skip()
        {
            if (_engine != null && _engine.IsPlaying)
            {
                CancelPlay();
                _engine.SkipToEnd();
                _textComponent.maxVisibleCharacters = _engine.TotalCharacters;
                OnComplete?.Invoke();
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

        private void OnDestroy()
        {
            CancelPlay();
        }
    }
}

#endif
