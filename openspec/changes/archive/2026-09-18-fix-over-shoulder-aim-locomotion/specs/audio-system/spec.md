## MODIFIED Requirements

### Requirement: AudioBridge 用 Update 相位驱动出声

角色脚步/落地播放 SHALL 由实际动画相位和接地/落地状态驱动，不依赖供应商 AnimationEvent SendMessage。SHALL 支持非瞄准及瞄准方向运动、速度混合与状态过渡，不再只接受旧状态名、Walk/Run 白名单或单一 clip 权重超过一半的条件。

现有脚步/落地资产、两个播放端、音频 Model 与外部 Command/Event 契约 SHALL 保留。脚步采样 SHALL 按实际使用动作标定，使用独立相位去重，保持每个有效落脚事件一声；脚步播放可中断前段避免重叠，落地每次一次。旧 Walk/Run 相位和短音筛选仅作现有动作的基线，不假定自动适用于新动作/速率。

组件接入 SHALL 初始化当前相位，停用/销毁 SHALL 清理订阅、停止本组件播放并重置检测状态，重新启用不补播停用期间的事件。SHALL 不把运行时辅助组件写回场景。

#### Scenario: 脚步音链路端到端
- **WHEN** 非瞄准或瞄准时按任一方向行走并出现有效落脚
- **THEN** SHALL 每步一声，声音与实际脚部接触相位协调，不因动画名称变化失声

#### Scenario: 跑步音均匀无重叠
- **WHEN** 非瞄准向前后、侧向、斜向奔跑，或举枪纯向前奔跑并切换到其他方向行走
- **THEN** SHALL 按当前动作节奏播放，无重复叠播或与脚步明显错拍，不沿用旧固定间距假定

#### Scenario: 落地音链路端到端
- **WHEN** 原地跳或移动跳结束并发生实际落地
- **THEN** SHALL 每次落地只播放一声，不因多个过渡状态重复触发，也不因新状态名称漏播

#### Scenario: 禁用 AudioBridge 后音效消失
- **WHEN** 禁用音频桥接组件
- **THEN** 本组件的脚步/落地 SHALL 停止且不再触发，没有旧角色或动画事件播放端继续重复出声

#### Scenario: 静止站立无杂音
- **WHEN** 角色静止、被阻挡停住或处于无脚部接触的空中状态
- **THEN** SHALL 不产生无实际落脚的移动脚步音；原地转身仅在存在真实踏步动作时按接触相位出声

#### Scenario: 落地后回到静止无杂音
- **WHEN** 落地后回到静止，或重新启用音频组件
- **THEN** 相位 SHALL 从当前动作初始化，不补播跨越状态或停用期间的假脚步音

#### Scenario: 事件订阅不泄漏
- **WHEN** 反复禁用/启用、销毁音频对象或退出运行
- **THEN** 订阅 SHALL 成对清理，一次请求最多一次播放，无销毁后回调和场景持久化运行辅助组件

#### Scenario: 多动作混合中仍有脚步
- **WHEN** 方向或速度混合时没有任一 clip 的权重超过 0.5
- **THEN** 有效落脚 SHALL 仍得到单次播放，不因主导权重门槛静音，不因多个参与 clip 重复出声

### Requirement: QF AudioModel 持有音频资产引用

`AudioModel`（`AbstractModel`）SHALL 持有脚步音 `AudioClip[]`、落地音 `AudioClip` 与 `MasterVolume BindableProperty<float>`。音频表现启用绑定时 SHALL 经配置 Command 和 AudioSystem 注入资产，不直接写 Model；Model SHALL NOT 包含播放逻辑。

#### Scenario: AudioBridge 注入后 Model 字段有效
- **WHEN** Play Mode 启动且 AudioBridge 完成启用绑定
- **THEN** `AudioModel.FootstepClips.Length > 0` 且 `AudioModel.LandingClip != null`

#### Scenario: Model 不含播放逻辑
- **WHEN** 阅读 AudioModel.cs 源码
- **THEN** 文件内 SHALL NOT 包含 AudioSource 或任何播放、停止调用

### Requirement: GameApp 注册 AudioModel 和 AudioSystem

唯一业务入口 GameApp SHALL 按 Model → Utility → System 的依赖顺序注册，AudioModel 先于 AudioSystem，AudioSystem 保持在 PlayerSystem 之后。运行时 SHALL 能获取已初始化的音频 Model。

#### Scenario: AudioModel 先于 AudioSystem 注册
- **WHEN** 阅读 GameApp.cs 源码
- **THEN** AudioModel SHALL 在 Model 阶段注册，早于 AudioSystem 及 System 阶段

#### Scenario: AudioSystem 可在运行时获取 AudioModel
- **WHEN** Play Mode 下经 GameApp 获取 AudioModel
- **THEN** SHALL 返回非 null 的实例

### Requirement: PlayFootstepCommand / PlayLandCommand 封装音频调用

脚步与落地 Command SHALL 将位置参数转交 AudioSystem 对应播放请求，无返回值，不复制领域规则或直接操作 AudioSource。动画相位及实际落地检测 SHALL 经同一 Command → System → Event 链路触发音频，保留外部显式调用能力。

#### Scenario: PlayFootstepCommand 路由正确
- **WHEN** 发出 PlayFootstepCommand(pos)
- **THEN** AudioSystem.PlayFootstep(pos) SHALL 被触发一次

#### Scenario: PlayLandCommand 路由正确
- **WHEN** 发出 PlayLandCommand(pos)
- **THEN** AudioSystem.PlayLand(pos) SHALL 被触发一次

### Requirement: SampleScene 音频生命周期辅助组件只在运行时创建

SampleScene 的 Audio 对象 SHALL 保留 AudioBridge、两份 AudioSource 和有效音频引用，Animator/姿态引用 SHALL 指向当前项目角色。SHALL NOT 持久化运行时退订辅助组件、内嵌临时 MonoScript 或缺失脚本槽。订阅 SHALL 成对释放；不再需要的辅助组件无需创建，也不得依赖失效的场景保存项。

#### Scenario: 场景保存与重载后无残留
- **WHEN** 保存 SampleScene 并重新加载
- **THEN** Audio SHALL 不含保存的 UnRegisterOnDestroyTrigger 或缺失脚本槽，两个播放端、原音频与当前角色引用保持有效

#### Scenario: 多次启动与退出不报缺失脚本
- **WHEN** 正式场景至少完成两次独立 Play Mode 启动和退出
- **THEN** SHALL 无音频 Missing Script 日志，所需运行辅助对象只在运行时创建且正常释放，不写回场景

#### Scenario: 音频能力不回退
- **WHEN** 检查音频资源并触发脚步和落地播放路径
- **THEN** 音频 Model 与两个播放端 SHALL 有效、可播放原音频且无新增异常；整体听感另经用户运行验收
