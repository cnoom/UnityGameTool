using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using CNoom.UnityGameTool.CameraShake;
using CNoom.UnityGameTool.Dialogue;
using CNoom.UnityGameTool.Pulse;
using CNoom.UnityGameTool.ProgressBar;
using CNoom.UnityGameTool.ScreenFlash;
using CNoom.UnityGameTool.TextAnimation;
using CNoom.UnityGameTool.Timer;
using CNoom.UnityGameTool.Typewriter;
using TMPro;

namespace CNoom.UnityGameTool.Tests
{
    /// <summary>
    /// 预览场景控制器。自动创建按钮面板，触发各模块效果。
    /// 挂载到 Canvas 上即可使用。
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class PreviewController : MonoBehaviour
    {
        [Header("引用（留空则自动查找）")]
        [SerializeField] private TypewriterCoroutine _typewriter;
        [SerializeField] private TextAnimationDriver _textAnimation;
        [SerializeField] private ScreenFlashDriver _screenFlash;
        [SerializeField] private PulseDriver _pulse;
        [SerializeField] private ProgressBarDriver _progressBar;
        [SerializeField] private TimerDriver _timer;
        [SerializeField] private DialogueSequencerDriver _dialogue;
        [SerializeField] private CameraShakeDriver _cameraShake;

        private TMP_Text _displayText;
        private float _progressValue;

        private void Start()
        {
            AutoResolveReferences();
            CreateDisplayText();
            CreateButtonPanel();
        }

        private void AutoResolveReferences()
        {
            var all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var obj in all)
            {
                if (_typewriter == null) _typewriter = obj as TypewriterCoroutine;
                if (_textAnimation == null) _textAnimation = obj as TextAnimationDriver;
                if (_screenFlash == null) _screenFlash = obj as ScreenFlashDriver;
                if (_pulse == null) _pulse = obj as PulseDriver;
                if (_progressBar == null) _progressBar = obj as ProgressBarDriver;
                if (_timer == null) _timer = obj as TimerDriver;
                if (_dialogue == null) _dialogue = obj as DialogueSequencerDriver;
                if (_cameraShake == null) _cameraShake = obj as CameraShakeDriver;
            }
        }

        /// <summary>
        /// 创建中央文本区域。
        /// </summary>
        private void CreateDisplayText()
        {
            var textAreaObj = new GameObject("DisplayText");
            textAreaObj.transform.SetParent(transform, false);

            var rect = textAreaObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.5f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _displayText = textAreaObj.AddComponent<TextMeshProUGUI>();
            _displayText.fontSize = 30;
            _displayText.alignment = TextAlignmentOptions.Center;
            _displayText.color = Color.white;
            _displayText.text = "点击下方按钮测试效果";
        }

        private void CreateButtonPanel()
        {
            var panelObj = new GameObject("ButtonPanel");
            panelObj.transform.SetParent(transform, false);

            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0.45f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var vlg = panelObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(20, 20, 15, 15);
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var buttons = new List<(string label, UnityAction callback)>
            {
                ("打字机 + 文字动画", OnTypewriterClick),
                ("屏幕闪烁", OnScreenFlashClick),
                ("脉冲效果", OnPulseClick),
                ("进度条", OnProgressBarClick),
                ("计时器 (5s)", OnTimerClick),
                ("对话系统", OnDialogueClick),
                ("摄像机震动", OnCameraShakeClick),
            };

            foreach (var (label, callback) in buttons)
            {
                CreateButton(panelObj.transform, label, callback);
            }
        }

        private void CreateButton(Transform parent, string label, UnityAction callback)
        {
            var btnObj = new GameObject("Btn_" + label.Replace(" ", ""));
            btnObj.transform.SetParent(parent, false);

            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(0, 42);

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
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            uiBtn.onClick.AddListener(callback);
        }

        // === 按钮回调 ===

        public void OnTypewriterClick()
        {
            if (_typewriter == null) return;
            _typewriter.Play("你好！这是一个打字机效果演示，同时每个字符都会播放动画效果。");
        }

        public void OnScreenFlashClick()
        {
            if (_screenFlash == null) return;
            var config = new ScreenFlashConfig(Color.white, 0.3f);
            _screenFlash.Flash(config);
        }

        public void OnPulseClick()
        {
            if (_pulse == null) return;
            var config = new PulseConfig(PulseType.Glow);
            _pulse.Play(config);
        }

        public void OnProgressBarClick()
        {
            if (_progressBar == null) return;
            _progressValue = 0f;
            _progressBar.Set(0f);
            StartCoroutine(ProgressBarRoutine());
        }

        public void OnTimerClick()
        {
            if (_timer == null) return;
            _timer.StartCountdown(5f);
        }

        public void OnDialogueClick()
        {
            if (_dialogue == null) return;
            var data = new DialogueData();
            data.Segments.Add(new DialogueSegment
            {
                SpeakerName = "NPC",
                Text = "欢迎来到对话系统演示！点击继续..."
            });
            data.Segments.Add(new DialogueSegment
            {
                SpeakerName = "NPC",
                Text = "这是第二段对话，可以继续点击推进。"
            });
            data.Segments.Add(new DialogueSegment
            {
                SpeakerName = "NPC",
                Text = "谢谢观看！",
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice { Text = "再见", NextSegmentIndex = -1 }
                }
            });
            _dialogue.Play(data);
        }

        public void OnCameraShakeClick()
        {
            if (_cameraShake == null) return;
            _cameraShake.Shake(0.5f, 5f);
        }

        private System.Collections.IEnumerator ProgressBarRoutine()
        {
            while (_progressValue < 1f)
            {
                _progressValue += Time.deltaTime / 2f;
                _progressBar.TransitionTo(Mathf.Clamp01(_progressValue));
                yield return null;
            }
        }
    }
}
