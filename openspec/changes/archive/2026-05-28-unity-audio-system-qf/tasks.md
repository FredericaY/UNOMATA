## 1. 前置确认（无操作风险）

- [x] 1.1 确认 Unity Editor 处于 Edit Mode（非 Play Mode、非编译中）
- [x] 1.2 确认 B1c.1 遗留态：用 `find_gameobjects search_term=PlayerArmature` 定位 PlayerArmature instanceID，验证 TPC 上 `FootstepAudioClips.Length > 0` 且 `LandingAudioClip != null`（迁移前 baseline）
- [x] 1.3 确认 `Assets/_Project/Scripts/Gameplay/Audio/` 目录不存在（干净起步）

## 2. 新建 Audio 层文件

- [x] 2.1 创建 `Assets/_Project/Scripts/Gameplay/Audio/SoundId.cs`：`enum SoundId { Footstep, Land, GunShot, HitSurface, HitEnemy, UIClick }`，命名空间 `Unomata.Gameplay`
- [x] 2.2 创建 `Assets/_Project/Scripts/Gameplay/Audio/AudioEvents.cs`：三个 struct（`FootstepPlayedEvent { Vector3 Position }`、`LandPlayedEvent { Vector3 Position }`、`SoundPlayedEvent { SoundId Id; Vector3 Position }`），命名空间 `Unomata.Gameplay`
- [x] 2.3 创建 `Assets/_Project/Scripts/Gameplay/Audio/AudioModel.cs`：继承 `AbstractModel`，字段 `AudioClip[] FootstepClips`、`AudioClip LandingClip`、`BindableProperty<float> MasterVolume = new(1f)`，`OnInit()` 空实现，命名空间 `Unomata.Gameplay`
- [x] 2.4 创建 `Assets/_Project/Scripts/Gameplay/Audio/AudioSystem.cs`：继承 `AbstractSystem`，持有 `_audioModel`（`OnInit` 取），实现 `PlayFootstep(Vector3 pos)`（SendEvent FootstepPlayedEvent）、`PlayLand(Vector3 pos)`（SendEvent LandPlayedEvent）、`Play(SoundId id, Vector3 pos)`（SendEvent SoundPlayedEvent，占位），命名空间 `Unomata.Gameplay`
- [x] 2.5 创建 `Assets/_Project/Scripts/Gameplay/Audio/AudioBridge.cs`：MonoBehaviour + IController；`[SerializeField] AudioClip[] _footstepClips`、`[SerializeField] AudioClip _landingClip`；`[SerializeField] AudioSource _footstepSrc`、`[SerializeField] AudioSource _landSrc`；`Awake` 注入 AudioModel；`Start` 注册两个 Event（UnRegisterWhenGameObjectDestroyed）；OnFootstepPlayed：`_footstepSrc.Stop()`+随机 clip+`Play()`；OnLandPlayed：`_landSrc.PlayOneShot(clip)`，命名空间 `Unomata.Gameplay`

## 3. 新建 Command 文件

- [x] 3.1 创建 `Assets/_Project/Scripts/Gameplay/Commands/PlayFootstepCommand.cs`：继承 `AbstractCommand`，构造器接收 `Vector3 pos`，`OnExecute` 调 `this.GetSystem<AudioSystem>().PlayFootstep(_pos)`，命名空间 `Unomata.Gameplay`
- [x] 3.2 创建 `Assets/_Project/Scripts/Gameplay/Commands/PlayLandCommand.cs`：同上，调 `PlayLand(_pos)`，命名空间 `Unomata.Gameplay`

## 4. 修改 GameApp.cs

- [x] 4.1 在 `RegisterModel` 区域末尾追加 `this.RegisterModel<AudioModel>(new AudioModel())`
- [x] 4.2 在 `RegisterSystem` 区域末尾追加 `this.RegisterSystem<AudioSystem>(new AudioSystem())`
- [x] 4.3 更新文件头 summary 注释（Model/System 列表加 AudioModel / AudioSystem）

## 5. 修改 PlayerAnimEventReceiver.cs

