using System.Collections;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.NumberRoller
{
    /// <summary>
    /// 基于协程的数字滚动组件。零外部依赖，开箱即用。
    /// 自动将滚动值写入 TMP_Text。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class NumberRollerCoroutine : MonoBehaviour, INumberRoller
    {
        [Header("滚动配置")]
        [Tooltip("数字滚动效果配置，可直接在 Inspector 中编辑")]
        [SerializeField]
        private NumberRollerConfig _config = new NumberRollerConfig();

        private NumberRollerEngine _engine;
        private TMP_Text _textComponent;
        private Coroutine _playCoroutine;

        /// <inheritdoc />
        public bool IsPlaying => _engine != null && _engine.IsPlaying;

        /// <inheritdoc />
        public double CurrentValue => _engine != null ? _engine.CurrentValue : 0;

        /// <inheritdoc />
        public double TargetValue => _engine != null ? _engine.ToValue : 0;

        /// <inheritdoc />
        public event NumberRollerCompleteHandler OnComplete;

        /// <inheritdoc />
        public event NumberRollerUpdateHandler OnUpdate;

        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
            _engine = new NumberRollerEngine(_config);
        }

        /// <inheritdoc />
        public void Play(double from, double to)
        {
            StopDriver();
            _playCoroutine = StartCoroutine(PlayRoutine(from, to));
        }

        /// <inheritdoc />
        public void Play(double to)
        {
            double from = _engine != null ? _engine.CurrentValue : 0;
            Play(from, to);
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
                string text = _engine.GetFormattedValue();
                _textComponent.text = text;
                OnUpdate?.Invoke(_engine.CurrentValue, text);
                OnComplete?.Invoke();
            }
        }

        private IEnumerator PlayRoutine(double from, double to)
        {
            _engine.Begin(from, to);

            // 立即显示起始值
            if (!_engine.IsPlaying)
            {
                string text = _engine.GetFormattedValue();
                _textComponent.text = text;
                OnUpdate?.Invoke(_engine.CurrentValue, text);
                _playCoroutine = null;
                OnComplete?.Invoke();
                yield break;
            }

            string initialText = _engine.GetFormattedValue();
            _textComponent.text = initialText;
            OnUpdate?.Invoke(_engine.CurrentValue, initialText);

            while (_engine.IsPlaying)
            {
                _engine.Tick(Time.deltaTime);
                string text = _engine.GetFormattedValue();
                _textComponent.text = text;
                OnUpdate?.Invoke(_engine.CurrentValue, text);

                yield return null;
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
