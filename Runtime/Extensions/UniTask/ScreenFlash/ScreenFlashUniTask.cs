// asmdef Version Defines，安装 UniTask 包后自动启用。

#if UNITASK_SUPPORT

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CNoom.UnityGameTool.ScreenFlash
{
    /// <summary>
    /// 基于 UniTask 的屏幕特效组件。支持异步等待单次闪烁完成和 CancellationToken 取消。
    /// 需要安装 UniTask 包后自动编译。
    /// </summary>
    [DisallowMultipleComponent]
    public class ScreenFlashUniTask : MonoBehaviour, IScreenFlash
    {
        [Header("默认闪烁配置")]
        [Tooltip("默认闪烁配置，Flash() 无参时使用")]
        [SerializeField]
        private ScreenFlashConfig _defaultConfig = new ScreenFlashConfig();

        private ScreenFlashEngine _engine;
        private CancellationTokenSource _tickCts;
        private Image _overlayImage;
        private readonly List<int> _completedIds = new List<int>();

        /// <inheritdoc />
        public bool IsPlaying => _engine != null && _engine.IsPlaying;

        /// <inheritdoc />
        public int ActiveCount => _engine != null ? _engine.ActiveCount : 0;

        /// <inheritdoc />
        public event ScreenFlashCompleteHandler OnComplete;

        private void Awake()
        {
            _engine = new ScreenFlashEngine(_defaultConfig);
            CreateOverlay();
        }

        /// <inheritdoc />
        public int Flash()
        {
            int id = _engine.AddFlash(null);
            EnsureRunning();
            return id;
        }

        /// <inheritdoc />
        public int Flash(ScreenFlashConfig config)
        {
            int id = _engine.AddFlash(config);
            EnsureRunning();
            return id;
        }

        /// <inheritdoc />
        public int Flash(Color color, float duration)
        {
            var config = new ScreenFlashConfig(color, duration);
            int id = _engine.AddFlash(config);
            EnsureRunning();
            return id;
        }

        /// <summary>
        /// 异步触发一次闪烁，等待该闪烁完成后返回。
        /// </summary>
        /// <param name="config">闪烁配置，null 则使用默认配置</param>
        /// <param name="token">取消令牌</param>
        public async UniTask FlashAsync(ScreenFlashConfig config = null, CancellationToken token = default)
        {
            int id = _engine.AddFlash(config);
            EnsureRunning();

            try
            {
                await UniTask.WaitWhile(
                    () => _engine.IsActive(id),
                    cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                _engine.Stop(id);
                HideOverlay();
                throw;
            }
        }

        /// <inheritdoc />
        public void StopAll()
        {
            CancelTick();
            _engine?.StopAll();
            HideOverlay();
        }

        /// <inheritdoc />
        public void Stop(int flashId)
        {
            if (_engine != null && _engine.Stop(flashId))
            {
                if (!_engine.IsPlaying)
                {
                    CancelTick();
                    HideOverlay();
                }
            }
        }

        private void CreateOverlay()
        {
            var go = new GameObject("ScreenFlashOverlay");
            go.transform.SetParent(transform, false);

            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            _overlayImage = go.AddComponent<Image>();
            _overlayImage.color = Color.clear;
            _overlayImage.raycastTarget = false;

            go.transform.SetAsLastSibling();
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

                    _completedIds.Clear();
                    _engine.GetCompletedIds(_completedIds);
                    for (int i = 0; i < _completedIds.Count; i++)
                    {
                        OnComplete?.Invoke(_completedIds[i]);
                    }

                    if (frameData.Alpha > 0.001f)
                    {
                        _overlayImage.enabled = true;
                        _overlayImage.color = new Color(
                            frameData.R, frameData.G, frameData.B, frameData.Alpha);
                    }
                    else
                    {
                        HideOverlay();
                    }
                }

                HideOverlay();
            }
            catch (OperationCanceledException)
            {
                HideOverlay();
            }
            finally
            {
                _tickCts?.Dispose();
                _tickCts = null;
            }
        }

        private void HideOverlay()
        {
            if (_overlayImage != null)
            {
                _overlayImage.enabled = false;
                _overlayImage.color = Color.clear;
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
