# Tasks: aim-ik-rig-constraint

> Phase 2.1 · B1b.4
> 前置：B1b.3 (`unity-player-input-qf-bridge`) 已归档 (2026-05-31)
> 下游：B2a；B1c.1 / B1c.2 已于 2026-05-28 归档，不再作为本 change 的未来下游。
> 2026-09-17 重启：以下勾选是历史记录，本轮未重跑 Unity。用户确认搁置原因是瞄准动作和枪口朝向。当前场景 Aim_Spine3 offset 为 (-50, 20, -35)，需运行验证；本 change 保持活动，不提前归档。恢复入口见 Docs/PROJECT_REVIEW.md 与 Docs/AboutTheAnimation.md。

---

## 1. 前置确认

- [x] 1.1 确认 `com.unity.animation.rigging` 1.3.1 在 `Packages/manifest.json` 中
- [x] 1.2 确认 `PlayerArmature` 脊椎骨骼路径与名称（Spine=spine_01 / Chest=spine_02 / UpperChest=spine_03 / Head=head）
- [x] 1.3 确认瞄准方向源相机（Camera.main 带 CinemachineBrain；PlayerAimCamera 为虚拟相机）

---

## 2. Rig 资产与层级搭建（Unity Editor）

- [x] 2.1 `PlayerArmature` 添加 `RigBuilder` 组件
- [x] 2.2 新建子对象 `AimRig`（挂 `Rig` 组件），加入 `RigBuilder.layers`，初始 weight = 0
  - ⚠️ **必须挂在 Humanoid avatar 骨骼树根 `Rifle_Full_Body` 之下**，不能放 `PlayerArmature` 直接子级（详见末尾「调试记录」）
- [x] 2.3 新建子对象 `AimTarget`（空 Transform），初始置于角色前方 50m
- [x] 2.4 `AimRig` 下新建 `MultiAimConstraint` 链：Aim_Spine1/2/3 → spine_01/02/03
- [x] 2.5 每个 Constraint 的 `Constrained Object` 指向对应骨骼，`Source Objects` = `AimTarget`
- [x] 2.6 标定初值 `Aimed Axis=Y` / `Up Axis=X_NEG` / `WorldUpType=SceneUp`，沿链权重 0.4/0.7/1.0（待 Play 标定微调）

---

## 3. AimTargetDriver 脚本

- [x] 3.1 新建 `Assets/_Project/Scripts/Gameplay/Player/AimTargetDriver.cs`（`[DefaultExecutionOrder(-5)]`）
- [x] 3.2 序列化字段：`Transform _aimTarget` / `Camera _aimCamera`（留空自动 Camera.main）/ `float _distance = 50f`
- [x] 3.3 `LateUpdate()`：`_aimTarget.position = cam.position + cam.forward * _distance`
- [x] 3.4 编译通过，Console 零红错

---

## 4. AnimatorAimBridge 改造

- [x] 4.1 删除 spine 骨骼叠加代码块（`Quaternion.AngleAxis` 等）
- [x] 4.2 删除 `AimYawBias` / `AimPitchBias` / `AimWalkYawBias` / `AimWalkPitchBias` 等 bias `SerializeField`
- [x] 4.3 增加 `Rig _aimRig` 序列化引用 + `_weightLerpTime`（默认 0.15s）
- [x] 4.4 `Update`：`MoveTowards(_aimRig.weight, IsAiming?1:0, dt/_weightLerpTime)`
- [x] 4.5 保留 MoveX/MoveY 从 PlayerInputModel.Move 写入（B1b.3 契约不动）
- [x] 4.6 编译通过，Console 零红错

---

## 5. 场景接线

- [x] 5.1 `AimTargetDriver` 挂载并赋值 AimTarget / AimCamera(Camera.main)
- [x] 5.2 `AnimatorAimBridge._aimRig` 赋值为 AimRig 的 Rig，`_animator` 赋值
- [x] 5.3 SampleScene 保存（已用 EditorSceneManager.SaveOpenScenes 固化 AimRig 重挂 + offset 归零）

---

## 6. 固定姿态标定（Play Mode 验证；Edit Mode 保存参数）

> 注：Animation Rigging 仅运行时（PlayableGraph）评估，Editor 非 Play 看不到 IK 效果，标定在 Play Mode 进行（见 7）。

