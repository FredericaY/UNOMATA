## ADDED Requirements

### Requirement: 第三方资产包位于规划目录

所有通过 Asset Store 或手动导入的第三方资产包，均 SHALL 位于 `Assets/ThirdParty/<PackageName>/` 目录下。根目录下不得存在错位的第三方资产文件夹（QFramework 及 QFrameworkData 除外，其路径由框架硬编码）。

#### Scenario: StarterAssets 位于正确目录

- **WHEN** 项目目录清理完成
- **THEN** `Assets/ThirdParty/StarterAssets/` 包含 StarterAssets 全部内容，`Assets/StarterAssets/` 目录不存在

#### Scenario: CombatGirlsCharacterPack 位于正确目录

- **WHEN** 项目目录清理完成
- **THEN** `Assets/ThirdParty/CombatGirls/` 包含 CombatGirlsCharacterPack 全部内容，`Assets/CombatGirlsCharacterPack/` 目录不存在

#### Scenario: MagicaCloth2 位于正确目录

- **WHEN** 项目目录清理完成
- **THEN** `Assets/ThirdParty/MagicaCloth2/` 包含 MagicaCloth2 全部内容，`Assets/MagicaCloth2/` 目录不存在

#### Scenario: ThirdParty/QFramework 空目录已删除

- **WHEN** 项目目录清理完成
- **THEN** `Assets/ThirdParty/QFramework/` 目录不存在（QFramework 在 `Assets/QFramework/` 根目录下）

---

### Requirement: 移动后无资产引用断裂

资产移动 SHALL 通过 Unity AssetDatabase API 完成，确保所有 GUID 引用保持有效，Unity Editor Console 在移动后无红色错误。

#### Scenario: 移动后 Console 无报错

- **WHEN** 所有资产移动完成，执行 AssetDatabase Refresh
- **THEN** Unity Console 中不出现与移动资产相关的编译错误或缺失引用错误

---

### Requirement: CombatGirls 材质完成 URP 转换

CombatGirls 角色材质 SHALL 转换为 URP 兼容材质（使用包内自带的 `URP_UTS_Convertor_CombatGirl_Rifle.unitypackage` 工具），转换后角色模型在 URP 项目中不出现紫色/粉色材质。

#### Scenario: 角色材质在 URP 下正常显示

- **WHEN** URP 材质转换完成，在 Scene 视图中查看 CombatGirls 角色
- **THEN** 角色模型显示正常颜色，无紫色或粉色材质占位

---

### Requirement: DEPENDENCIES.md 记录 MagicaCloth2 依赖

`Docs/DEPENDENCIES.md` 的 Asset Store 资产表 SHALL 包含 MagicaCloth2 条目，注明其用途（CombatGirls 布料物理依赖）和目标目录。

#### Scenario: DEPENDENCIES.md 包含 MagicaCloth2

- **WHEN** 文档更新完成
- **THEN** DEPENDENCIES.md Asset Store 资产表中有 MagicaCloth2 条目，包含资产名、用途、目标目录字段
