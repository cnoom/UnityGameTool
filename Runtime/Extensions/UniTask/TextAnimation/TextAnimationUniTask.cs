// asmdef Version Defines，安装 UniTask 包后自动启用。

#if UNITASK_SUPPORT

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.TextAnimation
{
    /// <summary>
    /// 基于 UniTask 的文字动画组件。支持异步等待完成和 CancellationToken 取消。
    /// 需要安装 UniTask 包后自动编译。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TextAnimationUniTask : MonoBehaviour, ITextAnimation
    {
        [Header("动画配置")]
        [Tooltip("文字动画效果配置，可直接在 Inspector 中编辑")]
        [SerializeField]
        private TextAnimationConfig _config = new TextAnimationConfig();

        private TextAnimationEngine _engine;
        private TMP_Text _textComponent;
        private CancellationTokenSource _playCts;
        private bool _isPlaying;

        // 原始网格数据缓存，每帧先恢复再应用动画，避免修改累积
        private bool _meshCached;
        private Vector3[][] _cachedVertices;
        private Color32[][] _cachedColors;

        /// <inheritdoc />
        public bool IsPlaying => _isPlaying;

        /// <inheritdoc />
        public event TextAnimationCompleteHandler OnComplete;

        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
            _engine = new TextAnimationEngine(_config);
            enabled = false;
        }

        /// <inheritdoc />
        public void Play(int visibleCharacterCount)
        {
            PlayAsync(visibleCharacterCount).Forget();
        }

        /// <summary>
        /// 异步播放文字动画，支持 await 等待完成。
        /// </summary>
        /// <param name="visibleCharacterCount">当前可见字符总数</param>
        public async UniTask PlayAsync(int visibleCharacterCount)
        {
            CancelPlay();
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            try
            {
                if (visibleCharacterCount <= 0) return;

                // 清理上一次动画的顶点残留
                if (_engine.IsPlaying)
                {
                    _engine.Stop();
                    RestoreMesh();
                }

                _engine.Begin(visibleCharacterCount);
                _isPlaying = true;
                enabled = true;
                CacheOriginalMesh();

                // 循环动画需要外部取消
                if (_config.IsLooping)
                {
                    await UniTask.WaitWhile(() => _isPlaying, cancellationToken: token);
                }
                else
                {
                    // 等待引擎播放完成
                    await UniTask.WaitWhile(
                        () => _engine.IsPlaying,
                        cancellationToken: token);
                }

                _isPlaying = false;
                enabled = false;
                RestoreMesh();
                OnComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // 由 Stop/Skip/Destroy 触发取消
                _isPlaying = false;
                // 对象可能已被销毁（如退出游戏时），安全访问
                if (this != null)
                {
                    enabled = false;
                }
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (_isPlaying)
            {
                _engine.Stop();
                _isPlaying = false;
                enabled = false;
                CancelPlay();
                RestoreMesh();
            }
        }

        /// <inheritdoc />
        public void Skip()
        {
            if (_isPlaying)
            {
                _engine.SkipToEnd();
                _isPlaying = false;
                enabled = false;
                CancelPlay();
                RestoreMesh();
                OnComplete?.Invoke();
            }
        }

        /// <summary>
        /// 当文本内容变化时由外部调用，更新可见字符数。
        /// </summary>
        public void UpdateVisibleCount(int visibleCharacterCount)
        {
            if (_isPlaying && visibleCharacterCount > 0)
            {
                _engine.Begin(visibleCharacterCount);
                CacheOriginalMesh();
            }
        }

        private void Update()
        {
            if (!_isPlaying || !_engine.IsPlaying) return;

            bool stillPlaying = _engine.Tick(Time.deltaTime);
            ApplyToMesh();

            if (!stillPlaying)
            {
                _isPlaying = false;
                enabled = false;
                RestoreMesh();
            }
        }

        private void ApplyToMesh()
        {
            // 先恢复原始网格数据，避免帧间修改累积
            RestoreOriginalMesh();

            var textInfo = _textComponent.textInfo;
            if (textInfo == null) return;

            int visibleCount = 0;
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (textInfo.characterInfo[i].isVisible)
                {
                    ApplyCharAnimation(visibleCount, textInfo.characterInfo[i]);
                    visibleCount++;
                }
            }

            _textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32 |
                                            TMP_VertexDataUpdateFlags.Vertices);
        }

        private void ApplyCharAnimation(int engineIndex, TMP_CharacterInfo charInfo)
        {
            if (engineIndex >= _engine.CharCount) return;

            ref readonly var data = ref _engine.GetCharData(engineIndex);
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            var meshInfo = _textComponent.textInfo.meshInfo[materialIndex];
            var vertices = meshInfo.vertices;
            var colors = meshInfo.colors32;

            // 位移偏移
            if (data.XOffset != 0f || data.YOffset != 0f)
            {
                Vector3 offset = new Vector3(data.XOffset, data.YOffset, 0f);
                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }

            // 缩放
            if (data.Scale != 1f)
            {
                Vector3 center = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) * 0.5f;
                for (int j = 0; j < 4; j++)
                {
                    vertices[vertexIndex + j] = center +
                        (vertices[vertexIndex + j] - center) * data.Scale;
                }
            }

            // 透明度
            if (data.Alpha < 1f)
            {
                byte alpha = (byte)(255 * Mathf.Clamp01(data.Alpha));
                for (int j = 0; j < 4; j++)
                {
                    int idx = vertexIndex + j;
                    colors[idx].a = (byte)(colors[idx].a * alpha / 255);
                }
            }
        }

        /// <summary>
        /// 恢复 TMP 网格到原始状态（动画结束时调用）。
        /// </summary>
        private void RestoreMesh()
        {
            _meshCached = false;
            _textComponent.ForceMeshUpdate(true);
        }

        /// <summary>
        /// 缓存 TMP 原始网格数据（动画开始时调用一次）。
        /// </summary>
        private void CacheOriginalMesh()
        {
            var textInfo = _textComponent.textInfo;
            if (textInfo == null) return;

            int meshCount = textInfo.meshInfo.Length;
            if (_cachedVertices == null || _cachedVertices.Length != meshCount)
            {
                _cachedVertices = new Vector3[meshCount][];
                _cachedColors = new Color32[meshCount][];
            }

            for (int i = 0; i < meshCount; i++)
            {
                var mesh = textInfo.meshInfo[i];
                int vertLen = mesh.vertices.Length;
                int colorLen = mesh.colors32.Length;

                if (_cachedVertices[i] == null || _cachedVertices[i].Length != vertLen)
                    _cachedVertices[i] = new Vector3[vertLen];
                if (_cachedColors[i] == null || _cachedColors[i].Length != colorLen)
                    _cachedColors[i] = new Color32[colorLen];

                Array.Copy(mesh.vertices, _cachedVertices[i], vertLen);
                Array.Copy(mesh.colors32, _cachedColors[i], colorLen);
            }

            _meshCached = true;
        }

        /// <summary>
        /// 从缓存恢复原始网格数据（每帧调用，避免修改累积）。
        /// </summary>
        private void RestoreOriginalMesh()
        {
            if (!_meshCached) return;

            var textInfo = _textComponent.textInfo;
            if (textInfo == null) return;

            int meshCount = Math.Min(_cachedVertices.Length, textInfo.meshInfo.Length);
            for (int i = 0; i < meshCount; i++)
            {
                var mesh = textInfo.meshInfo[i];
                int vertLen = Math.Min(_cachedVertices[i].Length, mesh.vertices.Length);
                int colorLen = Math.Min(_cachedColors[i].Length, mesh.colors32.Length);
                Array.Copy(_cachedVertices[i], mesh.vertices, vertLen);
                Array.Copy(_cachedColors[i], mesh.colors32, colorLen);
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
