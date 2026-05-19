// asmdef Version Defines，安装 UniTask 包后自动启用。

#if UNITASK_SUPPORT

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CNoom.UnityGameTool.ProgressBar
{
    /// <summary>
    /// 基于 UniTask 的进度条组件。支持异步等待过渡完成和 CancellationToken 取消。
    /// 需要安装 UniTask 包后自动编译。
    /// </summary>
    [DisallowMultipleComponent]
    public class ProgressBarUniTask : MonoBehaviour, IProgressBar
    {
        [Header("进度条配置")]
        [Tooltip("进度条配置，可直接在 Inspector 中编辑")]
        [SerializeField]
        private ProgressBarConfig _config = new ProgressBarConfig();

        private ProgressBarEngine _engine;
        private CancellationTokenSource _tickCts;

        /// <inheritdoc />
        public float Value => _engine != null ? _engine.Value : 0f;

        /// <inheritdoc />
        public float DelayedValue => _engine != null ? _engine.DelayedValue : 0f;

        /// <inheritdoc />
        public bool IsTransitioning => _engine != null && _engine.IsTransitioning;

        /// <inheritdoc />
        public event ProgressBarUpdateHandler OnValueChanged;

        /// <inheritdoc />
        public event ProgressBarCompleteHandler OnComplete;

        private void Awake()
        {
            _engine = new ProgressBarEngine(_config);
        }

        /// <inheritdoc />
        public void Set(float value)
        {
            _engine.SetValue(value);
            OnValueChanged?.Invoke(_engine.Value);
        }

        /// <inheritdoc />
        public void TransitionTo(float targetValue)
        {
            _engine.BeginTransition(targetValue);
            EnsureRunning();
        }

        /// <inheritdoc />
        public void Add(float delta)
        {
            float target = _engine != null ? _engine.TargetValue + delta : delta;
            TransitionTo(target);
        }

        /// <inheritdoc />
        public void Subtract(float delta)
        {
            float target = _engine != null ? _engine.TargetValue - delta : -delta;
            TransitionTo(target);
        }

        /// <summary>
        /// 异步过渡到目标值，等待过渡完成后返回。
        /// </summary>
        /// <param name="targetValue">目标归一化进度值 0~1</param>
        /// <param name="token">取消令牌</param>
        public async UniTask TransitionToAsync(float targetValue, CancellationToken token = default)
        {
            _engine.BeginTransition(targetValue);
            EnsureRunning();

            try
            {
                await UniTask.WaitWhile(
                    () => _engine.IsTransitioning,
                    cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                _engine.Stop();
                throw;
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            CancelTick();
            _engine?.Stop();
        }

        /// <inheritdoc />
        public void Reset()
        {
            CancelTick();
            _engine?.Reset();
            OnValueChanged?.Invoke(0f);
        }

        private void EnsureRunning()
        {
            if (_tickCts == null || _tickCts.IsCancellationRequested)
            {
                _tickCts?.Dispose();
                _tickCts = new CancellationTokenSource();
                TickLoop(_tickCts.Token).Forget();
            }
        }

        private async UniTaskVoid TickLoop(CancellationToken token)
        {
            try
            {
                while (_engine.IsTransitioning)
                {
                    await UniTask.Yield(token);

                    var frameData = _engine.Tick(Time.deltaTime);
                    OnValueChanged?.Invoke(frameData.Value);

                    if (frameData.ValueTransitionComplete && frameData.Value >= 0.9999f)
                    {
                        OnComplete?.Invoke();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 取消时静默退出
            }
            finally
            {
                _tickCts?.Dispose();
                _tickCts = null;
            }
        }

        private void CancelTick()
        {
            if (_tickCts != null)
            {
                _tickCts.Cancel();
            }
        }

        private void OnDestroy()
        {
            CancelTick();
        }
    }
}

#endif
