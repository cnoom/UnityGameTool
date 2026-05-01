using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.Typewriter
{
    /// <summary>
    /// 基于协程的打字机组件。零外部依赖，开箱即用。
    /// 通过 TMP 的 maxVisibleCharacters 实现逐字显示。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterCoroutine : MonoBehaviour, ITypewriter
    {
        [Header("打字机配置")]
        [Tooltip("打字机效果配置，可直接在 Inspector 中编辑")]
        [SerializeField]
        private TypewriterConfig _config = new TypewriterConfig();

        private TypewriterEngine _engine;
        private TMP_Text _textComponent;
        private Coroutine _playCoroutine;

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
                    yield return new WaitForSeconds(delay);
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
