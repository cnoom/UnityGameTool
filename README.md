# UnityGameTool

Unity 游戏功能工具集，提供常用的游戏功能实现。

## 功能模块

### 打字机效果（Typewriter）

逐字符显示文本的效果，常用于对话系统、剧情表现。

**特性：**
- 基于 TMP 的 `maxVisibleCharacters` 实现，性能优良
- 支持富文本（TMP 自动处理，标签不计入打字进度）
- 可配置打字速度、标点延迟、换行延迟
- 支持跳过、停止
- 事件回调：每字符显示回调、完成回调

**协程版（默认）：** 零外部依赖，开箱即用。

```csharp
// 挂载 TypewriterCoroutine 到含有 TMP_Text 组件的 GameObject 上
var typewriter = GetComponent<TypewriterCoroutine>();
typewriter.OnCharacterTyped += (index, c) => { /* 播放打字音效 */ };
typewriter.OnComplete += () => { /* 打字完成 */ };
typewriter.Play("你好，世界！");
```

**UniTask 版（需安装 UniTask）：** 支持异步等待和取消。

```csharp
var typewriter = GetComponent<TypewriterUniTask>();
await typewriter.PlayAsync("你好，世界！");
Debug.Log("打字完成");
```

## 扩展程序集

| 程序集 | 说明 | 依赖 |
|---|---|---|
| `CNoom.UnityGameTool.Runtime` | 主程序集，协程版功能 | TextMeshPro |
| `CNoom.UnityGameTool.UniTask` | UniTask 扩展 | 主程序集 + UniTask |

安装 UniTask 后，扩展程序集自动编译，无需手动配置。

## 架构说明

每个功能模块遵循三层架构：

```
接口层（ITypewriter）     → 行为契约
逻辑层（TypewriterEngine）→ 纯 C# 逻辑，无 Unity 依赖，可单元测试
驱动层（Coroutine/UniTask）→ 异步驱动，操作 TMP 组件
```

## 环境要求

- Unity 2021.3+
- TextMeshPro（Unity 内置）
- UniTask（可选，用于异步增强版本）
