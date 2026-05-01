using System;
using System.Collections.Generic;

namespace CNoom.UnityGameTool.Dialogue
{
    /// <summary>
    /// 对话序列状态
    /// </summary>
    public enum DialogueState
    {
        /// <summary>未开始/已停止</summary>
        Idle,
        /// <summary>打字机正在显示当前段落</summary>
        Typing,
        /// <summary>当前段落打字完成，等待用户操作（点击继续或选择分支）</summary>
        WaitingForInput,
        /// <summary>正在展示分支选项，等待玩家选择</summary>
        WaitingForChoice,
        /// <summary>全部对话结束</summary>
        Completed
    }

    /// <summary>
    /// 新段落开始事件
    /// </summary>
    /// <param name="segmentIndex">段落索引</param>
    /// <param name="segment">段落数据</param>
    public delegate void SegmentStartHandler(int segmentIndex, DialogueSegment segment);

    /// <summary>
    /// 段落打字完成事件（文本已全部显示）
    /// </summary>
    /// <param name="segmentIndex">段落索引</param>
    /// <param name="segment">段落数据</param>
    public delegate void SegmentCompleteHandler(int segmentIndex, DialogueSegment segment);

    /// <summary>
    /// 全部对话完成事件
    /// </summary>
    public delegate void DialogueCompleteHandler();

    /// <summary>
    /// 分支选项展示事件
    /// </summary>
    /// <param name="choices">选项列表</param>
    public delegate void ChoicesPresentedHandler(IReadOnlyList<DialogueChoice> choices);

    /// <summary>
    /// 对话序列管理器接口，定义多段对话播放、分支选择、流程控制的核心行为契约。
    /// </summary>
    public interface IDialogueSequencer
    {
        /// <summary>当前对话状态</summary>
        DialogueState State { get; }

        /// <summary>当前段落索引</summary>
        int CurrentSegmentIndex { get; }

        /// <summary>当前段落数据</summary>
        DialogueSegment CurrentSegment { get; }

        /// <summary>新段落开始时触发（可用于更新角色名、头像等）</summary>
        event SegmentStartHandler OnSegmentStart;

        /// <summary>段落文本全部显示完成时触发</summary>
        event SegmentCompleteHandler OnSegmentComplete;

        /// <summary>全部对话结束时触发</summary>
        event DialogueCompleteHandler OnDialogueComplete;

        /// <summary>分支选项展示时触发</summary>
        event ChoicesPresentedHandler OnChoicesPresented;

        /// <summary>
        /// 开始播放对话。
        /// </summary>
        /// <param name="dialogueData">对话数据</param>
        void Play(DialogueData dialogueData);

        /// <summary>
        /// 开始播放对话，从指定段落索引。
        /// </summary>
        /// <param name="dialogueData">对话数据</param>
        /// <param name="startSegmentIndex">起始段落索引</param>
        void Play(DialogueData dialogueData, int startSegmentIndex);

        /// <summary>
        /// 推进到下一段对话（打字完成后由用户点击触发）。
        /// 如果当前段落有分支选项，此方法无效，应使用 <see cref="Choose(int)"/>。
        /// </summary>
        void Next();

        /// <summary>
        /// 选择分支选项。
        /// </summary>
        /// <param name="choiceIndex">选项索引（对应 Choices 列表的下标）</param>
        void Choose(int choiceIndex);

        /// <summary>
        /// 跳过当前段落的打字动画（立即显示全文），不推进到下一段。
        /// </summary>
        void SkipTyping();

        /// <summary>
        /// 停止对话并重置状态。
        /// </summary>
        void Stop();
    }
}
