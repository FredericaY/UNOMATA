# Design: aim-ik-rig-constraint

## Context

B1b.3 落地了「输入 QF 化 + 下半身死区迟滞朝向（StrafeController）+ 上半身手写 spine 叠加（AnimatorAimBridge）」。下半身手感对了，上半身手写叠加调不准且破坏脊椎曲线。本 change 只换上半身实现，下半身保持。

## Goals / Non-Goals

**Goals**
- 用 Animation Rigging Multi-Aim Constraint 取代手写 spine 叠加，得到「枪口对准准星、脊椎自然弯曲、俯仰不翻转」。
- 保留 StrafeController 死区迟滞作为下半身 Yaw，组合成完整 TPS 手感。

**Non-Goals**
- 不动下半身逻辑、双相机、持枪挂点 IK、左手扶枪。

## 职责分层

```
                     ┌──────────────── 瞄准朝向 ────────────────┐
                     │                                          │
   下半身 Yaw（脚）                          上半身（脊椎+枪口）
        │                                          │
  StrafeController（不改）                  Animation Rigging Aim Rig
  ─ 进入瞄准 snap 相机Yaw                   ─ MultiAimConstraint 链 Spine→Chest→UpperChest
  ─ 移动锁相机Yaw                            ─ 瞄准 AimTarget（视线上的世界点）
  ─ 静止死区迟滞(±死区)                       ─ Rig.weight 由 IsAiming 渐变 0↔1
        │                                          │
        └──── transform.rotation ───┐    ┌── AnimatorAimBridge 驱动 weight
                                    ▼    ▼
                              PlayerArmature（Animator + RigBuilder）
                                          ▲
                              AimTargetDriver 每帧写 AimTarget 世界位置
                              = AimCamera.pos + AimCamera.forward × 距离
```

## 数据流

```
PlayerInputModel.IsAiming ──RegisterEvent/Register──▶ AnimatorAimBridge
                                                        └─ MoveTowards(Rig.weight, IsAiming?1:0)

[每帧] AimTargetDriver.LateUpdate
        AimTarget.position = aimCam.position + aimCam.forward * distance
[每帧] RigBuilder 求值
        MultiAimConstraint 按 weight 把脊椎链朝向 AimTarget（叠加在持枪动画之上）
```

- IK 与下半身解耦：StrafeController 决定脚朝哪，IK 在此基础上把上半身拧向准星，残差由脊椎链吸收 → 死区内只上半身扭、超死区脚追身后上半身回正。这正是用户要的「变动少只动上半身、变动多上下半身一起」。

## 关键决策

### D1：为何 Multi-Aim 而非手写 / 而非 Animator Aim Layer 单独搞定
- 手写：已验证调参地狱 + 轴向错位（见 proposal Why）。
- 纯动画层 BlendTree：只能离散方向，无法连续俯仰对准任意点。
- Multi-Aim Constraint：连续、对准世界点、叠加在动画上、权重链平滑、官方支持。✅

### D2：脊椎链分配
- 单骨（只约束 UpperChest）会硬拐、脖子僵。
- 沿 Spine→Chest→UpperChest 递增权重分担，整条脊椎平滑弯。Head 可选（加了更「盯着准星」，但可能与头部动画冲突，验收时决定是否纳入）。

### D3：AimTarget 距离
- 太近 → IK 朝向随距离剧烈变化、瞄准发飘。
- 取较远（默认 50m）使方向近似平行视线，准星与枪口指向一致。后续若做精确弹道，可改为「准星 Raycast 命中点」驱动（本 change 不做，留 hook）。

### D4：执行顺序
- `AimTargetDriver` 须在 RigBuilder 求值前写好 AimTarget（`[DefaultExecutionOrder(-5)]` 或在 LateUpdate 早段）。
- RigBuilder 默认在 Animator 之后求值（Animation Rigging 内部 PlayableGraph），IK 叠加在动画 pose 上，无需手动改 Animator 顺序。
- StrafeController `[DefaultExecutionOrder(10)]` 改 transform.rotation 不与 IK 冲突（IK 作用骨骼局部，transform 是整体 Yaw）。

## 风险

| 风险 | 缓解 |
|------|------|
| Aimed Axis / World Up 标定错（枪口偏） | 先在 Editor 静态拖 AimTarget 调到枪口对准，再接 Driver |
| 大俯仰脊椎翻转 | MultiAimConstraint 角度限制 + 限幅相机 pitch |
| IK 与持枪动画 pose 叠加冲突 | 持枪动画提供基础 pose，IK 只补朝向；权重链调试 |
| Head 约束与头部动画打架 | Head 设为可选，必要时移除或降权 |
| RigBuilder 漏挂 / 层未加 | 验收 Scenario 覆盖 weight=0/1 表现 |

## 迁移

- 删除 `AnimatorAimBridge` 的 spine 叠加代码块与 4 个 bias `SerializeField`。
- 保留 MoveX/MoveY 写入（B1b.3 契约）与 Rig weight 渐变逻辑。
- StrafeController 零改动。