- [ ] 6.1 确认枪口对准（Aimed Axis 正确）；若上半身偏转方向错，调 aimAxis
- [ ] 6.2 上移/下移视角，确认上半身俯仰自然、脊椎不翻转；若翻转调 upAxis/worldUpType/limits
- [ ] 6.3 weight=1 时检查持枪动画 pose 与 IK 叠加无冲突

---

## 7. Play Mode 验收

- [ ] 7.1 进入瞄准：上半身 Rig 权重 ~0.15s 渐入，枪口对准准星方向
- [ ] 7.2 俯仰相机：上半身随准星俯仰，下半身竖直不前倾后仰
- [ ] 7.3 静止小幅转视角：脚不动（StrafeController 死区），上半身 IK 扭腰跟枪
- [ ] 7.4 静止大幅转视角：脚追身（StrafeController），上半身 IK 回到中性
- [ ] 7.5 瞄准移动：下半身锁相机不转身，BlendTree MoveX/Y 正确，IK 叠加正常
- [ ] 7.6 退出瞄准：Rig 权重渐出，上半身回纯动画
- [ ] 7.7 `git diff -- Assets/ThirdParty/` 为空
- [ ] 7.8 Console 零红色错误

---

## 8. 文档对齐

- [ ] 8.1 `Docs/AboutTheAnimation.md` 追加 IK 瞄准方案备忘（取代手写 spine 叠加的原因与标定要点）
- [ ] 8.2 归档时同步 delta spec → 主 specs，勾选 DEVELOPMENT_PLAN / TODO

---

## 调试记录（2026-05-31）：约束「完全不生效」根因与修复

### 现象
- 进入瞄准，上半身完全不动；改 `MultiAimConstraint.offset`（甚至 60°）无任何反应。
- 运行态 `rb.Build()` + 强制 `rig.weight = 1` 仍不动。
- 六轴 dot 测量：`spine_03` local Y（aimAxis）对 target 的 dot = 0.79，等于纯动画 pose 值——证明约束零作用，骨骼未被 IK 驱动。

### 根因
`AimRig` 被建在 `PlayerArmature`（Animator 所在）的**直接子级**，与 Humanoid avatar 骨骼树根 `Rifle_Full_Body` **平级**，因此不在 Animator 的 avatar hierarchy 内。RigBuilder build 时报：

```
Could not resolve 'PlayerArmature/AimRig/Aim_Spine1' because it is not a child Transform in the Animator hierarchy.
（Aim_Spine2 / Aim_Spine3 / AimRig 同样）
```

→ 三个 MultiAimConstraint 被 **静默忽略**，weight / offset / Build 全部无效。

> **关键教训**：Humanoid Animator + Animation Rigging，Rig 必须置于 **avatar 骨骼根（此处 `Rifle_Full_Body`）之下**，而非 Animator GameObject 的直接子级。否则约束不进 PlayableGraph，且只在 Console 留一行 warning，不报红错，极难察觉。

### 修复
`AimRig.SetParent(Rifle_Full_Body, worldPositionStays=true)` + 清零探针 offset + 保存场景。重新 Play 后：
- 约束成功解析，`dot(+Y, dirToTarget) = 0.924`，上半身随准星俯仰跟随。✓
- 剩余偏差仅为持枪 pose 的固定「左下」夹角（`rightHand.fwd` 与 dir 差 ~72°），由 `Aim_Spine3.Offset` 补偿。
- 注意：约束进 stream 后 offset 才会生效，但 offset **非 stream-synced**，需 **Edit 模式改 + 重 Play** 验证，运行态实时改无效。

### 剩余 TODO
- [ ] 标定 `Aim_Spine3.Offset` 补掉持枪 pose 的左下固定偏移（用户自行在 Inspector 调）。
- [ ] 死区分级：小幅转视角仅上半身 IK 扭腰、大幅转视角下半身追身（StrafeController 死区 + Rig 配合）。

## 2026-09-18 后续边界

用户接受“恢复到暂停前阶段”的项目基线，但明确瞄准动画仍有偏差，后续单独新建 change。本文保留原 22/37 的历史实现与未验收记录，不作为本次恢复的未完成任务，也不自动续作或当作已完成归档。后续新 change 应明确承接或替代关系。
