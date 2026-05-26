## 1. 探查当前场景层级

- [x] 1.1 打开 SampleScene，读取 PlayerArmature 的层级结构，确认 StarterAssets 原视觉子对象名称
      → 原视觉子对象：`PlayerArmature/Geometry`（含 SkinnedMeshRenderer `Armature_Mesh`）
- [x] 1.2 确认场景中 Main Camera / AudioListener 数量及对象名称
      → 多余：`Main Camera`（无 CinemachineBrain）；保留：`MainCamera`（有 CinemachineBrain）
- [x] 1.3 确认 `Assets/ThirdParty/CombatGirls/RifleGirl/Prefab/Rifle_Full_Body.prefab` 路径存在
- [x] 1.4 确认 RifleGirl Avatar Asset 路径
      → `Humanoid_FAvatar`（来自 `Assets/ThirdParty/CombatGirls/Humanoid_Bot/Models/Humanoid_F.fbx`，FBX 使用 CopyFromOther）

## 2. 禁用原 StarterAssets 视觉子对象

- [x] 2.1 找到 `PlayerArmature` 下的原 StarterAssets 视觉 Mesh 子对象（`Geometry`）
- [x] 2.2 对该子对象执行 `SetActive(false)`（不删除）

## 3. 嵌入 Rifle_Full_Body.prefab

- [x] 3.1 将 `Rifle_Full_Body.prefab` 实例化为 `PlayerArmature` 的子对象
- [x] 3.2 将其 local position 设为 (0, 0, 0)，local rotation 设为 (0, 0, 0)，local scale = (1, 1, 1)
- [x] 3.3 确认 Hierarchy 中 Rifle_Full_Body 已作为 PlayerArmature 子对象出现
      → `Rifle_Full_Body` 内部 Animator 已禁用，避免双 Animator 冲突

## 4. 切换 Animator Avatar

- [x] 4.1 找到 `PlayerArmature` 根对象上的 Animator 组件
      → 原 Avatar：`ArmatureAvatar`（StarterAssets）
- [x] 4.2 将 Animator 的 Avatar 字段切换为 `Humanoid_FAvatar`（SerializedObject 操作）
- [x] 4.3 `Rifle_Full_Body` 子对象内部 Animator 已禁用（Task 3 时处理）

## 5. 清理多余相机与 AudioListener

- [x] 5.1 检查场景中 Main Camera 与 AudioListener 数量
      → 发现 `Main Camera`（无 CinemachineBrain，有 AudioListener）为多余项
- [x] 5.2 删除多余的 `Main Camera` 对象（保留 `MainCamera` with CinemachineBrain）
- [x] 5.3 确认 Console 不再出现多 AudioListener 警告

## 6. 保存场景并验证

- [x] 6.1 保存 SampleScene（`Assets/_Project/Scenes/SampleScene.unity`）
- [x] 6.2 进入 Play Mode，截图确认 RifleGirl 渲染正常
      → ✅ 材质无紫色，角色正常显示
      → ✅ MagicaCloth2 布料物理激活（Scene View 绿色 Gizmo 为正常布料骨骼可视化）
      → ✅ Console 零红色错误
      → ✅ `[QF验证通过]` 两条验证日志正常输出（无回归）
- [x] 6.3 确认 WASD 移动动画通过 Humanoid Retargeting 正常播放（动画无 T-pose 固定错误）
- [x] 6.4 确认 MagicaCloth2 布料（Rifle_Dress、Rifle_Jacket）在 Play Mode 下物理模拟正常
- [x] 6.5 退出 Play Mode，最终保存场景
