# 插件开发说明

本文档介绍如何为 tShockLoader 编写兼容的 TShock 插件。

---

## 1. 开发原则

1. **统一基准**：
   - 面向标准的 **TShock 5.2.3** 与 **Terraria Server API (TSAPI 2.1)** 进行编译。
   - 不需要直接引用 tShockLoader 内部适配项目，编译生成的插件可在原版 TShock 5.2 与 tShockLoader 专服中运行。
2. **动态内容上限**：
   - 在 tModLoader 环境中，物品（Item）、NPC、图格（Tile）、Buff、Prefix 的总数由模组动态决定。
   - 避免使用编译期常数 `ItemID.Count` / `NPCID.Count` 作为数组或循环的固定上限。

---

## 2. 工程配置示例 (`.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- 引用 TShock 5.2.3 与 TSAPI 2.1 依赖 -->
    <Reference Include="TerrariaApi.Server">
      <HintPath>..\lib\TerrariaApi.Server.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="TShockAPI">
      <HintPath>..\lib\TShockAPI.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="OTAPI">
      <HintPath>..\lib\OTAPI.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

</Project>
```

---

## 3. 基础插件示例

```csharp
using System;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace SamplePlugin
{
    [ApiVersion(2, 1)]
    public class SamplePlugin : TerrariaPlugin
    {
        public override string Name => "SamplePlugin";
        public override string Author => "Author";
        public override string Description => "Sample plugin for tShockLoader";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

        public SamplePlugin(Main game) : base(game)
        {
            Order = 1;
        }

        public override void Initialize()
        {
            // 注册指令
            Commands.ChatCommands.Add(new Command("sample.use", OnCommand, "sample"));

            // 注册事件
            ServerApi.Hooks.ServerJoin.Register(this, OnPlayerJoin);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.ServerJoin.Deregister(this, OnPlayerJoin);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            }
            base.Dispose(disposing);
        }

        private void OnCommand(CommandArgs args)
        {
            args.Player.SendSuccessMessage($"[SamplePlugin] 在线玩家数：{TShock.Utils.ActivePlayers()}");
        }

        private void OnPlayerJoin(JoinEventArgs args)
        {
            var player = TShock.Players[args.Who];
            player?.SendMessage("欢迎进入服务器！", Microsoft.Xna.Framework.Color.Yellow);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            // 若需取消原版操作，设置 args.Handled = true
        }
    }
}
```

---

## 4. 模组适配注意事项

### 4.1 动态 ID 查询

- 查找物品时，优先使用 `TShock.Utils.GetItemByIdOrName`，避免以 `ItemID.Count` 作为边界遍历数组。
- 在模组环境下，若需要获取当前已加载物品上限，可通过动态反射或加载后的注册表上限获取。

### 4.2 数据包处理

- `args.Handled = true`：表示阻止后续原版操作执行。
- `args.Handled = false`：表示放行，保持正常流程。
- 模组的 ModPacket/ModFile 扩展包（249–253）不会进入 TSAPI `NetGetData` 拦截。

### 4.3 可选 TML 入口

引用 `TShockLoader.Abstractions`（net6.0）。tShockLoader 会提供进程内唯一实例；普通 TShock 没有提供者。

```csharp
if (TmlBridge.TryGet(out var tml))
{
    // tml.Host.Product == "tShockLoader"
}
```
