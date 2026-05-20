using System;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.TextAnimation
{
    /// <summary>
    /// 文字动画驱动组件。通过修改 TMP 网格顶点实现逐字动画效果。
    /// 零外部依赖，开箱即用。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TextAnimationDriver : MonoBehaviour, ITextAnimation
    {
        [Header("动画配置")]
        [Tooltip("文字动画效果配置，可直接在 Inspector 中编辑")]
        [SerializeField]
        private TextAnimationConfig _config = new TextAnimationConfig();

        private TextAnimationEngine _engine;
        private TMP_Text _textComponent;

        // 原始网格数据缓存，每帧先恢复再应用动画，避免修改累积
        private bool _meshCached;
        private Vector3[][] _cachedVertices;
        private Color32[][] _cachedColors;

        /// <inheritdoc />
        public bool IsPlaying => _engine != null && _engine.IsPlaying;

        /// <inheritdoc />
        public event TextAnimationCompleteHandler OnComplete;

        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
            _engine = new TextAnimationEngine(_config);
            enabled = false;
        }

        /// <summary>
        /// 运行时替换动画配置。会重建引擎实例，正在播放的动画将被终止。
        /// </summary>
        /// <param name="config">新的动画配置，不可为 null</param>
        public void SetConfig(TextAnimationConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (_engine != null && _engine.IsPlaying)
            {
                _engine.Stop();
                RestoreMesh();
                enabled = false;
            }
            _config = config;
            _engine = new TextAnimationEngine(_config);
        }

        /// <inheritdoc />
        public void Play(int visibleCharacterCount)
        {
            if (visibleCharacterCount <= 0) return;

            // 清理上一次动画的顶点残留
            if (_engine.IsPlaying)
            {
                _engine.Stop();
                RestoreMesh();
            }

            _engine.Begin(visibleCharacterCount);
            CacheOriginalMesh();
            enabled = true;
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (_engine != null && _engine.IsPlaying)
            {
                _engine.Stop();
                RestoreMesh();
                enabled = false;
            }
        }

        /// <inheritdoc />
        public void Skip()
        {
            if (_engine != null && _engine.IsPlaying)
            {
                _engine.SkipToEnd();
                RestoreMesh();
                enabled = false;
                OnComplete?.Invoke();
            }
        }

        private void Update()
        {
            bool stillPlaying = _engine.Tick(Time.deltaTime);
            ApplyToMesh();

            if (!stillPlaying)
            {
                enabled = false;
                RestoreMesh();
                OnComplete?.Invoke();
            }
        }

        /// <summary>
        /// 当文本内容变化时由外部调用，更新可见字符数。
        /// 不重置动画时间轴，保持已有字符的动画连续性。
        /// </summary>
        /// <param name="visibleCharacterCount">当前可见字符总数</param>
        public void UpdateVisibleCount(int visibleCharacterCount)
        {
            if (_engine.IsPlaying && visibleCharacterCount > 0)
            {
                _engine.UpdateCharCount(visibleCharacterCount);
                CacheOriginalMesh();
            }
        }

        /// <summary>
        /// 将引擎计算结果应用到 TMP 网格顶点。
        /// </summary>
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

        /// <summary>
        /// 将单个字符的动画数据应用到其 TMP 顶点。
        /// </summary>
        private void ApplyCharAnimation(int engineIndex, TMP_CharacterInfo charInfo)
        {
            if (engineIndex >= _engine.CharCount) return;

            ref readonly var data = ref _engine.GetCharData(engineIndex);
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            var meshInfo = _textComponent.textInfo.meshInfo[materialIndex];
            var vertices = meshInfo.vertices;
            var colors = meshInfo.colors32;

            // --- 位移偏移 ---
            if (data.XOffset != 0f || data.YOffset != 0f)
            {
                Vector3 offset = new Vector3(data.XOffset, data.YOffset, 0f);
                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }

            // --- 缩放（围绕字符中心） ---
            if (data.Scale != 1f)
            {
                Vector3 center = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) * 0.5f;
                for (int j = 0; j < 4; j++)
                {
                    vertices[vertexIndex + j] = center +
                        (vertices[vertexIndex + j] - center) * data.Scale;
                }
            }

            // --- 透明度 ---
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

        private void OnDestroy()
        {
            if (_engine != null)
            {
                _engine.Stop();
            }
        }
    }
}
