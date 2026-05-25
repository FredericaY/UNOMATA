## 1. 安装缺失的 Unity 包

- [x] 1.1 通过 Package Manager 安装 `com.unity.animation.rigging`（瞄准 IK 依赖）

## 2. 资产目录清理

- [x] 2.1 将 `Assets/StarterAssets/` 通过 AssetDatabase.MoveAsset 移动到 `Assets/ThirdParty/StarterAssets/`
- [x] 2.2 将 `Assets/CombatGirlsCharacterPack/` 通过 AssetDatabase.MoveAsset 移动到 `Assets/ThirdParty/CombatGirls/`
- [x] 2.3 将 `Assets/MagicaCloth2/` 通过 AssetDatabase.MoveAsset 移动到 `Assets/ThirdParty/MagicaCloth2/`
- [x] 2.4 删除 `Assets/ThirdParty/QFramework/` 空目录（含 QFramework.Toolkits.unitypackage，已移至 Assets/QFramework/）
- [x] 2.5 执行 AssetDatabase Refresh，确认 Console 无红色错误

## 3. CombatGirls URP 材质转换

- [x] 3.1 在 Unity Editor 中导入 `Assets/ThirdParty/CombatGirls/URP_UTS_Convertor_CombatGirl_Rifle.unitypackage`
- [x] 3.2 按转换器指引完成材质转换（URP_UTS_Convertor 已静默导入，shader 切换完成）
- [x] 3.3 在 Scene 视图中确认 RifleGirl 模型无紫色/粉色材质（所有 Toon 材质 isSupported=true，零 Console 错误）

## 4. QFramework 可用性验证（TODO Task 1）

- [x] 4.1 确认 Unity Editor Console 无红色编译错误（Console 红字 = 0）
- [x] 4.2 确认菜单栏出现 `QFramework` 顶级菜单入口（QFramework.CoreKit + QFramework 程序集已加载）
- [x] 4.3 新建测试脚本，添加 `using QFramework;`，确认编译通过
- [x] 4.4 在 `Assets/_Project/Scripts/Gameplay/GameApp.cs` 创建最小 `GameApp : Architecture<GameApp>` 入口类，Play Mode 正常初始化（输出"Start BuildAssetDataTable!"）
- [x] 4.5 编写最小 `Command → System → Event` 验证链路（QFrameworkValidator.cs），Console 输出"[QF验证通过] Command→System→Event 链路正常"
- [x] 4.6 无 API Updater 弹窗；QFramework ResKit 有3条 CS0618 警告（UnityWebRequest.isNetworkError 已废弃），不影响框架可用性

## 5. CombatGirls + StarterAssets 角色控制器验证（TODO Task 2）

- [x] 5.1 确认 CombatGirls RifleGirl 骨骼为 Humanoid Rig（全部 FBX 均 AnimationType.Human ✓）
- [x] 5.2 在 SampleScene 中实例化 StarterAssets PlayerArmature + PlayerFollowCamera + Ground
- [x] 5.3 骨骼兼容性：CombatGirls 与 StarterAssets 均为 Humanoid → Unity Mecanim Avatar 重定向自动处理，无骨骼冲突
- [x] 5.4 CombatGirls 动画（R_Idle/R_Walk/R_Run/R_AimIdle 等）均为 Humanoid 格式，可挂到 StarterAssets Animator Controller 上方 Layer
- [x] 5.5 Play Mode 运行正常，CharacterController+Cinemachine 初始化成功，截图确认角色立于地面（见 Assets/Screenshots/）
- [x] 5.6 结论：**方案 B**（需打补丁）。改动清单：1) 用 RifleGirl 模型替换 Geometry/Armature_Mesh 并设置 Avatar；2) 添加上半身 Animation Layer + Avatar Mask 用于持枪动画；3) 删除重复 Main Camera（双 AudioListener 警告）。已记录到 DEVELOPMENT_PLAN.md Phase 2

## 6. 文档更新

- [x] 6.1 更新 `Docs/DEPENDENCIES.md`：在 Asset Store 资产表中补充 MagicaCloth2 条目（资产名、用途、目标目录）
- [x] 6.2 更新 `Docs/DEPENDENCIES.md`：补全 CombatGirls 和 StarterAssets 的状态列（已验证-方案B）
- [x] 6.3 更新 `Docs/DEPENDENCIES.md`：末尾添加「QFramework 实测兼容性记录」章节（验证日期、结论：完全可用）
- [x] 6.4 更新 `Docs/DEVELOPMENT_PLAN.md`：Phase 0 B 端任务全部标记完成，Phase 2 补充方案B补丁清单
- [x] 6.5 更新 `Docs/TODO.md`：两个验证任务均标记已完成，结论已同步到 DEPENDENCIES.md 和 DEVELOPMENT_PLAN.md
