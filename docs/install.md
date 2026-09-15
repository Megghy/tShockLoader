# 安装与部署

本文档说明如何在 tModLoader 专服中安装、配置、升级以及多实例部署 tShockLoader。

---

## 1. 环境要求

| 组件 | 要求 | 备注 |
|---|---|---|
| **操作系统** | Windows x64 / Linux x64 | Windows x64 已通过自动化回归测试 |
| **.NET 运行时** | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) | 专服运行依赖 .NET 8 |
| **tModLoader 专服** | `1.4.4.9+2026.07.3.0` | 对应 Steam 专服版本 |
| **TShock 基线** | `5.2.3`（TSAPI 2.1） | 已打包在 `.tmod` 内，无需单独下载 |

---

## 2. 安装步骤

1. 前往 GitHub [Releases](https://github.com/Megghy/tShockLoader/releases) 下载 `tShockLoader.tmod`。
2. 将 `tShockLoader.tmod` 放入专服根目录下的 `Mods/` 文件夹中。
3. 打开或新建 `Mods/enabled.json`，写入：
   ```json
   [
     "tShockLoader"
   ]
   ```
4. 启动服务器：

```powershell
# Windows
dotnet ./tModLoader.dll -server -config ./serverconfig.txt -tmlsavedirectory ./save -instancepath ./instance
```

```bash
# Linux
dotnet ./tModLoader.dll -server -config ./serverconfig.txt -tmlsavedirectory ./save -instancepath ./instance
```

---

## 3. 目录与路径说明

```text
Server_Root/
├── tModLoader.dll                  # 专服主程序
├── Mods/
│   ├── enabled.json                # 模组列表
│   └── tShockLoader.tmod           # 发行文件
└── instance/                       # 由 -instancepath 指定
    ├── ServerPlugins/              # 第三方插件 (.dll)
    └── tshock/                     # 配置与数据
        ├── config.json             # 核心配置
        ├── sscconfig.json          # SSC 配置
        ├── tshock.sqlite           # SQLite 数据库
        └── logs/                   # 日志 (ServerLog.txt)
```

### 启动参数优先级

- `-instancepath`：指定 tShockLoader 实例根目录（包含 `ServerPlugins/` 和 `tshock/`）。
- `-tmlsavedirectory`：指定 tModLoader 存档保存目录。
- 若同时指定两者，`-instancepath` 优先作为插件和 TShock 数据根目录；若未指定 `-instancepath`，则以 `-tmlsavedirectory` 为基准。

> **提示**：请勿把 `TShockAPI.dll` 或 `TerrariaApi.Server.dll` 复制到 `ServerPlugins/`，否则启动时会因检测到重复核心而报错退出。

---

## 4. 多实例开服

多实例可共享一份 tModLoader 专服文件，通过为每个子服指定不同的 `-instancepath` 和端口实现隔离：

```bash
# 子服 1 (端口 7777)
dotnet ./tModLoader.dll -server \
  -config ./servers/s1/serverconfig.txt \
  -tmlsavedirectory ./servers/s1/save \
  -instancepath ./servers/s1/instance

# 子服 2 (端口 7778)
dotnet ./tModLoader.dll -server \
  -config ./servers/s2/serverconfig.txt \
  -tmlsavedirectory ./servers/s2/save \
  -instancepath ./servers/s2/instance
```

不同实例使用各自独立的 `tshock.sqlite` 和日志文件，不会发生文件锁冲突。

---

## 5. 版本更新

1. 在控制台输入 `exit` 或 `off` 正常停服（确保世界与数据已保存）。
2. 用新版本的 `tShockLoader.tmod` 替换 `Mods/tShockLoader.tmod`。
3. 重新启动服务器。

更新时不要删除或覆盖 `instance/tshock/`（配置文件与数据库）和 `instance/ServerPlugins/` 目录。
