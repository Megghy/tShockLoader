# 版本与支持清单

本文档列出 tShockLoader 的版本基准与支持状态。

---

## 1. 版本基准

| 项 | 版本 / 标识 | 说明 |
|---|---|---|
| **产品版本** | `0.1.0` | tShockLoader 发行版本 |
| **发行格式** | `tShockLoader.tmod` | 单一 Mod 发行包，内置核心依赖程序集 |
| **目标运行时** | `.NET 8.0` (`net8.0`) | 匹配 tModLoader 运行时 |
| **tModLoader 基准** | `1.4.4.9+2026.07.3.0` (`TML_2026_07`) | Steam 专服基准 |
| **TShock 版本** | `5.2.3` | 内置 TShock 核心 |
| **TSAPI 契约** | `ApiVersion(2, 1)` | 插件 API 契约 |
| **上游 TShock** | Megghy/tShockLoader.TShock (`9508768863c2`) | 跟踪 Pryaxis/TShock |
| **上游 TSAPI** | Megghy/tShockLoader.TSAPI (`d4add121c246`) | 跟踪 Pryaxis/TSAPI |

---

## 2. 平台支持

| 平台 | 状态 | 说明 |
|---|---|---|
| **Windows x64** | 已验证 | 经自动化与实机测试 |
| **Linux x64** | 待验证 | 需安装 .NET 8 运行时 |

---

## 3. 功能状态

- **基础管理与权限**：正常可用（账号、权限组、封禁、白名单、区域保护、REST API）。
- **插件兼容层 (Relinker)**：正常可用（支持标准 TShock 5.2.3 插件及 On Hook 映射）。
- **模组网络与动态内容**：正常可用（ModPacket 自动放行，动态 ID 自适应）。
- **服务端角色存档 (SSC)**：默认关闭（基础角色快照已支持，完整跨模组 SSC 处于开发中）。
