using System;
using System.Collections.Generic;

namespace CNoom.UnityGameTool.Dialogue
{
    /// <summary>
    /// 对话分支选项
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        /// <summary>选项显示文本</summary>
        public string Text;

        /// <summary>选择后跳转到的段落索引（-1 表示结束对话）</summary>
        public int NextSegmentIndex = -1;

        public DialogueChoice() { }

        public DialogueChoice(string text, int nextSegmentIndex = -1)
        {
            Text = text;
            NextSegmentIndex = nextSegmentIndex;
        }
    }

    /// <summary>
    /// 单段对话数据
    /// </summary>
    [Serializable]
    public class DialogueSegment
    {
        /// <summary>说话者名称（可为空表示旁白）</summary>
        public string SpeakerName = "";

        /// <summary>对话文本内容</summary>
        public string Text = "";

        /// <summary>分支选项列表（为空或 null 表示无分支，自动进入下一段）</summary>
        public List<DialogueChoice> Choices;

        /// <summary>
        /// 下一自然段落索引（无分支时自动跳转）。
        /// -1 表示对话结束。
        /// </summary>
        public int NextSegmentIndex = -1;

        /// <summary>是否有分支选项</summary>
        public bool HasChoices => Choices != null && Choices.Count > 0;
    }

    /// <summary>
    /// 完整对话数据（一组按顺序播放的对话段落）
    /// </summary>
    [Serializable]
    public class DialogueData
    {
        /// <summary>对话段落列表</summary>
        public List<DialogueSegment> Segments = new List<DialogueSegment>();

        /// <summary>段落总数</summary>
        public int Count => Segments != null ? Segments.Count : 0;

        /// <summary>
        /// 获取指定索引的段落。
        /// </summary>
        /// <param name="index">段落索引</param>
        /// <returns>段落数据，索引越界返回 null</returns>
        public DialogueSegment GetSegment(int index)
        {
            if (Segments == null || index < 0 || index >= Segments.Count)
                return null;
            return Segments[index];
        }
    }
}
