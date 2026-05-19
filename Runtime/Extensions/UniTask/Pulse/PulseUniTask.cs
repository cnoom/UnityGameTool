// asmdef Version Defines，安装 UniTask 包后自动启用。

#if UNITASK_SUPPORT

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CNoom.UnityGameTool.Pulse
{
    /// <summary>
    /// 基于 UniTask 的脉冲/呼吸效果组件。支持异步等待非循环动画完成和 CancellationToken 取消。
    /// 需要安装 UniTask 包后自动编译。
    /// </summary>
    [DisallowMultipleComponent]
    public class PulseUniTask : MonoBehaviour, IPulse
    {
        [Header("默认脉冲配置")]
        [Tooltip("默认脉冲配置，Play() 无参时使用")]
        [SerializeField]
        private PulseConfig _defaultConfig = new PulseConfig();

        private PulseEngine _engine;
        private CancellationTokenSource _tickCts;

        private Vector3 _originalLocalScale;
        private Vector3 _originalLocalPos;
        private bool _hasOriginal;

        /// <inheritdoc />
        public bool IsPlaying => _engine != null && _engine.IsPlaying;

        /// <inheritdoc />
        public float CurrentScale => 1f;

        /// <inheritdoc />
        public float CurrentAlpha => 1f;

        /// <inheritdoc />
        public float CurrentYOffset => 0f;

        /// <inheritdoc />
        public event PulseCompleteHandler OnComplete;

        private void Awake()
        {
            _engine = new PulseEngine();
        }

        /// <inheritdoc />
        public void Play()
        {
            CaptureOriginal();
            _engine.Begin(_defaultConfig);
            EnsureRunning();
        }

        /// <inheritdoc />
        public void Play(PulseConfig config)
        {
            CaptureOriginal();
            _engine.Begin(config);
            EnsureRunning();
        }

        /// <summary>
        /// 异步播放非循环脉冲动画，等待完成后返回。
        /// </summary>
        /// <param name="config">脉冲配置</param>
        /// <param name="token">取消令牌</param>
        public async UniTask PlayAsync(PulseConfig config, CancellationToken token = default)
        {
            CaptureOriginal();
            _engine.Begin(config);
            EnsureRunning();

            try
            {
                await UniTask.WaitWhile(
                    () => _engine.IsPlaying,
                    cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                _engine.Stop();
                RestoreOriginal();
                throw;
            }
        }

        /// <inheritdoc />
        public void Pause()
        {
            _engine?.Pause();
        }

        /// <inheritdoc />
        public void Resume()
        {
            _engine?.Resume();
        }

        /// <inheritdoc />
        public void Stop()
        {
            CancelTick();
            _engine?.Stop();
            RestoreOriginal();
        }

        /// <inheritdoc />
        public void Skip()
        {
            if (_engine != null && (_engine.IsPlaying || _engine.IsPaused))
            {
                CancelTick();
                _engine.SkipToEnd();
                RestoreOriginal();
                OnComplete?.Invoke();
            }
        }

        private void CaptureOriginal()
        {
            if (!_hasOriginal)
            {
                _originalLocalScale = transform.localScale;
                _originalLocalPos = transform.localPosition;
                _hasOriginal = true;
            }
        }

        private void RestoreOriginal()
        {
            if (_hasOriginal)
            {
                transform.localScale = _originalLocalScale;
                transform.localPosition = _originalLocalPos;
                _hasOriginal = false;
            }
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
                while (_engine.IsPlaying)
                {
                    await UniTask.Yield(token);

                    var frameData = _engine.Tick(Time.deltaTime);

                    if (frameData.Completed)
                    {
                        RestoreOriginal();
                        OnComplete?.Invoke();
                        return;
                    }

                    if (frameData.Scale != 1f)
                    {
                        transform.localScale = _originalLocalScale * frameData.Scale;
                    }

                    if (frameData.YOffset != 0f)
                    {
                        transform.localPosition = _originalLocalPos + new Vector3(0f, frameData.YOffset, 0f);
                    }

                    OnAlphaChanged(frameData.Alpha);
                }

                RestoreOriginal();
            }
            catch (OperationCanceledException)
            {
                RestoreOriginal();
            }
            finally
            {
                _tickCts?.Dispose();
                _tickCts = null;
            }
        }

        /// <summary>
        /// Alpha 变化时调用。默认通过 CanvasGroup 应用。
        /// </summary>
        protected virtual void OnAlphaChanged(float alpha)
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
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
