## 技术决策

### 模型嵌入方式

采用**子对象嵌入**而非直接替换 Mesh：
- CombatGirls 分件结构（15 个部件），布料部件（Rifle_Dress / Rifle_Jacket）上挂有 MagicaCloth2 组件
- 直接替换 SkinnedMeshRenderer 的 Mesh 会丢失 MagicaCloth2 的组件配置和骨骼绑定
- 将 `Rifle_Full_Body.prefab` 作为子对象整体嵌入，MagicaCloth2 组件完整保留

### 原视觉模型处理

`SetActive(false)` 禁用，不删除：
- 保留回退能力（出问题时直接重新启用）
- 不破坏 StarterAssets 原有 prefab 引用关系

### Avatar 切换位置

在 `PlayerArmature`（根对象）的 `Animator` 组件上切换 Avatar，而非在 RifleGirl 子对象的 Animator（若有）上操作：
- StarterAssets ThirdPersonController 脚本持有的 `Animator` 引用指向 `PlayerArmature` 根
- 根 Animator 的 Avatar 决定 Humanoid Retargeting 的骨骼映射
- RifleGirl 子对象自身的 Animator（若有）需要**禁用**，避免双 Animator 冲突

### AudioListener / Camera 清理

StarterAssets 默认场景包含 StarterAssets 自带相机和 Main Camera，可能产生多 AudioListener 警告。本 change 清理：
- 确认场景中只保留 **一个** Main Camera（含 Cinemachine Brain）
- 确认场景中只有 **一个** AudioListener

## 场景层级结构变化（Before / After）

### Before
```
PlayerArmature (Animator: StarterAssets Avatar)
  ├── PlayerArmature/[StarterAssets原始视觉子对象]  ← Active
  └── [其他控制器相关子对象]
```

### After
```
PlayerArmature (Animator: RifleGirl Humanoid Avatar ← 已切换)
  ├── PlayerArmature/[StarterAssets原始视觉子对象]  ← SetActive(false)
  ├── Rifle_Full_Body (prefab instance, local pos/rot = 0)
  │     ├── [RifleGirl 模型部件 × 15]
  │     │     ├── Rifle_Dress  (MagicaCloth2)
  │     │     └── Rifle_Jacket (MagicaCloth2)
  │     └── [RifleGirl 内部 Animator] ← 禁用（如有）
  └── [其他控制器相关子对象]
```

## 验证标准

| 验证项 | 预期结果 |
|--------|---------|
| Unity Console | 零红色错误（MagicaCloth2 初始化可能有 Log，非红色） |
| 材质渲染 | 无紫色材质（URP Toon Shader 已在 Phase 0 转换） |
| WASD 移动 | 角色在场景中移动，动画通过 Humanoid Retargeting 正确播放 |
| MagicaCloth2 | Play Mode 下裙摆/夹克布料随动作自然摆动 |
| AudioListener | Console 无 "There are 2 audio listeners in the scene" 警告 |
| 相机 | 场景中只有一个 Main Camera 对象 |
