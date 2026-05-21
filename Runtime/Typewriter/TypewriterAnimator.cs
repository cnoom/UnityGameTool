using System;
using System.Collections;
using CNoom.UnityGameTool.TextAnimation;
using TMPro;
using UnityEngine;

namespace CNoom.UnityGameTool.Typewriter
{
    /// <summary>
    /// 打字动画组件。将打字机逐字显示与文字入场动画融为一体，
    /// 无需额外挂载 TextAnimationDriver。
    /// 内部同时持有 TypewriterEngine（节奏）和 TextAnimationEngine（动画），
    /// 共享同一份时间轴，彻底解决同步问题。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterAnimator : MonoBehaviour, ITypewriter
    {
        [Header("打字机配置")]
        [Tooltip("打字机效果配置，控制逐字显示速度、标点延迟等")]
        [SerializeField]
        private TypewriterConfig _typewriterConfig = new TypewriterConfig();

        [Header("文字动画配置")]
        [Tooltip("逐字入场动画配置，控制每个字符的入场效果")]
        [SerializeField]
        private TextAnimationConfig _animationConfig = new TextAnimationConfig(
            TextAnimationType.Bounce,
            TextAnimationPlayMode.Once,
            duration: 0.35f,
            speed: 1f,
            amplitude: 18f,
            frequency: 2f,
            charDelay: 0.03f
        );

        private TypewriterEngine _typewriterEngine;
        private TextAnimationEngine _animationEngine;
        private TMP_Text _textComponent;
        private Coroutine _playCoroutine;

        // 原始网格数据缓存
        private bool _meshCached;
        private Vector3[][] _cachedVertices;
        private Color32[][] _cachedColors;

        /// <inheritdoc />
        public bool IsPlaying =>
            (_typewriterEngine != null && _typewriterEngine.IsPlaying) ||
            (_animationEngine != null && _animationEngine.IsPlaying);

        /// <inheritdoc />
        public event TypewriterCompleteHandler OnComplete;

        /// <inheritdoc />
        public event TypewriterCharacterHandler OnCharacterTyped;

        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
            _typewriterEngine = new TypewriterEngine(_typewriterConfig);
            _animationEngine = new TextAnimationEngine(_animationConfig);
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
            _typewriterEngine?.Stop();
            _animationEngine?.Stop();
        }

        /// <inheritdoc />
        public void Skip()
        {
            if (_typewriterEngine == null || !_typewriterEngine.IsPlaying) return;

            StopDriver();
            _typewriterEngine.SkipToEnd();
            _animationEngine.SkipToEnd();
            _textComponent.maxVisibleCharacters = _typewriterEngine.TotalCharacters;
            RestoreMesh();

            OnComplete?.Invoke();
        }

        /// <summary>
        /// 运行时替换打字机配置。
        /// </summary>
        public void SetTypewriterConfig(TypewriterConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _typewriterConfig = config;
            _typewriterEngine = new TypewriterEngine(_typewriterConfig);
        }

        /// <summary>
        /// 运行时替换动画配置。
        /// </summary>
        public void SetAnimationConfig(TextAnimationConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _animationConfig = config;
            _animationEngine = new TextAnimationEngine(_animationConfig);
        }

        // ============================================================
        // 核心协程：逐字显示 + 入场动画
        // ============================================================

        private IEnumerator PlayRoutine(string text)
        {
            _textComponent.text = text ?? string.Empty;
            _textComponent.ForceMeshUpdate(true);

            int total = _textComponent.textInfo.characterCount;
            _typewriterEngine.Begin(total);
            _textComponent.maxVisibleCharacters = 0;

            if (total == 0)
            {
                _playCoroutine = null;
                OnComplete?.Invoke();
                yield break;
            }

            // 启动动画引擎（仅第一个字符），并缓存原始网格
            _animationEngine.Begin(1);
            CacheOriginalMesh();
            enabled = true;

            while (_typewriterEngine.IsPlaying)
            {
                int index = _typewriterEngine.CurrentIndex;

                // 边界检查：防止 TMP 富文本解析导致 characterCount 变化
                if (index >= _textComponent.textInfo.characterCount)
                {
                    break;
                }

                char c = _textComponent.textInfo.characterInfo[index].character;
                var (hasMore, delay) = _typewriterEngine.Advance(c);

                _textComponent.maxVisibleCharacters = _typewriterEngine.CurrentIndex;

                // 同步扩展动画引擎的字符数（不重置时间轴）
                _animationEngine.UpdateCharCount(_typewriterEngine.CurrentIndex);

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

            // 打字完成，等待所有字符的入场动画播完
            yield return null;

            // 动画引擎继续在 Update 中运行直到所有字符归位
            // 当动画结束时 Update 会自动禁用组件并触发 OnComplete
            _playCoroutine = null;
        }

        // ============================================================
        // 帧更新：应用文字动画到 TMP 网格顶点
        // ============================================================

        private void Update()
        {
            if (_animationEngine == null)
            {
                enabled = false;
                return;
            }

            bool stillAnimating = _animationEngine.Tick(Time.deltaTime);
            ApplyAnimationToMesh();

            // 打字已完成且动画也播完
            if (!stillAnimating && (_typewriterEngine == null || !_typewriterEngine.IsPlaying))
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
        /// 将动画引擎计算结果应用到 TMP 网格顶点。
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

                if (engineIndex < _animationEngine.CharCount)
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
            ref readonly var data = ref _animationEngine.GetCharData(engineIndex);
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
            _animationEngine?.Reset();
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