- [x] 5.1 添加 `IController` 实现：类声明改为 `public class PlayerAnimEventReceiver : MonoBehaviour, IController`，添加 `IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;`
- [x] 5.2 添加 `private void OnFootstep(AnimationEvent animationEvent)` 方法：含 `weight > 0.5f` guard，调 `this.SendCommand(new PlayFootstepCommand(transform.position))`
- [x] 5.3 添加 `private void OnLand(AnimationEvent animationEvent)` 方法：含 `weight > 0.5f` guard，调 `this.SendCommand(new PlayLandCommand(transform.position))`
- [x] 5.4 保留原 `SwitchSocket(string slot)` 占位方法不变
- [x] 5.5 更新文件头 summary 注释，说明 IController 升级与音频转发职责

## 6. Unity 编译校验

- [x] 6.1 通过 `refresh_unity` 触发编译，等待完成
- [x] 6.2 通过 `read_console` 确认 Console 零红色错误，零与本 change 相关的黄色警告

## 7. 场景：建立 Audio GameObject + AudioBridge

- [x] 7.1 通过 `manage_gameobject action=create` 在场景根建立空 GameObject，命名 `Audio`
- [x] 7.2 通过 `manage_components action=add` 给 `Audio` GO 挂载 `AudioBridge` 组件
- [x] 7.3 通过 `manage_components action=add` 给 `Audio` GO 挂载两个 `AudioSource` 组件（`_footstepSrc` 和 `_landSrc`），或在 AudioBridge Inspector 上直接引用同一 GO 上已有的 AudioSource
- [x] 7.4 通过 `manage_components action=set_property` 把 SA 的 10 段脚步 AudioClip 赋给 `AudioBridge._footstepClips`（原来挂在 TPC.FootstepAudioClips 上的同一批 wav）
- [x] 7.5 通过 `manage_components action=set_property` 把落地 AudioClip（`Player_Land.wav`）赋给 `AudioBridge._landingClip`
- [x] 7.6 通过 `manage_components action=set_property` 把两个 AudioSource 分别绑定到 `AudioBridge._footstepSrc` / `AudioBridge._landSrc`

## 8. 场景：清空 TPC 音频字段

- [x] 8.1 通过 `manage_components action=set_property` 把 PlayerArmature TPC 上的 `FootstepAudioClips` 设为空数组（Length=0）
- [x] 8.2 通过 `manage_components action=set_property` 把 PlayerArmature TPC 上的 `LandingAudioClip` 设为 None（null）

## 9. Play Mode 验证

- [x] 9.1 进入 Play Mode
- [x] 9.2 验证 Console 零红色错误（仅 QF 框架自身 UnRegisterOnDestroyTrigger fileID warning，不影响功能）
- [x] 9.3 走路 WASD：脚步音正常响起，每步一声，相位驱动对齐脚部视觉
- [x] 9.4 跑步 Shift+WASD：脚步音节奏均匀，无叠播浑浊感（筛后 5 段 ≤0.313s + Update 相位驱动）
- [x] 9.5 站立跳跃落地：落地音响一次（JumpLand 状态切入首帧检测）
- [x] 9.6 走路跳跃落地：落地音响一次 ✓
- [x] 9.7 奔跑跳跃落地：落地音响一次 ✓
- [x] 9.8 临时 Disable AudioBridge 组件 → 脚步音与落地音消失（出口已完全迁入 QF）
- [x] 9.9 重新 Enable AudioBridge → 音效恢复 ✓
- [x] 9.10 SampleScene 已保存

> **实施备注**：原方案 A（AnimationEvent SendMessage）因多 Animator GO 路由问题、BlendTree 双触发、time=0 event 被过渡吞掉等问题调试困难，最终采用**方案 B（Update 相位驱动）**：
> - AudioBridge.Update() 每帧读 Animator.GetCurrentAnimatorStateInfo + GetCurrentAnimatorClipInfo
> - 相位越过检测触发脚步音（Walk/Run 相位值来自 B1c.1 程序化标定）
> - JumpLand 状态切入首帧触发落地音
> - PlayerAnimEventReceiver 降回普通 MonoBehaviour（仅保留 SwitchSocket 占位）
> - R_Walk/R_Run/R_Land_* 的 OnFootstep/OnLand AnimationEvent 全部清除（fbx .meta 干净）

## 10. 收尾

- [x] 10.1 `git status -- Assets/ThirdParty/` 显示仅 5 个 .fbx.meta modified（B1c.1+B1c.2 联合结果，均为 meta 文件，fbx 本体未改）
- [x] 10.2 B1c.2 验收条件全部覆盖：脚步/落地音正常 / Disable AudioBridge 无声 / TPC 字段已迁空 / Console 零红错
- [x] 10.3 SampleScene 已保存，进入归档流程
