# UNOMATA

双线并行玩法验证项目。以卡普空《PRAGMATA》的双线 gameplay 为研究对象，通过简化 TPS 竞技场验证其核心设计的可扩展性。

## 项目结构

```
UNOMATA/
├── Assets/
│   ├── _Project/               # 项目自有代码与资产
│   │   ├── Scripts/
│   │   │   ├── Core/           # 底层纯C#逻辑（Phase 4 由 CardChainCore 复制迁入）
│   │   │   ├── Gameplay/       # Unity端玩法逻辑（TPS、波次、联动）
│   │   │   └── UI/             # 副线UI逻辑
│   │   ├── Prefabs/
│   │   ├── Scenes/
│   │   └── Settings/
│   ├── QFramework/             # QFramework 框架本体（路径硬编码，禁移动）
│   ├── QFrameworkData/         # QFramework 运行时配置（路径硬编码，禁移动）
│   ├── ThirdParty/             # Asset Store 资产（StarterAssets / CombatGirls / MagicaCloth2）
│   └── StreamingAssets/
├── CardChainCore/              # 独立 .NET 8 控制台工程（Core 层开发期载体）
│   ├── src/Unomata.Core/         #   类库，Phase 4 迁入 Unity
│   ├── tests/Unomata.Core.Tests/ #   xUnit 测试套件，不迁
│   └── console/Unomata.Core.Console/ # 控制台 demo，不迁
├── Docs/                       # 项目文档
├── openspec/                   # OpenSpec 规约与 change 归档
├── .codemaker/                 # Agent Rules (codemaker)
└── .clinerules/                # Agent Rules (cline)
```

## 环境要求

- Unity 2022.3.x LTS
- Universal Render Pipeline (URP)
- QFramework（通过 Package Manager 安装）
- .NET 8 SDK（用于 `CardChainCore/` 独立工程）

## 文档

- [架构说明](Docs/ARCHITECTURE.md)
- [接口约定](Docs/INTERFACE.md)
- [依赖清单](Docs/DEPENDENCIES.md)
- [开发计划](Docs/DEVELOPMENT_PLAN.md)
