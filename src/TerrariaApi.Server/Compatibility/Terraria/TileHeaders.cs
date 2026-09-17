using Terraria;
using Terraria.ID;

namespace TerrariaApi.Server;

static class TileHeaders
{
    internal const ushort ColorMask = 0x1F;
    internal const ushort ActiveBit = 0x20;
    internal const ushort InActiveBit = 0x40;
    internal const ushort WireBit = 0x80;
    internal const ushort Wire2Bit = 0x100;
    internal const ushort Wire3Bit = 0x200;
    internal const ushort HalfBrickBit = 0x400;
    internal const ushort ActuatorBit = 0x800;
    internal const ushort SlopeMask = 0x7000;
    internal const ushort FullbrightWallBit = 0x8000;

    internal static byte DecodeLiquid(byte header)
    {
        var bits = header & 0x60;
        if (bits == 0x60)
            return (byte)LiquidID.Shimmer;
        if (bits == 0x20)
            return (byte)LiquidID.Lava;
        if (bits == 0x40)
            return (byte)LiquidID.Honey;
        return (byte)LiquidID.Water;
    }

    internal static byte EncodeLiquid(byte header, byte liquidType)
    {
        header &= 0x9F;
        if (liquidType == LiquidID.Lava)
            return (byte)(header | 0x20);
        if (liquidType == LiquidID.Honey)
            return (byte)(header | 0x40);
        if (liquidType == LiquidID.Shimmer)
            return (byte)(header | 0x60);
        return header;
    }

    internal static ushort PackS(Tile tile)
    {
        ushort v = tile.TileColor;
        if (tile.HasTile) v |= ActiveBit;
        if (tile.IsActuated) v |= InActiveBit;
        if (tile.RedWire) v |= WireBit;
        if (tile.BlueWire) v |= Wire2Bit;
        if (tile.GreenWire) v |= Wire3Bit;
        if (tile.IsHalfBlock) v |= HalfBrickBit;
        if (tile.HasActuator) v |= ActuatorBit;
        if (tile.IsWallFullbright) v |= FullbrightWallBit;
        v |= (ushort)((int)tile.Slope << 12);
        return v;
    }

    internal static void UnpackS(Tile tile, ushort value)
    {
        tile.TileColor = (byte)(value & ColorMask);
        tile.HasTile = (value & ActiveBit) != 0;
        tile.IsActuated = (value & InActiveBit) != 0;
        tile.RedWire = (value & WireBit) != 0;
        tile.BlueWire = (value & Wire2Bit) != 0;
        tile.GreenWire = (value & Wire3Bit) != 0;
        tile.IsHalfBlock = (value & HalfBrickBit) != 0;
        tile.HasActuator = (value & ActuatorBit) != 0;
        tile.IsWallFullbright = (value & FullbrightWallBit) != 0;
        tile.Slope = (SlopeType)((value & SlopeMask) >> 12);
    }

    internal static byte PackB(Tile tile)
    {
        byte v = EncodeLiquid(tile.WallColor, (byte)tile.LiquidType);
        if (tile.YellowWire) v |= 0x80;
        return v;
    }

    internal static void UnpackB(Tile tile, byte value)
    {
        tile.WallColor = (byte)(value & 0x1F);
        tile.LiquidType = DecodeLiquid(value);
        tile.YellowWire = (value & 0x80) != 0;
    }

    internal static byte PackB2(Tile tile) => (byte)(
        ((tile.WallFrameX / 36) & 0xF)
        | ((tile.TileFrameNumber & 3) << 4)
        | ((tile.WallFrameNumber & 3) << 6));

    internal static void UnpackB2(Tile tile, byte value)
    {
        tile.WallFrameX = (value & 0xF) * 36;
        tile.TileFrameNumber = (value >> 4) & 3;
        tile.WallFrameNumber = (value >> 6) & 3;
    }

    internal static byte PackB3(Tile tile)
    {
        byte v = (byte)((tile.WallFrameY / 36) & 7);
        if (tile.SkipLiquid) v |= 8;
        if (tile.CheckingLiquid) v |= 16;
        if (tile.IsTileInvisible) v |= 32;
        if (tile.IsWallInvisible) v |= 64;
        if (tile.IsTileFullbright) v |= 128;
        return v;
    }

    internal static void UnpackB3(Tile tile, byte value)
    {
        tile.WallFrameY = (value & 7) * 36;
        tile.SkipLiquid = (value & 8) != 0;
        tile.CheckingLiquid = (value & 16) != 0;
        tile.IsTileInvisible = (value & 32) != 0;
        tile.IsWallInvisible = (value & 64) != 0;
        tile.IsTileFullbright = (value & 128) != 0;
    }
}
