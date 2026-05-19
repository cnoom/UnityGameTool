# UnityGameTool

Unity 游戏功能工具集，提供常用的游戏表现功能实现。每个模块采用**三层架构**（接口 → 纯逻辑引擎 → Unity 驱动），支持协程和 UniTask 两种异步模式。

## 目录

- [环境要求](#环境要求)
- [架构说明](#架构说明)
- [打字机效果（Typewriter）](#打字机效果typewriter)
- [屏幕震动（CameraShake）](#屏幕震动camershake)
- [计时器（Timer）](#计时器timer)
- [数字滚动（NumberRoller）](#数字滚动numberroller)
- [文字动画（TextAnimation）](#文字动画textanimation)
- [对话系统（Dialogue）](#对话系统dialogue)
- [进度条（ProgressBar）](#进度条progressbar)
- [屏幕特效（ScreenFlash）](#屏幕特效screenflash)
- [脉冲/呼吸效果（Pulse）](#脉冲呼吸效果pulse)
- [扩展程序集](#扩展程序集)

## 环境要求

- Unity 2021.3+
- TextMeshPro（Unity 内置）
- UniTask（可选，用于异步增强版本）

## 架构说明

每个功能模块遵循三层架构：

```
接口层（ITypewriter）       → 行为契约
逻辑层（TypewriterEngine）  → 纯 C# 逻辑，无 Unity 依赖，可单元测试
驱动层（Coroutine / UniTask）→ MonoBehaviour 驱动，操作 Unity 组件
```

每个模块提供两个驱动组件：
- **协程版**：零外部依赖，开箱即用（如 `TypewriterCoroutine`）
- **UniTask 版**：支持 `await` 等待完成和 `CancellationToken` 取消（如 `TypewriterUniTask`）

---

## 打字机效果（Typewriter）

逐字符显示文本的效果，常用于对话系统、剧情表现。

**组件：** `TypewriterCoroutine` / `TypewriterUniTask`  
**依赖：** 挂载到含有 `TMP_Text` 组件的 GameObject 上

### 特性

- 基于 TMP 的 `maxVisibleCharacters` 实现
- 支持富文本（TMP 自动处理，标签不计入打字进度）
- 可配置打字速度、标点延迟、换行延迟
- 支持跳过、停止

### Inspector 配置（TypewriterConfig）

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Characters Per Second | 30 | 每秒显示的字符数 |
| Enable Punctuation Delay | true | 标点符号是否额外延迟 |
| Punctuation Delay Multiplier | 5 | 标点延迟倍率 |
| New Line Delay Multiplier | 3 | 换行延迟倍率 |
| Skip Space Delay | true | 空格是否跳过延迟 |

### 使用示例

**协程版：**

```csharp
// 挂载 TypewriterCoroutine 到含有 TMP_Text 的 GameObject 上
var typewriter = GetComponent<TypewriterCoroutine>();

// 订阅事件
typewriter.OnCharacterTyped += (index, c) => { /* 播放打字音效 */ };
typewriter.OnComplete += () => { Debug.Log("打字完成"); };

// 开始播放
typewriter.Play("你好，世界！");

// 跳过动画，立即显示全部文本
typewriter.Skip();
```

**UniTask 版：**

```csharp
var typewriter = GetComponent<TypewriterUniTask>();

// await 等待完成
await typewriter.PlayAsync("你好，世界！");
Debug.Log("打字完成");

// 支持取消
var cts = new CancellationTokenSource();
try
{
    await typewriter.PlayAsync("长文本...", cts.Token);
}
catch (OperationCanceledException) { /* 已取消 */ }
```

---

## 屏幕震动（CameraShake）

多震源叠加的相机震动效果，支持位置偏移和旋转抖动。

**组件：** `CameraShakeDriver` / `CameraShakeUniTask`  
**依赖：** 挂载到 Camera 的 GameObject 上

### 特性

- 多震源同时叠加，自动混合
- 3 种衰减曲线：线性、指数、无衰减
- 可单独控制 X/Y 轴和 Z 轴旋转
- 震动完成后自动恢复原始位置

### Inspector 配置（CameraShakeConfig）

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Intensity | 1 | 震动强度 |
| Duration | 0.3 | 持续时间（秒） |
| Frequency | 15 | 频率（次/秒） |
| X Influence | 1 | X 轴强度倍率 |
| Y Influence | 1 | Y 轴强度倍率 |
| Z Rotation Influence | 0 | Z 轴旋转强度倍率 |
| Decay Type | Exponential | 衰减曲线类型 |

### 使用示例

**协程版：**

```csharp
// 挂载 CameraShakeDriver 到 Camera 上
var shake = GetComponent<CameraShakeDriver>();

// 使用默认配置震动
int id = shake.Shake();

// 使用自定义配置
var config = new CameraShakeConfig(intensity: 3f, duration: 0.5f);
shake.Shake(config);

// 简短震动（指定强度和持续时间）
shake.Shake(2f, 0.2f);

// 停止所有震动
shake.StopAll();

// 停止指定震动
shake.Stop(id);

// 震动完成回调
shake.OnShakeComplete += (shakeId) => { Debug.Log($"震动 {shakeId} 完成"); };

// 如果外部代码修改了 Camera 位置，调用此方法刷新缓存
shake.ResetOrigin();
```

**UniTask 版：**

```csharp
var shake = GetComponent<CameraShakeUniTask>();

// await 等待震动完成
int id = await shake.ShakeAsync(2f, 0.5f);
Debug.Log("震动完成");
```

---

## 计时器（Timer）

倒计时/正计时双模式的游戏计时器，支持暂停、恢复、警告阈值。

**组件：** `TimerDriver` / `TimerUniTask`  
**依赖：** 无特殊依赖

### 特性

- 倒计时模式：从指定时间倒数到 0
- 正计时模式：从 0 开始累加
- 支持暂停/恢复/跳过
- 警告阈值回调（倒计时剩余时间到达阈值时触发一次）
- 支持不受 Time.timeScale 影响的 unscaledTime 模式

### Inspector 配置（TimerConfig）

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Time Scale | 1 | 时间缩放因子 |
| Use Unscaled Time | false | 是否使用 unscaledTime |
| Enable Warning | true | 是否启用警告阈值 |
| Warning Threshold | 10 | 警告阈值（秒） |

### 使用示例

**协程版：**

```csharp
// 挂载 TimerDriver 到任意 GameObject 上
var timer = GetComponent<TimerDriver>();

// 订阅事件
timer.OnUpdate += (elapsed, remaining) => { /* 更新 UI 显示 */ };
timer.OnWarning += (remaining) => { Debug.Log($"剩余 {remaining} 秒！"); };
timer.OnComplete += () => { Debug.Log("倒计时结束"); };

// 开始 60 秒倒计时
timer.StartCountdown(60f);

// 暂停/恢复
timer.Pause();
timer.Resume();

// 跳到完成（倒计时置 0）
timer.Skip();

// 开始正计时（无限模式）
timer.StartStopwatch();

// 开始正计时（最大 120 秒）
timer.StartStopwatch(120f);

// 查询状态
float elapsed = timer.Elapsed;
float remaining = timer.Remaining;
float progress = timer.Progress; // 0~1 归一化
```

**UniTask 版：**

```csharp
var timer = GetComponent<TimerUniTask>();

// await 等待倒计时完成
await timer.StartCountdownAsync(60f);
Debug.Log("倒计时结束");
```

---

## 数字滚动（NumberRoller）

数值从起始到目标的平滑过渡动画，支持自定义缓动曲线和格式化。

**组件：** `NumberRollerCoroutine` / `NumberRollerUniTask`  
**依赖：** 挂载到含有 `TMP_Text` 组件的 GameObject 上

### 特性

- 6 种缓动曲线：Linear / EaseIn / EaseOut / EaseInOut / Bounce / Overshoot
- 灵活的格式化：整数、固定小数位、自定义格式（如货币、百分比）
- 千分位分隔符、正数前缀（如 `+`）
- 差值吸附阈值，避免长时间微小滚动

### Inspector 配置（NumberRollerConfig）

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Duration | 1 | 滚动持续时间（秒） |
| Ease Type | EaseOut | 缓动曲线类型 |
| Format Type | Integer | 格式化类型 |
| Decimal Places | 0 | 小数位数 |
| Use Thousands Separator | true | 千分位分隔符 |
| Positive Prefix | "" | 正数前缀 |
| Snap Threshold | 0.5 | 吸附阈值 |

### 使用示例

**协程版：**

```csharp
// 挂载 NumberRollerCoroutine 到含有 TMP_Text 的 GameObject 上
var roller = GetComponent<NumberRollerCoroutine>();

// 订阅事件
roller.OnUpdate += (value, text) => { Debug.Log($"当前值: {text}"); };
roller.OnComplete += () => { Debug.Log("滚动完成"); };

// 从 0 滚动到 1000
roller.Play(0, 1000);

// 从当前值继续滚动到新目标
roller.Play(5000);

// 跳过动画
roller.Skip();
```

**UniTask 版：**

```csharp
var roller = GetComponent<NumberRollerUniTask>();

// await 等待完成
await roller.PlayAsync(0, 1000);
Debug.Log("滚动完成");
```

---

## 文字动画（TextAnimation）

基于 TMP 顶点操作的逐字动画效果，支持波浪、抖动、弹跳、渐显四种类型。

**组件：** `TextAnimationDriver` / `TextAnimationUniTask`  
**依赖：** 挂载到含有 `TMP_Text` 组件的 GameObject 上

### 特性

- 4 种动画类型：Wave（波浪浮动）、Shake（随机抖动）、Bounce（缩放弹跳）、Fade（逐字渐显）
- 支持循环模式（Duration 设为 -1）
- 字符间延迟产生波浪扩散效果
- 通过修改 TMP 网格顶点实现，不生成额外 GameObject

### Inspector 配置（TextAnimationConfig）

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Type | Wave | 动画类型 |
| Duration | -1 | 持续时间（秒），-1 为无限循环 |
| Speed | 1 | 播放速度倍率 |
| Amplitude | 10 | 动画幅度（像素） |
| Frequency | 2 | 动画频率 |
| Char Delay | 0.05 | 字符间延迟（秒） |
| Fade Duration | 0.3 | Fade 模式渐显时长（秒） |

### 使用示例

**协程版：**

```csharp
// 挂载 TextAnimationDriver 到含有 TMP_Text 的 GameObject 上
var anim = GetComponent<TextAnimationDriver>();

// 设置文本后开始播放
textComponent.text = "波浪文字效果";
textComponent.ForceMeshUpdate();
anim.Play(textComponent.textInfo.characterCount);

// 动画完成回调（仅非循环模式）
anim.OnComplete += () => { Debug.Log("动画完成"); };

// 停止动画并恢复原始状态
anim.Stop();

// 跳过动画并恢复原始状态
anim.Skip();

// 如果文本内容变化（如与打字机配合），更新可见字符数
anim.UpdateVisibleCount(newVisibleCount);
```

**UniTask 版：**

```csharp
var anim = GetComponent<TextAnimationUniTask>();

// await 等待动画完成
await anim.PlayAsync(visibleCount);
Debug.Log("动画完成");
```

### 与打字机配合使用

```csharp
// 打字机逐字显示时，同步更新文字动画的可见字符数
var typewriter = GetComponent<TypewriterCoroutine>();
var textAnim = GetComponent<TextAnimationDriver>();

typewriter.OnCharacterTyped += (index, c) =>
{
    textAnim.UpdateVisibleCount(index + 1);
};

typewriter.OnComplete += () =>
{
    // 打字完成，开始完整的文字动画
    textAnim.Play(textComponent.textInfo.characterCount);
};

textAnim.Play(0); // 初始无可见字符
typewriter.Play("逐字显示并带有波浪动画的文字");
```

---

## 对话系统（Dialogue）

多段对话播放、分支选择、打字机集成的完整对话系统。

**组件：** `DialogueSequencerCoroutine` / `DialogueSequencerUniTask`  
**依赖：** 挂载到含有 `TypewriterCoroutine`（或 UniTask 版）和 `TMP_Text` 的 GameObject 上

### 特性

- 多段对话按顺序播放，支持分支选项
- 自动集成打字机效果
- 状态机驱动：Idle → Typing → WaitingForInput/WaitingForChoice → Completed
- 支持从指定段落开始播放
- 分支跳转通过 `NextSegmentIndex` 控制（-1 表示对话结束）

### 数据结构

```csharp
// 创建对话数据
var dialogue = new DialogueData
{
    Segments = new List<DialogueSegment>
    {
        new DialogueSegment
        {
            SpeakerName = "角色A",
            Text = "你好，欢迎来到这个世界。"
            // NextSegmentIndex 默认 -1，自动进入下一段
        },
        new DialogueSegment
        {
            SpeakerName = "角色B",
            Text = "请选择你的道路。",
            Choices = new List<DialogueChoice>
            {
                new DialogueChoice("走向光明", nextSegmentIndex: 2),
                new DialogueChoice("走向黑暗", nextSegmentIndex: 3)
            }
        },
        new DialogueSegment
        {
            SpeakerName = "旁白",
            Text = "你选择了光明的道路。",
            NextSegmentIndex = -1 // 对话结束
        },
        new DialogueSegment
        {
            SpeakerName = "旁白",
            Text = "你选择了黑暗的道路。",
            NextSegmentIndex = -1
        }
    }
};
```

### 使用示例

**协程版：**

```csharp
// 挂载 DialogueSequencerCoroutine 到含有 TypewriterCoroutine 的 GameObject 上
var sequencer = GetComponent<DialogueSequencerCoroutine>();

// 订阅事件
sequencer.OnSegmentStart += (index, segment) =>
{
    Debug.Log($"[{segment.SpeakerName}]: 段落 {index} 开始");
};

sequencer.OnSegmentComplete += (index, segment) =>
{
    Debug.Log($"段落 {index} 打字完成");
};

sequencer.OnChoicesPresented += (choices) =>
{
    // 在 UI 上显示分支选项按钮
    for (int i = 0; i < choices.Count; i++)
    {
        Debug.Log($"选项 {i}: {choices[i].Text}");
    }
};

sequencer.OnDialogueComplete += () =>
{
    Debug.Log("全部对话结束");
};

// 开始播放对话
sequencer.Play(dialogue);

// 玩家点击"继续"时调用（推进到下一段）
sequencer.Next();

// 玩家选择分支时调用
sequencer.Choose(0); // 选择第一个选项

// 跳过当前段落的打字动画
sequencer.SkipTyping();

// 停止对话
sequencer.Stop();
```

**UniTask 版：**

```csharp
var sequencer = GetComponent<DialogueSequencerUniTask>();

// await 等待对话完成
await sequencer.PlayAsync(dialogue);
Debug.Log("对话结束");

// 或 await 单个操作
await sequencer.NextAsync();
await sequencer.ChooseAsync(0);
```

---

## 进度条（ProgressBar）

支持平滑过渡和延迟扣减条的进度条组件，常用于血条、经验条等。

**组件：** `ProgressBarDriver` / `ProgressBarUniTask`
**依赖：** 无特殊依赖

### 特性

- 主进度条平滑过渡，支持 3 种缓动曲线
- 延迟扣减条效果（血条经典效果：扣血时延迟条先停留再追赶）
- 增加时延迟条直接跟随，减少时延迟条延迟追赶
- 进度到达 100% 时触发完成事件

### Inspector 配置（ProgressBarConfig）

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Transition Duration | 0.3 | 主进度条过渡持续时间（秒） |
| Ease Type | EaseOut | 主进度条缓动类型 |
| Enable Delayed Bar | true | 是否启用延迟扣减条 |
| Delayed Wait Time | 0.5 | 延迟条等待时间（秒） |
| Delayed Catch Up Duration | 0.8 | 延迟条追赶持续时间（秒） |
| Delayed Ease Type | EaseInOut | 延迟条缓动类型 |

### 使用示例

**协程版：**

```csharp
// 挂载 ProgressBarDriver 到任意 GameObject 上
var bar = GetComponent<ProgressBarDriver>();

// 订阅事件
bar.OnValueChanged += (value) => { /* 更新 UI：主进度条 fillAmount = value */ };
bar.OnComplete += () => { Debug.Log("进度条满了"); };

// 立即设置进度（无动画）
bar.Set(0.8f);

// 平滑过渡到目标值
bar.TransitionTo(0.5f);

// 增加进度（带动画）
bar.Add(0.1f);

// 减少进度（带延迟条效果）
bar.Subtract(0.3f);

// 重置到 0
bar.Reset();
```

**UniTask 版：**

```csharp
var bar = GetComponent<ProgressBarUniTask>();

// await 等待过渡完成
await bar.TransitionToAsync(0.8f);
Debug.Log("过渡完成");
```

---

## 屏幕特效（ScreenFlash）

全屏闪烁/颜色渐变特效，支持多实例叠加，常用于受击红闪、治愈绿闪等。

**组件：** `ScreenFlashDriver` / `ScreenFlashUniTask`
**依赖：** 无特殊依赖（自动创建全屏覆盖 Image）

### 特性

- 自动创建全屏覆盖 Image 层
- 多特效实例叠加，颜色加权混合
- Flash 模式：单次淡入→停留→淡出
- Pulse 模式：周期性重复闪烁
- 可配置脉冲次数和间隔

### Inspector 配置（ScreenFlashConfig）

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Color | Red | 闪烁颜色 |
| Max Alpha | 0.5 | 最大不透明度 |
| Mode | Flash | 闪烁模式（Flash/Pulse） |
| Fade In Duration | 0.05 | 淡入时间（秒） |
| Hold Duration | 0.1 | 停留时间（秒） |
| Fade Out Duration | 0.3 | 淡出时间（秒） |
| Pulse Count | 0 | 脉冲次数（0=无限） |
| Pulse Interval | 0.5 | 脉冲间隔（秒） |

### 使用示例

**协程版：**

```csharp
// 挂载 ScreenFlashDriver 到 Canvas 根节点上
var flash = GetComponent<ScreenFlashDriver>();

// 使用默认配置闪烁
int id = flash.Flash();

// 指定颜色和持续时间
flash.Flash(Color.red, 0.5f);

// 使用自定义配置
var config = new ScreenFlashConfig(Color.blue, 0.3f);
flash.Flash(config);

// 停止所有特效
flash.StopAll();

// 完成回调
flash.OnComplete += (flashId) => { Debug.Log($"特效 {flashId} 完成"); };
```

**UniTask 版：**

```csharp
var flash = GetComponent<ScreenFlashUniTask>();

// await 等待闪烁完成
await flash.FlashAsync(Color.red, 0.3f);
Debug.Log("闪烁完成");
```

---

## 脉冲/呼吸效果（Pulse）

周期性动画效果，支持缩放脉冲、发光脉冲、位移浮动三种类型，常用于 UI 引导发光、呼吸效果等。

**组件：** `PulseDriver` / `PulseUniTask`
**依赖：** Glow 类型需要 `CanvasGroup` 组件

### 特性

- 3 种动画类型：Scale（缩放脉冲）、Glow（发光/Alpha 脉冲）、Float（Y 轴浮动）
- 3 种缓动类型：Sine（平滑）、Linear（线性）、Exponential（指数）
- 支持循环模式和非循环模式
- 支持暂停/恢复
- Glow 类型自动通过 CanvasGroup 应用 Alpha

### Inspector 配置（PulseConfig）

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Type | Scale | 动画类型 |
| Ease Type | Sine | 缓动类型 |
| Period | 1.5 | 周期时长（秒） |
| Speed | 1 | 播放速度倍率 |
| Is Looping | true | 是否无限循环 |
| Duration | 3 | 非循环模式总时长（秒） |
| Min Scale | 0.9 | 缩放最小值 |
| Max Scale | 1.1 | 缩放最大值 |
| Min Alpha | 0.3 | 发光最小 Alpha |
| Max Alpha | 1 | 发光最大 Alpha |
| Float Amplitude | 5 | 浮动幅度（像素） |

### 使用示例

**协程版：**

```csharp
// 挂载 PulseDriver 到需要脉冲动画的 UI 元素上
var pulse = GetComponent<PulseDriver>();

// 使用默认配置播放
pulse.Play();

// 使用自定义配置
var config = new PulseConfig(
    type: PulseType.Glow,
    easeType: PulseEaseType.Sine,
    period: 2f,
    minAlpha: 0.3f,
    maxAlpha: 1f);
pulse.Play(config);

// 暂停/恢复
pulse.Pause();
pulse.Resume();

// 停止（恢复原始状态）
pulse.Stop();

// 非循环模式完成回调
pulse.OnComplete += () => { Debug.Log("脉冲动画完成"); };
```

**UniTask 版：**

```csharp
var pulse = GetComponent<PulseUniTask>();

// await 等待非循环动画完成
var config = new PulseConfig(PulseType.Scale, isLooping: false, duration: 2f);
await pulse.PlayAsync(config);
Debug.Log("脉冲完成");
```

---

## 扩展程序集

| 程序集 | 说明 | 依赖 |
|--------|------|------|
| `CNoom.UnityGameTool.Runtime` | 主程序集，协程版功能 | TextMeshPro |
| `CNoom.UnityGameTool.UniTask` | UniTask 扩展 | 主程序集 + UniTask |

安装 UniTask 后，扩展程序集自动编译，无需手动配置。

## 模块速览

| 模块 | 组件 | 接口 | 说明 |
|------|------|------|------|
| Typewriter | `TypewriterCoroutine` / `TypewriterUniTask` | `ITypewriter` | 打字机效果 |
| CameraShake | `CameraShakeDriver` / `CameraShakeUniTask` | `ICameraShake` | 屏幕震动 |
| Timer | `TimerDriver` / `TimerUniTask` | `ITimer` | 计时器 |
| NumberRoller | `NumberRollerCoroutine` / `NumberRollerUniTask` | `INumberRoller` | 数字滚动 |
| TextAnimation | `TextAnimationDriver` / `TextAnimationUniTask` | `ITextAnimation` | 文字动画 |
| Dialogue | `DialogueSequencerCoroutine` / `DialogueSequencerUniTask` | `IDialogueSequencer` | 对话系统 |
| ProgressBar | `ProgressBarDriver` / `ProgressBarUniTask` | `IProgressBar` | 进度条 |
| ScreenFlash | `ScreenFlashDriver` / `ScreenFlashUniTask` | `IScreenFlash` | 屏幕特效 |
| Pulse | `PulseDriver` / `PulseUniTask` | `IPulse` | 脉冲/呼吸效果 |
