using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.Typewriter
{
    /// <summary>
    /// 打字动画驱动组件。将打字机逐字显示与文字入场动画融为一体。
    /// 薄封装层：持有 TypewriterAnimatorEngine，通过协程驱动逐字揭示，
    /// 通过 Update 驱动每帧顶点动画。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterAnimator : MonoBehaviour, ITypewriter
    {
        [Header("打字动画配置")]
        [Tooltip("打字动画效果配置，合并了打字速度和入场动画参数")]
        [SerializeField]
        private TypewriterAnimatorConfig _config = new TypewriterAnimatorConfig();

        private TypewriterAnimatorEngine _engine;
        private TMP_Text _textComponent;
        private Coroutine _playCoroutine;

        // 原始网格数据缓存
        private bool _meshCached;
        private Vector3[][] _cachedVertices;
        private Color32[][] _cachedColors;

        /// <inheritdoc />
        public bool IsPlaying => _engine != null && _engine.IsPlaying;

        /// <inheritdoc />
        public event TypewriterCompleteHandler OnComplete;

        /// <inheritdoc />
        public event TypewriterCharacterHandler OnCharacterTyped;

        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
            _engine = new TypewriterAnimatorEngine(_config);
            enabled = false;
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
            StopDriver();
            _engine?.Stop();
            enabled = false;
        }

        /// <inheritdoc />
        public void Skip()
        {
            if (_engine == null || !_engine.IsPlaying) return;

            StopDriver();
            _engine.SkipToEnd();
            _textComponent.maxVisibleCharacters = _engine.TotalCharacters;
            enabled = false;
            RestoreMesh();

            OnComplete?.Invoke();
        }

        /// <summary>
        /// 运行时替换配置。会重建引擎实例，正在播放的动画将被终止。
        /// </summary>
        public void SetConfig(TypewriterAnimatorConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (_engine != null && _engine.IsPlaying)
            {
                _engine.Stop();
                RestoreMesh();
                enabled = false;
            }
            _config = config;
            _engine = new TypewriterAnimatorEngine(_config);
        }

        // ============================================================
        // 核心协程：逐字揭示
        // ============================================================

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

            // 缓存原始网格，启动帧动画
            CacheOriginalMesh();
            enabled = true;

            while (_engine.IsTyping)
            {
                int index = _engine.CurrentIndex;

                // 边界检查：防止 TMP 富文本解析导致 characterCount 变化
                if (index >= _textComponent.textInfo.characterCount)
                {
                    break;
                }

                char c = _textComponent.textInfo.characterInfo[index].character;
                var (hasMore, delay) = _engine.Advance(c);

                _textComponent.maxVisibleCharacters = _engine.CurrentIndex;
                _engine.UpdateCharCount(_engine.CurrentIndex);

                OnCharacterTyped?.Invoke(index, c);

                if (!hasMore)
                {
                    break;
                }

                // 使用累计计时替代 WaitForSeconds，避免每字符 GC 分配
                if (delay > 0f)
                {
                    float timer = 0f;
                    while (timer < delay)
                    {
                        yield return null;
                        timer += Time.deltaTime;
                    }
                }
            }

            // 打字完成，动画引擎在 Update 中继续运行直到所有字符归位
            _playCoroutine = null;
        }

        // ============================================================
        // 帧更新：驱动 Engine.Tick + 应用到 TMP 网格顶点
        // ============================================================

        private void Update()
        {
            if (_engine == null)
            {
                enabled = false;
                return;
            }

            bool stillAnimating = _engine.Tick(Time.deltaTime);
            ApplyAnimationToMesh();

            if (!stillAnimating && !_engine.IsTyping)
            {
                enabled = false;
                RestoreMesh();
                OnComplete?.Invoke();
            }
        }

        // ============================================================
        // TMP 网格顶点操作
        // ============================================================

        /// <summary>
        /// 将引擎计算结果应用到 TMP 网格顶点。
        /// </summary>
        private void ApplyAnimationToMesh()
        {
            RestoreOriginalMesh();

            var textInfo = _textComponent.textInfo;
            if (textInfo == null) return;

            int engineIndex = 0;
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;

                if (engineIndex < _engine.CharCount)
                {
                    ApplyCharAnimation(engineIndex, textInfo.characterInfo[i]);
                }

                engineIndex++;
            }

            _textComponent.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Colors32 |
                TMP_VertexDataUpdateFlags.Vertices);
        }

        /// <summary>
        /// 将单个字符的动画数据应用到其 TMP 顶点。
        /// </summary>
        private void ApplyCharAnimation(int engineIndex, TMP_CharacterInfo charInfo)
        {
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

            // 缩放（围绕字符中心）
            if (data.Scale != 1f)
            {
                Vector3 center = (vertices[vertexIndex + 0] +
                                  vertices[vertexIndex + 2]) * 0.5f;
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
        /// 缓存 TMP 原始网格数据。
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

        /// <summary>
        /// 恢复 TMP 网格到原始状态并清除缓存。
        /// </summary>
        private void RestoreMesh()
        {
            _meshCached = false;
            _engine?.Reset();
            if (_textComponent != null)
            {
                _textComponent.ForceMeshUpdate(true);
            }
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
