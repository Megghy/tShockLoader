using Terraria;
using Terraria.ID;

namespace TShockAPI.Extensions
{
	public static class UtilsExt
	{


    public static void CopyFrom(this Tile tile, ITile other)
    {
      tile.HasTile = other.active();
      tile.TileType = other.type;
      tile.WallType = other.wall;
      tile.WallColor = other.wallColor();
      tile.WallFrameX = other.wallFrameX();
      tile.WallFrameY = other.wallFrameY();
      tile.WallFrameNumber = other.wallFrameNumber();
      tile.TileFrameX = other.frameX;
      tile.TileFrameY = other.frameY;
      tile.TileColor = other.color();
      tile.TileFrameX = other.frameX;
      tile.TileFrameY = other.frameY;
      tile.TileFrameNumber = other.frameNumber();
      tile.BlockType = (BlockType)other.blockType();
      tile.LiquidType = other.liquidType();
      tile.LiquidAmount = other.liquid;
      tile.SkipLiquid = other.skipLiquid();
      tile.CheckingLiquid = other.checkingLiquid();
      tile.RedWire = other.wire();
      tile.BlueWire = other.wire2();
      tile.GreenWire = other.wire3();
      tile.YellowWire = other.wire4();
      tile.IsTileInvisible = other.invisibleBlock();
      tile.IsWallInvisible = other.invisibleWall();
      tile.IsTileFullbright = other.fullbrightBlock();
      tile.IsWallFullbright = other.fullbrightWall();
      tile.Slope = (SlopeType)other.slope();
      tile.HasActuator = other.actuator();
    }
  }
}
