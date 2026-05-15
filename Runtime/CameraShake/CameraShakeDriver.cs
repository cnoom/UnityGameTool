using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace CNoom.UnityGameTool.CameraShake
{
    /// <summary>
    /// 屏幕震动驱动组件。通过偏移 Camera 的 localPosition/localRotation 实现震动效果。
    /// 零外部依赖，开箱即用。支持多震源同时叠加。
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraShakeDriver : MonoBehaviour, ICameraShake
    {
        [Header("默认震动配置")]
        [Tooltip("默认震动配置，Shake() 无参时使用")]
        [SerializeField]
        private CameraShakeConfig _defaultConfig = new CameraShakeConfig();

        private CameraShakeEngine _engine;
        private Coroutine _tickCoroutine;
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

        /// <inheritdoc />
        public void StopAll()
        {
            StopDriver();
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
                    StopDriver();
                    RestoreTransform();
                }
            }
        }

        private void EnsureRunning()
        {
            CaptureOriginal();
            if (_tickCoroutine == null)
            {
                _tickCoroutine = StartCoroutine(TickRoutine());
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

        /// <summary>
        /// 重置原始位置缓存。如果外部代码修改了 Camera 的 localPosition，
        /// 调用此方法使下次震动捕获最新位置。
        /// </summary>
        public void ResetOrigin()
        {
            _hasOriginal = false;
        }

        private IEnumerator TickRoutine()
        {
            do
            {
                var offset = _engine.Tick(Time.deltaTime);

                // 触发完成回调
                _completedIds.Clear();
                _engine.GetCompletedIds(_completedIds);
                for (int i = 0; i < _completedIds.Count; i++)
                {
                    OnShakeComplete?.Invoke(_completedIds[i]);
                }

                // 应用位置偏移
                transform.localPosition = _originalLocalPos + new Vector3(offset.X, offset.Y, 0f);

                // 应用旋转偏移
                if (offset.ZRotation != 0f)
                {
                    transform.localRotation = _originalLocalRot * Quaternion.Euler(0f, 0f, offset.ZRotation);
                }
                else
                {
                    transform.localRotation = _originalLocalRot;
                }

                yield return null;
            } while (_engine.IsShaking);

            _tickCoroutine = null;
            RestoreTransform();
        }

        private void StopDriver()
        {
            if (_tickCoroutine != null)
            {
                StopCoroutine(_tickCoroutine);
                _tickCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            StopDriver();
        }
    }
}
