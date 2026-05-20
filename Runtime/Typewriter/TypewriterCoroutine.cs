using System;
using System.Collections;
using CNoom.UnityGameTool.TextAnimation;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.Typewriter
{
    /// <summary>
    /// 基于协程的打字机组件。零外部依赖，开箱即用。
    /// 通过 TMP 的 maxVisibleCharacters 实现逐字显示。
    /// 支持可选关联 ITextAnimation，实现边打字边播放逐字动画。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterCoroutine : MonoBehaviour, ITypewriter
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
        private Coroutine _playCoroutine;
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
                        $"[TypewriterCoroutine] {_textAnimationComponent.GetType().Name} 未实现 ITextAnimation 接口，文字动画功能已禁用。",
                        this);
                }
            }
            else
            {
                // 自动查找同 GameObject 或子级上的 ITextAnimation 实现
                _textAnimation = GetComponentInChildren<ITextAnimation>();
            }
        }

        /// <inheritdoc />
        public void Play(string text)
        {
            StopDriver();
            _playCoroutine = StartCoroutine(PlayRoutine(text));
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (_engine != null && _engine.IsPlaying)
            {
                StopDriver();
                _engine.Stop();
                _textAnimation?.Stop();
            }
        }

        /// <inheritdoc />
        public void Skip()
        {
            if (_engine != null && _engine.IsPlaying)
            {
                StopDriver();
                _engine.SkipToEnd();
                _textComponent.maxVisibleCharacters = _engine.TotalCharacters;
                _textAnimation?.Skip();
                OnComplete?.Invoke();
            }
        }

        private IEnumerator PlayRoutine(string text)
        {
            _textComponent.text = text ?? string.Empty;
            _textComponent.ForceMeshUpdate(true);

            int total = _textComponent.textInfo.characterCount;
            _engine.Begin(total);
            _textComponent.maxVisibleCharacters = 0;

            if (total == 0)
            {
                _playCoroutine = null;
                OnComplete?.Invoke();
                yield break;
            }

            // 启动文字动画（仅第一个字符）
            if (_textAnimation != null)
            {
                _textAnimation.Play(1);
            }

            while (_engine.IsPlaying)
            {
                int index = _engine.CurrentIndex;

                // 边界检查：防止 TMP 富文本解析导致 characterCount 变化
                if (index >= _textComponent.textInfo.characterCount)
                {
                    break;
                }

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

                // 使用累计计时替代 WaitForSeconds，避免每字符 GC 分配
                if (delay > 0f)
                {
                    float timer = 0f;
                    while (timer < delay)
                    {
                        yield return null;
                        timer += Time.deltaTime;
                    }
                }
            }

            _playCoroutine = null;
            OnComplete?.Invoke();
        }

        private void StopDriver()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            StopDriver();
        }
    }
}
