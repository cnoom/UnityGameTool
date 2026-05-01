// asmdef Version Defines，安装 UniTask 包后自动启用。

#if UNITASK_SUPPORT

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CNoom.UnityGameTool.CameraShake
{
    /// <summary>
    /// 基于 UniTask 的屏幕震动组件。支持异步等待单次震动完成和 CancellationToken 取消。
    /// 需要安装 UniTask 包后自动编译。
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraShakeUniTask : MonoBehaviour, ICameraShake
    {
        [Header("默认震动配置")]
        [Tooltip("默认震动配置，Shake() 无参时使用")]
        [SerializeField]
        private CameraShakeConfig _defaultConfig = new CameraShakeConfig();

        private CameraShakeEngine _engine;
        private CancellationTokenSource _tickCts;
        private Vector3 _originalLocalPos;
        private Quaternion _originalLocalRot;
        private bool _hasOriginal;
        private readonly List<int> _completedIds = new List<int>();

        /// <inheritdoc />
        public bool IsShaking => _engine != null && _engine.IsShaking;

        /// <inheritdoc />
        public int ActiveShakeCount => _engine != null ? _engine.ActiveShakeCount : 0;

        /// <inheritdoc />
        public event ShakeCompleteHandler OnShakeComplete;

        private void Awake()
        {
            _engine = new CameraShakeEngine(_defaultConfig);
        }

        /// <inheritdoc />
        public int Shake()
        {
            int id = _engine.AddShake(null);
            EnsureRunning();
            return id;
        }

        /// <inheritdoc />
        public int Shake(CameraShakeConfig config)
        {
            int id = _engine.AddShake(config);
            EnsureRunning();
            return id;
        }

        /// <inheritdoc />
        public int Shake(float intensity, float duration)
        {
            var config = new CameraShakeConfig(intensity, duration);
            int id = _engine.AddShake(config);
            EnsureRunning();
            return id;
        }

        /// <summary>
        /// 异步触发一次震动，等待该震动完成后返回。
        /// 注意：如果有其他震动源仍在活跃，整体震动未结束。
        /// </summary>
        /// <param name="config">震动配置，null 则使用默认配置</param>
        /// <param name="token">取消令牌</param>
        public async UniTask ShakeAsync(CameraShakeConfig config = null, CancellationToken token = default)
        {
            int id = _engine.AddShake(config);
            EnsureRunning();

            // 等待该震动源完成（查询指定 ID 是否仍活跃）
            try
            {
                await UniTask.WaitWhile(
                    () => _engine.IsActive(id),
                    cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                _engine.Stop(id);
                RestoreTransform();
                throw;
            }
        }

        /// <inheritdoc />
        public void StopAll()
        {
            CancelTick();
            _engine?.StopAll();
            RestoreTransform();
        }

        /// <inheritdoc />
        public void Stop(int shakeId)
        {
            if (_engine != null && _engine.Stop(shakeId))
            {
                if (!_engine.IsShaking)
                {
                    CancelTick();
                    RestoreTransform();
                }
            }
        }

        private void EnsureRunning()
        {
            CaptureOriginal();
            if (_tickCts == null || _tickCts.IsCancellationRequested)
            {
                _tickCts?.Dispose();
                _tickCts = new CancellationTokenSource();
                TickLoop(_tickCts.Token).Forget();
            }
        }

        private void CaptureOriginal()
        {
            if (!_hasOriginal)
            {
                _originalLocalPos = transform.localPosition;
                _originalLocalRot = transform.localRotation;
                _hasOriginal = true;
            }
        }

        private void RestoreTransform()
        {
            if (_hasOriginal)
            {
                transform.localPosition = _originalLocalPos;
                transform.localRotation = _originalLocalRot;
                _hasOriginal = false;
            }
        }

        private async UniTaskVoid TickLoop(CancellationToken token)
        {
            try
            {
                while (_engine.IsShaking)
                {
                    await UniTask.Yield(token);

                    var offset = _engine.Tick(Time.deltaTime);

                    transform.localPosition = _originalLocalPos + new Vector3(offset.X, offset.Y, 0f);

                    if (offset.ZRotation != 0f)
                    {
                        transform.localRotation = _originalLocalRot * Quaternion.Euler(0f, 0f, offset.ZRotation);
                    }
                    else
                    {
                        transform.localRotation = _originalLocalRot;
                    }

                    // 触发完成回调
                    _completedIds.Clear();
                    _engine.GetCompletedIds(_completedIds);
                    for (int i = 0; i < _completedIds.Count; i++)
                    {
                        OnShakeComplete?.Invoke(_completedIds[i]);
                    }
                }

                RestoreTransform();
            }
            catch (OperationCanceledException)
            {
                RestoreTransform();
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
                // Dispose 在 TickLoop 的 finally 中完成
            }
        }

        private void OnDestroy()
        {
            CancelTick();
        }
    }
}

#endif
