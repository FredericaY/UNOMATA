## Context

Unity 2022.3 LTS + URP 项目，刚从 Asset Store 导入 StarterAssets、CombatGirlsCharacterPack、MagicaCloth2，三个包均落在 `Assets/` 根目录，而 DEPENDENCIES.md 规划的目标路径是 `Assets/ThirdParty/`。

QFramework 已通过 unitypackage 方式导入到 `Assets/QFramework/`（根目录，符合文档规定，不移动）。

当前 `Assets/ThirdParty/` 下对应目录（CombatGirls、StarterAssets）均为空，等待迁移。

manifest.json 已包含 `com.unity.cinemachine`（2.10.3）和 `com.unity.inputsystem`（1.14.2），缺少 `com.unity.animation.rigging`。

## Goals / Non-Goals

**Goals:**
- 将三个错位资产包移动到 ThirdParty 规划目录，不破坏 GUID 引用
- CombatGirls 材质完成 URP 转换（利用包内自带转换器）
- 验证 QFramework 5 个验收标准全部通过
- 确定 CombatGirls + StarterAssets 组合方案（A/B/C），留下可 Play 的 SampleScene demo
- 更新 DEPENDENCIES.md、TODO.md、DEVELOPMENT_PLAN.md

**Non-Goals:**
- 不实现任何游戏逻辑（接龙、射击、波次等）
- 不搭建 CardChainCore .NET 项目（Phase 1 范围）
- 不做 QFramework 的深度功能开发，只验证可用性

## Decisions

### 决策 1：资产移动使用 Unity AssetDatabase API，而非文件系统操作

**选择原因**：Unity 资产通过 GUID 跟踪，直接移动文件会破坏 `.meta` 引用。使用 UnityMCP 的 `manage_asset action=move` 调用 Unity 的 `AssetDatabase.MoveAsset()` 是唯一安全方式。

**备选方案**：手动在 Project 窗口拖拽——同等效果，但无法脚本化记录。

### 决策 2：MagicaCloth2 移动到 `Assets/ThirdParty/MagicaCloth2/`

**选择原因**：MagicaCloth2 是 CombatGirls 的依赖，逻辑上属于第三方资产。其自带 `.asmdef`（`MagicaCloth2.asmdef`），移动后 Assembly name 不变，引用不会断。

**备选方案**：留在根目录 `Assets/MagicaCloth2/`——可行但违背目录规范，后续清理成本更高。

### 决策 3：CombatGirls URP 材质转换时机——目录移动之后

**选择原因**：需要先把资产移动到正确位置，再在 Unity Editor 中双击运行 `URP_UTS_Convertor_CombatGirl_Rifle.unitypackage` 进行材质转换。若移动前转换，转换结果的路径记录会指向旧路径。

### 决策 4：QFramework 验证采用最小脚本路径

按 TODO Task 1 的验收标准，依次：
1. 确认无编译错误
2. 确认 QFramework 菜单存在
3. 写一个 `GameApp : Architecture<GameApp>` 最小入口类
4. 跑通一条 `Command → System → Event` 链路

脚本放在 `Assets/_Project/Scripts/Tests/` 或直接写内联验证，验证完后可删除。

### 决策 5：角色控制器方案选择标准

按 TODO Task 2 描述：
- 方案 A（直接套用）：骨骼匹配，动画无冲突，优先选 A
- 方案 B（需要补丁）：列出改动清单，接受小量修改
- 方案 C（自研）：作为最后退路，触发条件：骨骼结构不兼容或动画冲突无法调和

## Risks / Trade-offs

- **[风险] AssetDatabase.MoveAsset 失败** → 回退：Project 窗口手动拖拽，结果等价
- **[风险] MagicaCloth2 移动后 asmdef 引用断裂** → 缓解：移动后立即 `Refresh Assets`，观察 Console 有无 CS 报错；有报错则检查 `.asmdef` 中的 `name` 字段和引用
- **[风险] QFramework API Updater 产生不可恢复错误** → 缓解：验证前截图/记录 Console 状态，API Updater 升级后如有残留问题记录在 DEPENDENCIES.md 中，不回退资产
- **[风险] CombatGirls 骨骼与 StarterAssets 不兼容（方案C触发）** → 影响：需要在 DEVELOPMENT_PLAN.md 的 Phase 2 中增加"自研 TPS 控制器"任务，本 Change 不实现控制器，只记录结论

## Migration Plan

1. **移动资产**（UnityMCP manage_asset move）
2. **刷新 AssetDatabase**，确认无 Console 错误
3. **删除 ThirdParty/QFramework 空目录**
4. **URP 材质转换**（导入 CombatGirls 内置转换包）
5. **QFramework 验证**（写最小测试脚本，观察编译和运行结果）
6. **角色控制器验证**（在 SampleScene 中替换模型，测试移动/动画）
7. **文档更新**（DEPENDENCIES.md、TODO.md、DEVELOPMENT_PLAN.md）

回退策略：资产移动均通过 AssetDatabase，Git 可随时回滚整个 Assets 目录到提交前状态。

## Open Questions

- `com.unity.animation.rigging` 是否在本 Change 中安装？（TODO Task 2 标注"可后置"，建议本 Change 顺手装上，避免验证时缺依赖）
- 若触发方案 C，DEVELOPMENT_PLAN.md 的修改范围留到 Change 执行时确认。
