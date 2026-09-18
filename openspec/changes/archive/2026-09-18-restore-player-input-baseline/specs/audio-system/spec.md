## ADDED Requirements

### Requirement: SampleScene 音频生命周期辅助组件只在运行时创建

SampleScene 的 Audio 对象 SHALL 保留 AudioBridge、两份 AudioSource 及原有音频/Animator 引用，但 SHALL NOT 持久化 QFramework 的运行时退订辅助组件或其内嵌 MonoScript。所需的运行时辅助组件 SHALL 由现有订阅链在启动时创建，退出或对象销毁时正常完成退订，不依赖一份失效的场景保存项。

本次清理 SHALL 限于已经定位的 Audio 残留，不修改框架源码、供应商内容或音频播放规则，不通过批量移除未知组件消除错误。

#### Scenario: 场景保存与重载后无残留
- **WHEN** 清理并保存 SampleScene，再重新加载场景
- **THEN** Audio SHALL 不包含保存的 UnRegisterOnDestroyTrigger 或缺失脚本槽，AudioBridge、两份 AudioSource 及其资产引用保持不变

#### Scenario: 多次启动与退出不报缺失脚本
- **WHEN** 清理后的 SampleScene 至少完成两次独立 Play Mode 启动和退出
- **THEN** SHALL 不再出现已定位到 Audio 的 Missing Script 日志，运行时辅助组件可正常创建，退出后不重新保存回场景

#### Scenario: 音频能力不回退
- **WHEN** 在运行态检查音频资源并触发既有脚步和落地播放路径
- **THEN** 音频模型及两个播放端 SHALL 保持有效，能播放原音频，无新增异常；该程序化验证不替代整体场景听感与输入手感验收
