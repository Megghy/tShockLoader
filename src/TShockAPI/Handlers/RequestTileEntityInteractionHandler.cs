using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using static TShockAPI.GetDataHandlers;

namespace TShockAPI.Handlers
{
	/// <summary>
	///
	/// </summary>
	public class RequestTileEntityInteractionHandler : IPacketHandler<RequestTileEntityInteractionEventArgs>
	{
		public void OnReceive(object sender, RequestTileEntityInteractionEventArgs args)
		{
			if (args.TileEntity is TEHatRack && !args.Player.HasBuildPermissionForTileObject(args.TileEntity.Position.X, args.TileEntity.Position.Y, Utils.GetStaticPrivateFieldValue<int>(typeof(TEHatRack), "entityTileWidth"), Utils.GetStaticPrivateFieldValue<int>(typeof(TEHatRack), "entityTileHeight"), false))
			{
				args.Player.SendErrorMessage(GetString("You do not have permission to modify a Hat Rack in a protected area!"));
				args.Handled = true;
				return;
			}
			else if (args.TileEntity is TEDisplayDoll && !args.Player.HasBuildPermissionForTileObject(args.TileEntity.Position.X, args.TileEntity.Position.Y, Utils.GetStaticPrivateFieldValue<int>(typeof(TEDisplayDoll), "entityTileWidth"), Utils.GetStaticPrivateFieldValue<int>(typeof(TEDisplayDoll), "entityTileHeight"), false))
			{
				args.Player.SendErrorMessage(GetString("You do not have permission to modify a Mannequin in a protected area!"));
				args.Handled = true;
				return;
			}
			else if (!args.Player.HasBuildPermission(args.TileEntity.Position.X, args.TileEntity.Position.Y, false))
			{
				args.Player.SendErrorMessage(GetString("You do not have permission to modify a TileEntity in a protected area!"));
				TShock.Log.ConsoleDebug(GetString($"RequestTileEntityInteractionHandler: Rejected packet due to lack of building permissions! - From {args.Player.Name} | Position X:{args.TileEntity.Position.X} Y:{args.TileEntity.Position.Y}, TileEntity type: {args.TileEntity.type}, Tile type: {Main.tile[args.TileEntity.Position.X, args.TileEntity.Position.Y].TileType}"));
				args.Handled = true;
				return;
			}
		}
	}
}
