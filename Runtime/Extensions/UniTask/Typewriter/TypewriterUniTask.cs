// asmdef Version Defines，安装 UniTask 包后自动启用。

#if UNITASK_SUPPORT

using System;
using System.Threading;
using CNoom.UnityGameTool.TextAnimation;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.Typewriter
{
    /// <summary>
    /// 基于 UniTask 的打字机组件。支持异步等待和 CancellationToken 取消。
    /// 需要安装 UniTask 包后自动编译。
    /// 支持可选关联 ITextAnimation，实现边打字边播放逐字动画。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterUniTask : MonoBehaviour, ITypewriter
    {
        [Header("打字机配置")]
        [Tooltip("打字机效果配置，可直接在 Inspector 中编辑")]
        [SerializeField]
        private TypewriterConfig _config = new TypewriterConfig();

        [Header("文字动画（可选）")]
        [Tooltip("关联的文字动画组件，打字时自动同步逐字动画。留空则不启用。")]
        [SerializeField]
        private MonoBehaviour _textAnimationComponent;

        private TypewriterEngine _engine;
        private TMP_Text _textComponent;
        private CancellationTokenSource _playCts;
        private ITextAnimation _textAnimation;

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
            ResolveTextAnimation();
        }

        /// <summary>
        /// 解析 ITextAnimation 引用。支持 Inspector 手动指定或自动查找。
        /// </summary>
        private void ResolveTextAnimation()
        {
            if (_textAnimationComponent != null)
            {
                _textAnimation = _textAnimationComponent as ITextAnimation;
                if (_textAnimation == null)
                {
                    Debug.LogWarning(
                        $"[TypewriterUniTask] {_textAnimationComponent.GetType().Name} 未实现 ITextAnimation 接口，文字动画功能已禁用。",
                        this);
                }
            }
            else
            {
                _textAnimation = GetComponentInChildren<ITextAnimation>();
            }
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

                // 启动文字动画（仅第一个字符）
                if (_textAnimation != null)
                {
                    _textAnimation.Play(1);
                }

                while (_engine.IsPlaying)
                {
                    int index = _engine.CurrentIndex;
                    char c = _textComponent.textInfo.characterInfo[index].character;
                    var (hasMore, delay) = _engine.Advance(c);

                    _textComponent.maxVisibleCharacters = _engine.CurrentIndex;

                    // 同步文字动画的可见字符数
                    if (_textAnimation != null)
                    {
                        _textAnimation.UpdateVisibleCount(_engine.CurrentIndex);
                    }

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
                _textAnimation?.Stop();
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
                _textAnimation?.Skip();
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
