# 常见问题 (FAQ)

本文列出使用 tShockLoader 时的常见问题与排查方法。

---

## 1. 启动与报错

### Q: 启动报错 `ServerPlugins 目录中发现了核心程序集副本`？
- **原因**：在 `tShockLoader/ServerPlugins/` 目录中放入了 `TShockAPI.dll`、`TerrariaApi.Server.dll` 或 `tShockLoader.dll`。
- **处理**：删除 `ServerPlugins/` 下的核心 DLL。核心程序集已内置于 `tShockLoader.tmod`，不应重复放置。

### Q: 报错 `System.IO.FileNotFoundException: 找不到指定的依赖程序集`？
- **原因**：部分第三方插件依赖未内置的 NuGet 库。
- **处理**：将该插件所需的依赖 `.dll` 一同放入 `tShockLoader/ServerPlugins/` 目录。

### Q: 启动时提示 `IOException: 句柄无效` (Invalid handle)？
- **原因**：在无交互控制台的环境（如部分后台服务、脚本调用）下，tModLoader 专服在模组加载后执行了 `Console.Clear()`。
- **处理**：该异常属于 tModLoader 自身捕获的良性异常，不影响服务端正常启动与运行。

---

## 2. 模组与网络

### Q: 模组的自定义数据包（ModPacket）会被拦截或误判吗？
- **说明**：不会。tShockLoader 放行了 tModLoader 的扩展协议包（249–253），仅对原版协议包（1–148）进行 TShock 权限与反作弊检查。

### Q: 模组物品在 TShock 指令与区域保护中是否受支持？
- **说明**：受支持。TShock 内部根据动态内容表处理物品，区域保护对模组方块同样生效。

---

## 3. 运维与多开

### Q: 多开服务器时如何避免数据冲突？
- **说明**：启动时使用 `-instancepath` 指定不同的实例目录，并配置不同的端口：
  - 实例 1：`-instancepath ./servers/s1/tShockLoader`，端口 `7777`
  - 实例 2：`-instancepath ./servers/s2/tShockLoader`，端口 `7778`
  各实例将使用各自独立的 `tshock.sqlite` 数据库与日志文件。

### Q: 如何安全停止服务器？
- **说明**：在控制台输入 `exit` 或 `off`，或通过 REST API 调用 `/v2/server/off`，服务器会保存世界并正常释放资源后退出。避免直接强制杀死进程。

---

## 4. 日志位置

排查问题时，可查看以下日志文件：
- **TShock 核心日志**：`<实例目录>/tshock/logs/ServerLog.txt`
- **tModLoader 日志**：`<存档目录>/Logs/server.log`
