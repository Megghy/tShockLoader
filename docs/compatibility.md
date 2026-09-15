# 插件兼容矩阵

本文档列出 tShockLoader 对 TShock 5.2.3 插件的兼容支持情况、Relinker 运行机制与已知限制。

---

## 1. 兼容机制（Relinker）

tModLoader 将原版 `TerrariaServer.exe` / `OTAPI.dll` 重构为基于 .NET 8 和 FNA 的 `tModLoader.dll`，导致原版 TShock 插件直接加载时缺少依赖。

tShockLoader 通过内置的 Relinker 在插件加载时进行内存重写：

- **程序集引用重定向**：
  - `OTAPI` / `TerrariaServer` / `Terraria` 映射至 `tModLoader` / `TerrariaApi.Server`
  - `Microsoft.Xna.Framework` 映射至 `FNA`
  - `System.Data.SQLite`（非托管驱动）映射至宿主 SQLite 提供器
- **Hook 映射**：
  - 将 `On.OTAPI.*` 与 `On.Terraria.*` 动态挂接至 tModLoader 的 Hook 与 RuntimeDetour。
- **保留原文件**：重写过程仅发生在内存中，不修改磁盘上的插件文件。

---

## 2. 已验证插件列表

以下插件均基于主流 TShock 5.2.3（.NET 6 / .NET 8）测试验证：

| 插件名称 | 测试版本 | 兼容方式 | 已验证功能 | 限制说明 |
|---|---|---|---|---|
| **ListPlugins** | `1.0.8` | Relink | `/pllist` 查看插件列表 | 无 |
| **ConsoleSql** | `1.0.2` | Relink | Initialize 加载、控制台 SQL 查询 | 注意 SQL 操作风险 |
| **HelpPlus** | `2024.12.18.4` | Relink + Hook 映射 | 控制台 `/help` 分页浏览 | 聊天框分页正常 |
| **AnnouncementBoxPlus** | `1.0.5` | Relink + Hook 映射 | 广播盒接线事件触发 | - |
| **ItemDecoration** | `3.0.0` | Relink + Hook 映射 | Initialize 加载、物品展示解析 | - |
| **LazyAPI** | `1.0.0.8` | Relink | 基础库初始化 | 核心依赖库 |
| **AutoBroadcast** | LazyAPI | Relink | 定时广播轮播 | - |
| **AutoTeam** | LazyAPI | Relink | 进服自动分配队伍 | - |
| **Back** | LazyAPI | Relink | `/back` 传送至死亡点 | - |
| **BanNpc** | LazyAPI | Relink | 限制特定 NPC 生成 | 模组 NPC 需使用动态 ID |
| **BetterWhitelist** | LazyAPI | Relink | 白名单验证与放行 | - |
| **Ezperm** | LazyAPI | Relink | 快速权限分配 | - |
| **GoodNight** | LazyAPI | Relink | 睡眠跳过夜晚 | - |
| **ShortCommand** | LazyAPI | Relink | 指令别名快捷映射 | - |
| **TeleportRequest** | LazyAPI | Relink | `/tpa` 玩家传送请求与接受 | - |
| **VeinMiner** | LazyAPI | Relink | 连锁挖矿 | 模组矿物支持正常 |
| **RealTime** | `2.6.0.4` | Relink + Hook 映射 | 服务器时间与现实同步 | - |
| **CaiRewardChest** | - | Relink + SQLite 映射 | 开箱发奖逻辑 | 映射至宿主 SQLite |
| **TimeRate** | `1.2.2` | Relink + 字段适配 | `/times`、`/times set 60` 流速调整 | 字段差异已抹平 |
| **RecipesBrowser** | - | ❌ 暂不支持 | - | 依赖原版内部地图构建的 IL 注入 |

---

## 3. 不支持的插件特征

以下模式暂无法通过 Relinker 自动适配：

1. **依赖 `IL.OTAPI.*` 或 `IL.Terraria.*` 进行底层 IL 注入的插件**：
   - tModLoader 对原版方法体进行了较多改动，针对特定 IL 偏移量的修改无法直接应用。
2. **通过反射直接访问已被 TML 移除或结构重组的私有字段**。
3. **依赖未随插件打包的外部第三方 NuGet 库**：需将对应 DLL 一同放入 `ServerPlugins/` 目录。

---

## 4. 插件安装与排查

1. 将第三方插件 `.dll` 放入 `instance/ServerPlugins/` 目录。
2. 启动服务端，观察控制台是否有 `Loaded plugin: <插件名>` 输出。
3. 若报错找不到依赖，请检查是否缺少该插件引用的第三方外部 DLL。
4. 若报错检测到核心程序集副本，请删除 `ServerPlugins/` 中的 `TShockAPI.dll` 或 `TerrariaApi.Server.dll`。
