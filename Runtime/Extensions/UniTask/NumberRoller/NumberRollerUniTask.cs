// asmdef Version Defines，安装 UniTask 包后自动启用。

#if UNITASK_SUPPORT

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.NumberRoller
{
    /// <summary>
    /// 基于 UniTask 的数字滚动组件。支持异步等待和 CancellationToken 取消。
    /// 需要安装 UniTask 包后自动编译。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class NumberRollerUniTask : MonoBehaviour, INumberRoller
    {
        [Header("滚动配置")]
        [Tooltip("数字滚动效果配置，可直接在 Inspector 中编辑")]
        [SerializeField]
        private NumberRollerConfig _config = new NumberRollerConfig();

        private NumberRollerEngine _engine;
        private TMP_Text _textComponent;
        private CancellationTokenSource _playCts;

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
            PlayAsync(from, to).Forget();
        }

        /// <inheritdoc />
        public void Play(double to)
        {
            double from = _engine != null ? _engine.CurrentValue : 0;
            PlayAsync(from, to).Forget();
        }

        /// <summary>
        /// 异步从起始值滚动到目标值，支持 await 等待完成。
        /// </summary>
        /// <param name="from">起始值</param>
        /// <param name="to">目标值</param>
        public async UniTask PlayAsync(double from, double to)
        {
            CancelPlay();
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            try
            {
                _engine.Begin(from, to);

                // 立即显示起始值
                string text = _engine.GetFormattedValue();
                _textComponent.text = text;
                OnUpdate?.Invoke(_engine.CurrentValue, text);

                if (!_engine.IsPlaying)
                {
                    OnComplete?.Invoke();
                    return;
                }

                while (_engine.IsPlaying)
                {
                    await UniTask.Yield(token);

                    _engine.Tick(Time.deltaTime);
                    text = _engine.GetFormattedValue();
                    _textComponent.text = text;
                    OnUpdate?.Invoke(_engine.CurrentValue, text);
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
                string text = _engine.GetFormattedValue();
                _textComponent.text = text;
                OnUpdate?.Invoke(_engine.CurrentValue, text);
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
