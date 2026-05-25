# UNOMATA

双线并行玩法验证项目。以卡普空《PRAGMATA》的双线 gameplay 为研究对象，通过简化 TPS 竞技场验证其核心设计的可扩展性。

## 项目结构

```
UNOMATA/
├── Assets/
│   ├── _Project/               # 项目自有代码与资产
│   │   ├── Scripts/
│   │   │   ├── Core/           # 底层纯C#逻辑（接龙规则、Combo、计时）
│   │   │   ├── Gameplay/       # Unity端玩法逻辑（TPS、波次、联动）
│   │   │   └── UI/             # 副线UI逻辑
│   │   ├── Prefabs/
│   │   ├── Scenes/
│   │   └── Settings/
│   ├── ThirdParty/             # Asset Store资产
│   └── StreamingAssets/
├── CardChainCore/              # 独立.NET控制台项目（开发验收用）
├── Docs/                       # 项目文档
└── .cursor/                    # Agent Rules
```

## 环境要求

- Unity 2022.3.x LTS
- Universal Render Pipeline (URP)
- QFramework（通过 Package Manager 安装）

## 文档

- [架构说明](Docs/ARCHITECTURE.md)
- [接口约定](Docs/INTERFACE.md)
- [依赖清单](Docs/DEPENDENCIES.md)
- [开发计划](Docs/DEVELOPMENT_PLAN.md)
