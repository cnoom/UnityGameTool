# Changelog

## [1.1.0] - 2026-05-19

### 新增
- 进度条模块
  - `IProgressBar` 接口定义
  - `ProgressBarEngine` 纯逻辑引擎（平滑过渡 + 延迟扣减条）
  - `ProgressBarConfig` 可序列化配置（缓动类型、延迟条参数）
  - `ProgressBarDriver` 协程版 MonoBehaviour 组件
  - `ProgressBarUniTask` UniTask 版 MonoBehaviour 组件（扩展程序集）
- 屏幕特效模块
  - `IScreenFlash` 接口定义
  - `ScreenFlashEngine` 纯逻辑引擎（多实例叠加、颜色加权混合）
  - `ScreenFlashConfig` 可序列化配置（Flash/Pulse 模式）
  - `ScreenFlashDriver` 协程版 MonoBehaviour 组件（自动创建覆盖层）
  - `ScreenFlashUniTask` UniTask 版 MonoBehaviour 组件（扩展程序集）
- 脉冲/呼吸效果模块
  - `IPulse` 接口定义
  - `PulseEngine` 纯逻辑引擎（Scale/Glow/Float 三种类型）
  - `PulseConfig` 可序列化配置（缓动类型、周期、幅度）
  - `PulseDriver` 协程版 MonoBehaviour 组件
  - `PulseUniTask` UniTask 版 MonoBehaviour 组件（扩展程序集）

## [1.0.0] - 2026-04-30

### 新增
- 打字机效果模块
  - `ITypewriter` 接口定义
  - `TypewriterEngine` 纯逻辑引擎
  - `TypewriterConfig` 可序列化配置（速度、标点延迟、换行延迟）
  - `TypewriterCoroutine` 协程版 MonoBehaviour 组件
  - `TypewriterUniTask` UniTask 版 MonoBehaviour 组件（扩展程序集）
