# 配置与权限管理

本文档介绍 tShockLoader 的核心配置文件、数据库连接、用户组权限系统与 REST API。

---

## 1. 核心配置文件

首次启动服务端后，将在 `instance/tshock/` 目录下生成默认配置文件：

- **`config.json`**：TShock 主配置（基础设置、聊天规则、反作弊规则、REST API 开关等）。
- **`sscconfig.json`**：服务端角色（Server-Side Characters）存档配置。

### 1.1 `config.json` 常用项说明

```json
{
  "Settings": {
    "ServerPassword": "",            // 服务器进入密码（留空表示无密码）
    "ServerPort": 7777,              // 服务器监听端口
    "MaxSlots": 16,                  // 最大玩家槽位数
    "ReservedSlots": 2,              // 保留槽位数（供管理员优先进服）
    
    // 聊天设置
    "ChatFormat": "[{1}] {2}: {3}",  // 聊天格式：{1}=组名, {2}=玩家名, {3}=消息内容
    "EnableChatAboveHeads": false,   // 是否在玩家头顶显示气泡消息
    "BroadcastRGB": [127, 255, 212], // 系统广播文字颜色 (RGB)
    
    // 账号与认证
    "RequireLogin": false,           // 是否强制登录
    "AllowLoginAnyUsername": false,  // 是否允许以任意游戏昵称登录已注册账号
    "MinimumPasswordLength": 4,      // 密码最小长度
    "DisableUUIDLogin": false,       // 是否禁用根据客户端 UUID 自动登录
    
    // 反作弊与防护
    "KickOnHardcoreDeath": false,    // 极限模式玩家死亡是否自动踢出
    "BanOnMediumcoreDeath": false,   // 中核玩家死亡是否自动封禁
    "EnableCheatProtection": true,   // 启用基础作弊防护检查
    "MaxTilePacksPerSecond": 50,     // 每秒最大方块操作数据包数量
    
    // REST API
    "EnableRestApi": true,           // 是否开启 REST API
    "RestApiPort": 7878,             // REST API 监听端口
    "RestApiToken": "",              // REST API 访问 Token
    
    // 自动备份
    "AutoSave": true,                // 定期自动保存世界
    "BackupInterval": 10,            // 世界自动备份时间间隔（分钟）
    "BackupKeepFor": 240             // 备份文件保留时长（分钟）
  }
}
```

---

## 2. 数据库配置

tShockLoader 默认使用 **SQLite** 存储用户账号、用户组、权限、封禁与区域数据，支持切换为 **MySQL / MariaDB** 或 **PostgreSQL**。

在 `config.json` 的 `Settings` 块中配置存储类型与连接信息：

### SQLite（默认）
```json
{
  "Settings": {
    "StorageType": "sqlite",
    "SqlitePath": "tshock.sqlite"
  }
}
```

### MySQL / MariaDB
```json
{
  "Settings": {
    "StorageType": "mysql",
    "MySqlHost": "127.0.0.1:3306",
    "MySqlDbName": "tshock_db",
    "MySqlUsername": "tshock_user",
    "MySqlPassword": "password"
  }
}
```

### PostgreSQL
```json
{
  "Settings": {
    "StorageType": "postgres",
    "PostgresHost": "127.0.0.1",
    "PostgresPort": "5432",
    "PostgresDbName": "tshock_db",
    "PostgresUsername": "postgres",
    "PostgresPassword": "password"
  }
}
```

---

## 3. 用户组与权限系统

TShock 使用基于组（Group）的继承式权限系统。新注册玩家默认属于 `default` 组。

### 3.1 默认用户组层级

```text
superadmin (拥有全部权限 *)
    ↑
admin (管理操作、踢出、封禁、区域保护)
    ↑
newadmin / mod (基础管理、禁言、传送)
    ↑
trusted / vip (特权玩家组)
    ↑
default (默认注册玩家)
    ↑
guest (未登录访客)
```

### 3.2 常用管理指令

| 指令 | 对应权限 | 说明 |
|---|---|---|
| `/setup <Token>` | 初始设置 | 验证控制台生成的 Token 并获得临时设置权限 |
| `/user add <用户名> <密码> [组名]` | `tshock.user.create` | 创建新用户账号 |
| `/user group <用户名> <组名>` | `tshock.user.group` | 将指定用户分配到目标用户组 |
| `/user password <用户名> <新密码>` | `tshock.user.password` | 修改用户密码 |
| `/group add <组名> [父组名]` | `tshock.group.add` | 创建新用户组（可继承父组权限） |
| `/group addperm <组名> <权限节点>` | `tshock.group.addperm` | 为指定组添加权限 |
| `/group delperm <组名> <权限节点>` | `tshock.group.delperm` | 从指定组移除权限 |
| `/group list` | `tshock.group.list` | 列出所有用户组 |
| `/ban add <玩家名/IP> [原因]` | `tshock.admin.ban` | 封禁指定玩家或 IP |
| `/whitelist add <玩家名>` | `tshock.admin.whitelist` | 将玩家添加到白名单 |

### 3.3 常用权限节点

| 权限节点 | 说明 |
|---|---|
| `*` | 通配符权限，拥有全部功能 |
| `tshock.world.edit` | 允许破坏和放置方块 |
| `tshock.tp.self` | 允许使用 `/tp` 传送 |
| `tshock.tp.to` | 允许使用 `/tphere` 传送他人 |
| `tshock.admin.kick` | 允许踢出玩家 |
| `tshock.admin.ban` | 允许封禁与解封玩家 |
| `tshock.admin.item` | 允许使用 `/item` 或 `/give` 生成物品 |
| `tshock.admin.heal` | 允许使用 `/heal` 恢复状态 |
| `tshock.admin.time` | 允许修改时间和天气 |
| `tshock.admin.region` | 允许管理区域保护 |
| `tshock.rest.manage` | 允许通过 REST API 操作服务器 |

---

## 4. 区域保护

用于保护出生点或特定建筑不被未授权玩家破坏。

1. **选区操作**：
   - 手持选区工具（默认魔杖），左键点击方块设置起点 A，右键点击方块设置终点 B。
2. **定义并保护区域**：
   ```text
   /region define spawn
   /region protect spawn true
   ```
3. **设置白名单权限**：
   - 允许特定玩家操作：`/region allow <玩家名> spawn`
   - 允许特定用户组操作：`/region allowg <组名> spawn`
4. **删除区域**：`/region delete <区域名>`

---

## 5. REST API

tShockLoader 支持 TShock 的 RESTful HTTP API，便于外部系统与服务器交互。

### 5.1 启用与配置

在 `config.json` 中配置：
```json
{
  "Settings": {
    "EnableRestApi": true,
    "RestApiPort": 7878,
    "RestApiToken": "your_token_here"
  }
}
```

请求时在 URL 参数中附带 `token=<RestApiToken>`。

### 5.2 常用接口示例

- **查询服务器状态**：
  `GET http://127.0.0.1:7878/v2/server/status?token=your_token_here`
- **查看在线玩家**：
  `GET http://127.0.0.1:7878/v2/players/list?token=your_token_here`
- **执行控制台指令**：
  `POST http://127.0.0.1:7878/v2/server/rawcmd?token=your_token_here`（Body: `cmd=say hello`）
- **正常关服**：
  `POST http://127.0.0.1:7878/v2/server/off?confirm=true&token=your_token_here`
