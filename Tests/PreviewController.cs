using System.Collections;
using System.Collections.Generic;
using CNoom.UnityGameTool.CameraShake;
using CNoom.UnityGameTool.Dialogue;
using CNoom.UnityGameTool.Pulse;
using CNoom.UnityGameTool.ProgressBar;
using CNoom.UnityGameTool.ScreenFlash;
using CNoom.UnityGameTool.TextAnimation;
using CNoom.UnityGameTool.Timer;
using CNoom.UnityGameTool.Typewriter;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CNoom.UnityGameTool.Tests
{
    /// <summary>
    /// 预览场景控制器。自动创建 EventSystem、所有模块组件和 UI，
    /// 挂载到 Canvas 上即可使用，无需手动配置场景物体。
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class PreviewController : MonoBehaviour
    {
        private TypewriterCoroutine _typewriter;
        private TextAnimationDriver _textAnimation;
        private ScreenFlashDriver _screenFlash;
        private PulseDriver _pulse;
        private ProgressBarDriver _progressBar;
        private TimerDriver _timer;
        private DialogueSequencerDriver _dialogue;
        private CameraShakeDriver _cameraShake;

        private TMP_Text _statusText;
        private Image _progressFillImage;
        private TMP_Text _timerText;
        private TMP_Text _typewriterText;
        private TMP_Text _dialogueText;
        private GameObject _pulseVisual;

        private List<GameObject> _displaySections = new List<GameObject>();
        private float _progressValue;

        private void Start()
        {
            EnsureEventSystem();
            CreateModuleObjects();
            CreateUI();
        }

        // ============================================================
        // 初始化
        // ============================================================

        /// <summary>
        /// 确保 EventSystem 存在，缺失时自动创建。
        /// </summary>
        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }
        }

        /// <summary>
        /// 动态创建所有模块所需的 GameObject 和组件。
        /// </summary>
        private void CreateModuleObjects()
        {
            // --- 打字机 + 文字动画 ---
            var twSection = CreateSection("TypewriterSection");
            _typewriterText = AddLabel(twSection, "TypewriterText",
                new Vector2(0.05f, 0f), new Vector2(0.95f, 1f), 28,
                TextAlignmentOptions.Center, Color.white, "");
            _typewriter = twSection.AddComponent<TypewriterCoroutine>();
            _textAnimation = twSection.AddComponent<TextAnimationDriver>();

            // --- 脉冲效果 ---
            var pulseSection = CreateSection("PulseSection");
            _pulseVisual = CreateUIObject("PulseBox", pulseSection.transform,
                new Vector2(0.35f, 0.1f), new Vector2(0.65f, 0.9f));
            var pulseImg = _pulseVisual.AddComponent<Image>();
            pulseImg.color = new Color(0.2f, 0.8f, 0.4f, 1f);
            _pulseVisual.AddComponent<CanvasRenderer>();
            _pulseVisual.AddComponent<CanvasGroup>();
            _pulse = _pulseVisual.AddComponent<PulseDriver>();

            // --- 进度条 ---
            var pbSection = CreateSection("ProgressBarSection");
            var pbBg = CreateUIObject("BarBg", pbSection.transform,
                new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.65f));
            var bgImg = pbBg.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            pbBg.AddComponent<CanvasRenderer>();
            _progressBar = pbBg.AddComponent<ProgressBarDriver>();

            var pbFill = CreateUIObject("Fill", pbBg.transform,
                Vector2.zero, new Vector2(1f, 1f));
            _progressFillImage = pbFill.AddComponent<Image>();
            _progressFillImage.color = new Color(0.2f, 0.7f, 1f, 1f);
            pbFill.AddComponent<CanvasRenderer>();
            _progressBar.OnValueChanged += v =>
            {
                _progressFillImage.rectTransform.anchorMax =
                    new Vector2(Mathf.Clamp01(v), 1f);
            };

            // --- 计时器 ---
            var timerSection = CreateSection("TimerSection");
            _timerText = AddLabel(timerSection, "TimerText",
                new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.9f), 56,
                TextAlignmentOptions.Center, Color.yellow, "");

            var timerRoot = new GameObject("TimerRoot");
            _timer = timerRoot.AddComponent<TimerDriver>();
            _timer.OnUpdate += (elapsed, remaining) =>
            {
                _timerText.text = remaining.ToString("F2");
                _timerText.color = remaining <= 2f ? Color.red : Color.yellow;
            };
            _timer.OnComplete += () =>
            {
                _timerText.text = "完成!";
                _timerText.color = Color.green;
            };

            // --- 屏幕闪烁（覆盖全屏） ---
            var sfObj = CreateUIObject("ScreenFlashHost", transform,
                Vector2.zero, Vector2.one);
            sfObj.AddComponent<CanvasRenderer>();
            _screenFlash = sfObj.AddComponent<ScreenFlashDriver>();

            // --- 对话系统 ---
            var dlgSection = CreateSection("DialogueSection");
            var dlgBg = dlgSection.AddComponent<Image>();
            dlgBg.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);
            dlgBg.raycastTarget = false;

            _dialogueText = AddLabel(dlgSection, "DialogueText",
                new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), 24,
                TextAlignmentOptions.TopLeft, Color.white, "");

            // 对话系统自带打字机
            var dlgTw = dlgSection.AddComponent<TypewriterCoroutine>();
            _dialogue = dlgSection.AddComponent<DialogueSequencerDriver>();
            _dialogue.OnDialogueComplete += () =>
            {
                _statusText.text = "对话结束";
            };

            // --- 摄像机震动（挂到 Main Camera 上） ---
            var cam = Camera.main;
            if (cam != null)
            {
                _cameraShake = cam.GetComponent<CameraShakeDriver>();
                if (_cameraShake == null)
                {
                    _cameraShake = cam.gameObject.AddComponent<CameraShakeDriver>();
                }
            }

            // 默认显示打字机区域
            ShowSection("TypewriterSection");
        }

        // ============================================================
        // UI 构建
        // ============================================================

        /// <summary>
        /// 创建显示区域占位（共享中间区域）。
        /// </summary>
        private GameObject CreateSection(string name)
        {
            var obj = CreateUIObject(name, transform,
                new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.9f));
            _displaySections.Add(obj);
            obj.SetActive(false);
            return obj;
        }

        private void ShowSection(string name)
        {
            foreach (var s in _displaySections)
            {
                s.SetActive(s.name == name);
            }
        }

        /// <summary>
        /// 创建 UI 布局（状态文本 + 按钮面板）。
        /// </summary>
        private void CreateUI()
        {
            // 状态文本
            var statusObj = CreateUIObject("StatusText", transform,
                new Vector2(0.1f, 0.9f), new Vector2(0.9f, 0.97f));
            _statusText = statusObj.AddComponent<TextMeshProUGUI>();
            _statusText.fontSize = 16;
            _statusText.alignment = TextAlignmentOptions.Center;
            _statusText.color = new Color(0.7f, 0.8f, 1f, 1f);
            _statusText.text = "点击下方按钮测试各模块效果";
            statusObj.AddComponent<CanvasRenderer>();

            // 按钮面板 - 底部 25%
            var panelObj = CreateUIObject("ButtonPanel", transform,
                new Vector2(0, 0), new Vector2(1, 0.25f));

            var vlg = panelObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(12, 12, 6, 6);
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var buttons = new (string label, System.Action callback)[]
            {
                ("打字机+动画", OnTypewriterClick),
                ("屏幕闪烁", OnScreenFlashClick),
                ("脉冲效果", OnPulseClick),
                ("进度条", OnProgressBarClick),
                ("计时器5s", OnTimerClick),
                ("对话系统", OnDialogueClick),
                ("摄像机震动", OnCameraShakeClick),
            };

            // 两列布局
            for (int i = 0; i < buttons.Length; i += 2)
            {
                var rowObj = CreateUIObject("Row", panelObj.transform);
                var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 6;
                hlg.childControlWidth = true;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = false;
                hlg.childAlignment = TextAnchor.MiddleCenter;

                CreateButton(rowObj.transform, buttons[i].label, buttons[i].callback);
                if (i + 1 < buttons.Length)
                {
                    CreateButton(rowObj.transform, buttons[i + 1].label,
                        buttons[i + 1].callback);
                }
            }
        }

        // ============================================================
        // UI 辅助
        // ============================================================

        private GameObject CreateUIObject(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return obj;
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return obj;
        }

        private TMP_Text AddLabel(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, float fontSize,
            TextAlignmentOptions alignment, Color color, string text)
        {
            var obj = CreateUIObject(name, parent.transform, anchorMin, anchorMax);
            obj.AddComponent<CanvasRenderer>();
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.text = text;
            return tmp;
        }

        private void CreateButton(Transform parent, string label,
            System.Action callback)
        {
            var btnObj = new GameObject("Btn_" + label.Replace(" ", ""));
            btnObj.transform.SetParent(parent, false);

            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(0, 34);

            var image = btnObj.AddComponent<Image>();
            image.color = new Color(0.18f, 0.28f, 0.48f, 0.92f);

            var uiBtn = btnObj.AddComponent<Button>();
            var colors = uiBtn.colors;
            colors.highlightedColor = new Color(0.28f, 0.4f, 0.65f, 1f);
            colors.pressedColor = new Color(0.12f, 0.18f, 0.35f, 1f);
            uiBtn.colors = colors;

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);
            var txtRect = textObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            uiBtn.onClick.AddListener(() => callback());
        }

        // ============================================================
        // 按钮回调
        // ============================================================

        public void OnTypewriterClick()
        {
            if (_typewriter == null) return;
            ShowSection("TypewriterSection");
            _statusText.text = "打字机 + 文字动画";
            _typewriter.Play("你好世界！这是打字机效果，每个字符都会播放文字动画。");
        }

        public void OnScreenFlashClick()
        {
            if (_screenFlash == null) return;
            _statusText.text = "屏幕闪烁";
            _screenFlash.Flash(Color.white, 0.4f);
        }

        public void OnPulseClick()
        {
            if (_pulse == null) return;
            ShowSection("PulseSection");
            _statusText.text = "脉冲/呼吸效果";
            var config = new PulseConfig(PulseType.Glow);
            _pulse.Play(config);
        }

        public void OnProgressBarClick()
        {
            if (_progressBar == null) return;
            ShowSection("ProgressBarSection");
            _statusText.text = "进度条填充";
            _progressValue = 0f;
            _progressBar.Set(0f);
            _progressFillImage.rectTransform.anchorMax = new Vector2(0f, 1f);
            StartCoroutine(ProgressBarRoutine());
        }

        public void OnTimerClick()
        {
            if (_timer == null) return;
            ShowSection("TimerSection");
            _statusText.text = "倒计时 5 秒";
            _timerText.text = "5.00";
            _timerText.color = Color.yellow;
            _timer.StartCountdown(5f);
        }

        public void OnDialogueClick()
        {
            if (_dialogue == null) return;
            ShowSection("DialogueSection");
            _statusText.text = "对话系统（点击屏幕推进对话）";
            var data = new DialogueData();
            data.Segments.Add(new DialogueSegment
            {
                SpeakerName = "旁白",
                Text = "欢迎来到对话系统演示！点击屏幕继续..."
            });
            data.Segments.Add(new DialogueSegment
            {
                SpeakerName = "NPC",
                Text = "你好，这是一段对话测试。"
            });
            data.Segments.Add(new DialogueSegment
            {
                SpeakerName = "NPC",
                Text = "祝你愉快！再见！"
            });
            _dialogue.Play(data);
        }

        public void OnCameraShakeClick()
        {
            if (_cameraShake == null) return;
            _statusText.text = "摄像机震动";
            _cameraShake.Shake(1f, 0.5f);
        }

        // ============================================================
        // 协程
        // ============================================================

        private IEnumerator ProgressBarRoutine()
        {
            _progressValue = 0f;
            while (_progressValue < 1f)
            {
                _progressValue += Time.deltaTime / 2f;
                _progressBar.TransitionTo(Mathf.Clamp01(_progressValue));
                yield return null;
            }
            _statusText.text = "进度条已完成";
        }

        // ============================================================
        // 帧更新
        // ============================================================

        private void Update()
        {
            // 点击屏幕推进对话
            if (_dialogue != null
                && _dialogue.State == DialogueState.WaitingForInput)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _dialogue.Next();
                }
            }
        }
    }
}
