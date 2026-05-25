# TODO.md — 交接任务清单

> 当前迭代未完成的验证任务。完成后将对应章节移入 `DEVELOPMENT_PLAN.md` 的"已完成"段。

---

## 任务 1：验证 QFramework 可用性

### 背景
QFramework 已通过 GitHub Release（`1.0.187-Unity2018Compatible`）以 unitypackage 形式导入到 `Assets/QFramework/`。
该版本名义上面向 Unity 2018，本项目使用 Unity 2022.3.62f3 LTS。
需实测验证在当前环境下是否可正常运行，是否触发不可恢复的 API 兼容问题。

### 验收标准
- [ ] Unity Editor 打开项目无编译错误（Console 红字 0）
- [ ] 菜单栏出现 `QFramework` 顶级菜单
- [ ] 新建空脚本可正常 `using QFramework;` 并通过编译
- [ ] 按 `ARCHITECTURE.md` 写一个最小 `GameApp : Architecture<GameApp>` 入口类，能在场景中正常初始化
- [ ] 写一个最小流程跑通：`Command → System → Event → UI 监听`，确认 IOC/事件机制可用
- [ ] 若导入时弹出 API Updater，记录处理结果（已自动升级 / 残留问题清单）

### 风险预判
- Unity 2018 的某些 API 在 2022.3 可能已废弃，会触发 `API Updater` 弹窗
- 若框架内部使用了 `UnityWebRequest` 旧签名、`WWW` 等已删除 API，可能需要手动修源码
- QFramework 自带的 ResKit / UIKit / AudioKit 子模块在 URP 下可能存在 Shader 兼容问题（本项目用 URP）

### 输出物
- 在 `DEPENDENCIES.md` 末尾补一段「QFramework 实测兼容性记录」
- 若发现问题，在本文件本任务下记录现象 + 临时解决方案
- 若完全可用，删除本任务

---

## 任务 2：验证 CombatGirls + Starter Assets 角色控制器兼容性

### 背景
角色资产链：
- **CombatGirls - RifleCharacterPack**：玩家角色模型 + 步枪持枪动画（`Assets/ThirdParty/CombatGirls/`）
- **Starter Assets - Third Person Controller**（Unity 官方免费 TPS 控制器）：拟用作移动/相机/输入基础（`Assets/ThirdParty/StarterAssets/`）

需验证两者能否直接组合（套模型 + 套动画），是否需要自己写控制器。

### 验收标准
- [ ] 资产已分别从 Unity Asset Store 下载到对应 ThirdParty 目录
- [ ] 用 CombatGirls 的角色模型替换 Starter Assets 默认 Armature，模型骨骼 / 蒙皮正常
- [ ] CombatGirls 自带的步枪动画可挂到 Starter Assets 的 Animator Controller 上，无 Avatar Mask 冲突
- [ ] WASD 移动、Shift 冲刺、空格跳跃、鼠标控制相机均正常
- [ ] 步枪持枪 idle / walk / run / aim / fire 状态切换无穿模、无脚滑
- [ ] 在 SampleScene 中放置可玩 demo，能直接 Play

### 决策点（验证完成后必须给出结论之一）
- **方案 A**：Starter Assets + CombatGirls 直接套用 → 无需自写控制器
- **方案 B**：Starter Assets 基础够用但需要打补丁（如改 Animator、加 IK 层、改输入绑定）→ 列改动清单
- **方案 C**：Starter Assets 与 RifleCharacterPack 冲突过大 → 需自写 TPS 控制器，列要实现的功能清单

### 重点排查项
| 项 | 关注点 |
|---|---|
| 骨骼结构 | CombatGirls 是否 Humanoid Rig，是否需要重定向 Avatar |
| 输入系统 | Starter Assets 用 Input System Package，需确认本项目已装且 `manifest.json` 含 `com.unity.inputsystem` |
| 相机 | Starter Assets 依赖 Cinemachine，需确认 `manifest.json` 中已添加 |
| 瞄准 IK | 步枪需 Animation Rigging 做枪口对准准星，确认包已装 |
| URP 适配 | 角色材质若是 Built-in，需批量转 URP/Lit |

### 当前 `manifest.json` 缺包提醒
以下包尚未加入 `Packages/manifest.json`，验证开始前需先装：
- `com.unity.inputsystem`（Starter Assets 依赖）
- `com.unity.cinemachine`（相机）
- `com.unity.animation.rigging`（瞄准 IK，可后置）

### 输出物
- 在 `DEPENDENCIES.md` 的「Asset Store 资产」表中补全状态列（已验证 / 不兼容）
- 若选方案 C，在 `ARCHITECTURE.md` 的 Gameplay 层加一节「自研 TPS 控制器」并列接口
- 在 `Assets/_Project/Scenes/SampleScene.unity` 留下可 Play 的 demo

---

## 交接说明

- 完成顺序建议：先任务 1（QFramework 不通后续架构全废），再任务 2
- 任意一项验证失败，**先在本文件记录现象，不要回退已导入的资产**，留给后续讨论方案
- 完成的任务从本文件删除，把结论同步到对应文档（`DEPENDENCIES.md` / `ARCHITECTURE.md` / `DEVELOPMENT_PLAN.md`）
- 本文件清空后可整体删除
