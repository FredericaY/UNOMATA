## ADDED Requirements

### Requirement: QF AudioModel 持有音频资产引用

`AudioModel`（`AbstractModel`）SHALL 持有脚步音 `AudioClip[]` 与落地音 `AudioClip` 字段，由 `AudioBridge.Awake()` 在运行时注入；预留 `MasterVolume BindableProperty<float>` 字段。Model 层 SHALL NOT 包含任何播放逻辑。

#### Scenario: AudioBridge 注入后 Model 字段有效

- **WHEN** Play Mode 启动，`AudioBridge.Awake()` 执行完毕
- **THEN** `AudioModel.FootstepClips.Length > 0` 且 `AudioModel.LandingClip != null`

#### Scenario: Model 不含播放逻辑

- **WHEN** 阅读 AudioModel.cs 源码
- **THEN** 文件内 SHALL NOT 包含 `AudioSource`、`Play`、`Stop`、`PlayClipAtPoint`、`PlayOneShot` 等播放相关调用

---

### Requirement: QF AudioSystem 提供 PlayFootstep / PlayLand / Play 接口

`AudioSystem`（`AbstractSystem`）SHALL 暴露 `PlayFootstep(Vector3 pos)` / `PlayLand(Vector3 pos)` / `Play(SoundId id, Vector3 pos)` 三个公开方法，内部通过 `this.SendEvent` 发送对应 QF Event，不直接持有 `AudioSource` 或任何 MonoBehaviour 引用。

#### Scenario: PlayFootstep 发送 FootstepPlayedEvent

- **WHEN** `AudioSystem.PlayFootstep(pos)` 被调用
- **THEN** QF 总线广播 `FootstepPlayedEvent { Position = pos }`

#### Scenario: PlayLand 发送 LandPlayedEvent

- **WHEN** `AudioSystem.PlayLand(pos)` 被调用
- **THEN** QF 总线广播 `LandPlayedEvent { Position = pos }`

#### Scenario: AudioSystem 不持有 MonoBehaviour 引用

- **WHEN** 阅读 AudioSystem.cs 源码
- **THEN** 类内 SHALL NOT 声明任何 `AudioSource`、`MonoBehaviour`、`GameObject` 类型字段或属性

---

### Requirement: SoundId 枚举预留所有音效类型

`SoundId` enum SHALL 包含 `Footstep`、`Land`（本期实装）以及 `GunShot`、`HitSurface`、`HitEnemy`、`UIClick`（本期仅预留，不实装）。

#### Scenario: 枚举值齐全

- **WHEN** 阅读 SoundId.cs 源码
- **THEN** 存在 `Footstep`、`Land`、`GunShot`、`HitSurface`、`HitEnemy`、`UIClick` 六个枚举值

---

### Requirement: QF Event 定义为 struct 放 Audio 目录

`FootstepPlayedEvent`、`LandPlayedEvent`、`SoundPlayedEvent` SHALL 定义为 `struct`，放置于 `Assets/_Project/Scripts/Gameplay/Audio/AudioEvents.cs`，位于 `Unomata.Gameplay` 命名空间下。

#### Scenario: Event 文件存在且为 struct

- **WHEN** 检查 `AudioEvents.cs` 源码
- **THEN** 三个类型均以 `public struct` 声明，无任何继承，无字段以外的逻辑

---

### Requirement: AudioBridge 作为 IController 持有 AudioSource 并出声

`AudioBridge`（MonoBehaviour + IController）SHALL：
- `Awake()` 把 `[SerializeField]` 引用写入 `AudioModel`
- `Start()` 订阅 `FootstepPlayedEvent` / `LandPlayedEvent`，UnRegisterWhenGameObjectDestroyed
- `_footstepSrc`：每次收到 `FootstepPlayedEvent` 先 `Stop()` 再随机选 clip 再 `Play()`（截断跑步重叠）
- `_landSrc`：收到 `LandPlayedEvent` 调 `PlayOneShot(LandingClip)`
- Inspector 上 2 个 `AudioSource` 字段分别对应 `_footstepSrc` / `_landSrc`

#### Scenario: 脚步音链路端到端

- **WHEN** Play Mode 下玩家走路，R_Walk fbx AnimEvent 触发 `OnFootstep`
- **THEN** `PlayerAnimEventReceiver.OnFootstep` → `SendCommand<PlayFootstepCommand>` → `AudioSystem.PlayFootstep` → `FootstepPlayedEvent` → `AudioBridge._footstepSrc.Stop()+Play()` → 有声

#### Scenario: 落地音链路端到端

- **WHEN** Play Mode 下玩家跳跃落地，R_Land_* fbx AnimEvent 触发 `OnLand`
- **THEN** `PlayerAnimEventReceiver.OnLand` → `SendCommand<PlayLandCommand>` → `AudioSystem.PlayLand` → `LandPlayedEvent` → `AudioBridge._landSrc.PlayOneShot()` → 有声

#### Scenario: 禁用 AudioBridge 后音效消失

- **WHEN** Play Mode 下将 `Audio` GameObject 的 AudioBridge 组件设为 disabled
- **THEN** 脚步音与落地音均无声（证明音频出口已完全迁移到 QF，TPC 不再出声）

#### Scenario: 跑步无重叠卡顿

- **WHEN** Play Mode 下玩家奔跑，R_Run clip 以 0.333s 间距连续触发 `OnFootstep`
- **THEN** 听感上不出现两段音频叠播的浑浊感（`Stop()` 截断保证每次重新开始）

#### Scenario: 事件订阅不泄漏

- **WHEN** `Audio` GameObject 被销毁（Play Mode 退出或 Scene 卸载）
- **THEN** `FootstepPlayedEvent` / `LandPlayedEvent` 订阅自动注销，不产生空引用回调

---

### Requirement: GameApp 注册 AudioModel 和 AudioSystem

`GameApp.Init()` SHALL 按 `RegisterModel<AudioModel>` 先于 `RegisterSystem<AudioSystem>` 的顺序完成注册，并置于 PlayerSystem 之后。

#### Scenario: AudioModel 先于 AudioSystem 注册

- **WHEN** 阅读 GameApp.cs 源码
- **THEN** `RegisterModel<AudioModel>` 所在行号早于 `RegisterSystem<AudioSystem>` 所在行号

#### Scenario: AudioSystem 可在运行时获取 AudioModel

- **WHEN** Play Mode 下执行 `this.GetModel<AudioModel>()` 于任意 IController 内
- **THEN** 返回非 null 的 AudioModel 实例

---

### Requirement: PlayFootstepCommand / PlayLandCommand 封装音频调用

`PlayFootstepCommand` 与 `PlayLandCommand` SHALL 继承 `AbstractCommand`，构造器接收 `Vector3 pos`，`OnExecute` 内仅调 `this.GetSystem<AudioSystem>()` 对应方法，无返回值，无副作用。

#### Scenario: PlayFootstepCommand 路由正确

- **WHEN** `this.SendCommand(new PlayFootstepCommand(pos))` 被调用
- **THEN** `AudioSystem.PlayFootstep(pos)` 被触发一次

#### Scenario: PlayLandCommand 路由正确

- **WHEN** `this.SendCommand(new PlayLandCommand(pos))` 被调用
- **THEN** `AudioSystem.PlayLand(pos)` 被触发一次
