using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TileColorCache = Terraria.TileColorCache;
using static TerrariaApi.Server.TileHeaders;

namespace TerrariaApi.Server;

public sealed class TileRef : ITile
{
    Data data;
    readonly bool live;
    readonly int x;
    readonly int y;

    internal bool Live => live;
    internal int X => x;
    internal int Y => y;
    Tile World => Main.tile[x, y];

    void Mutate(Action<Tile> mutate)
    {
        var tile = Main.tile[x, y];
        mutate(tile);
    }

    public TileRef() { }

    public TileRef(Terraria.ITile from) => CopyFrom(from);

    public TileRef(TileRef from) => CopyFrom(from);

    TileRef(int x, int y)
    {
        live = true;
        this.x = x;
        this.y = y;
    }

    internal static TileRef At(int x, int y) => new(x, y);

    public void Initialise() { }

    public static void SmoothSlope(int x, int y, bool applyToNeighbors = true, bool sync = false)
        => Tile.SmoothSlope(x, y, applyToNeighbors, sync);

    public int collisionType
    {
        get
        {
            if (!active())
                return 0;
            if (halfBrick())
                return 2;
            var slope = this.slope();
            if (slope > 0)
                return 2 + slope;
            return inActive() ? -1 : 1;
        }
    }

    public ushort type
    {
        get => live ? World.TileType : data.type;
        set { if (live) Mutate(t => t.TileType = value); else data.type = value; }
    }

    public ushort wall
    {
        get => live ? World.WallType : data.wall;
        set { if (live) Mutate(t => t.WallType = value); else data.wall = value; }
    }

    public byte liquid
    {
        get => live ? World.LiquidAmount : data.liquid;
        set { if (live) Mutate(t => t.LiquidAmount = value); else data.liquid = value; }
    }

    public ushort sTileHeader
    {
        get => live ? PackS(World) : data.sTileHeader;
        set { if (live) Mutate(t => UnpackS(t, value)); else data.sTileHeader = value; }
    }

    public byte bTileHeader
    {
        get => live ? PackB(World) : data.bTileHeader;
        set { if (live) Mutate(t => UnpackB(t, value)); else data.bTileHeader = value; }
    }

    public byte bTileHeader2
    {
        get => live ? PackB2(World) : data.bTileHeader2;
        set { if (live) Mutate(t => UnpackB2(t, value)); else data.bTileHeader2 = value; }
    }

    public byte bTileHeader3
    {
        get => live ? PackB3(World) : data.bTileHeader3;
        set { if (live) Mutate(t => UnpackB3(t, value)); else data.bTileHeader3 = value; }
    }

    public short frameX
    {
        get => live ? World.TileFrameX : data.frameX;
        set { if (live) Mutate(t => t.TileFrameX = value); else data.frameX = value; }
    }

    public short frameY
    {
        get => live ? World.TileFrameY : data.frameY;
        set { if (live) Mutate(t => t.TileFrameY = value); else data.frameY = value; }
    }

    public bool HasTile { get => active(); set => active(value); }
    public ushort TileType { get => type; set => type = value; }
    public ushort WallType { get => wall; set => wall = value; }
    public byte LiquidAmount { get => liquid; set => liquid = value; }
    public bool IsActuated { get => inActive(); set => inActive(value); }

    public object Clone()
    {
        var copy = new TileRef();
        copy.CopyFrom(this);
        return copy;
    }

    public void CopyFrom(Terraria.ITile from)
    {
        if (from is null)
            return;
        if (live && from is TileRef { live: true } other)
        {
            Mutate(t => t.CopyFrom(other.World));
            return;
        }

        type = from.type;
        wall = from.wall;
        liquid = from.liquid;
        sTileHeader = from.sTileHeader;
        bTileHeader = from.bTileHeader;
        bTileHeader2 = from.bTileHeader2;
        bTileHeader3 = from.bTileHeader3;
        frameX = from.frameX;
        frameY = from.frameY;
    }

    public void ClearEverything()
    {
        if (live)
            World.ClearEverything();
        else
            data = default;
    }

    public void ClearTile()
    {
        if (live)
        {
            World.ClearTile();
            return;
        }

        active(false);
        inActive(false);
        halfBrick(false);
        slope(0);
    }

    public void ClearMetadata()
    {
        if (live)
        {
            var tile = World;
            tile.HasTile = false;
            tile.IsActuated = false;
            tile.HasActuator = false;
            tile.Slope = SlopeType.Solid;
            tile.IsHalfBlock = false;
            tile.TileFrameX = 0;
            tile.TileFrameY = 0;
            tile.WallFrameX = 0;
            tile.WallFrameY = 0;
            tile.TileFrameNumber = 0;
            tile.WallFrameNumber = 0;
            tile.TileColor = 0;
            tile.WallColor = 0;
            tile.LiquidAmount = 0;
            tile.LiquidType = 0;
            tile.SkipLiquid = false;
            tile.CheckingLiquid = false;
            tile.RedWire = false;
            tile.BlueWire = false;
            tile.GreenWire = false;
            tile.YellowWire = false;
            tile.IsTileInvisible = false;
            tile.IsWallInvisible = false;
            tile.IsTileFullbright = false;
            tile.IsWallFullbright = false;
            return;
        }

        var savedType = type;
        var savedWall = wall;
        data = default;
        data.type = savedType;
        data.wall = savedWall;
    }

    public void ResetToType(ushort type)
    {
        if (live)
        {
            World.ResetToType(type);
            return;
        }

        ClearMetadata();
        this.type = type;
        active(true);
    }

    public void Clear(TileDataType types)
    {
        if (live)
        {
            World.Clear(types);
            return;
        }

        if ((types & TileDataType.Tile) != 0)
        {
            type = 0;
            active(false);
            frameX = 0;
            frameY = 0;
        }
        if ((types & TileDataType.Wall) != 0)
            wall = 0;
        if ((types & TileDataType.TilePaint) != 0)
            color(0);
        if ((types & TileDataType.WallPaint) != 0)
            wallColor(0);
        if ((types & TileDataType.Liquid) != 0)
        {
            liquid = 0;
            liquidType(0);
        }
        if ((types & TileDataType.Wiring) != 0)
        {
            wire(false);
            wire2(false);
            wire3(false);
            wire4(false);
            actuator(false);
        }
        if ((types & TileDataType.Slope) != 0)
        {
            halfBrick(false);
            slope(0);
        }
    }

    public bool isTheSameAs(Terraria.ITile compTile)
    {
        if (compTile is null)
            return false;
        if (sTileHeader != compTile.sTileHeader || wall != compTile.wall || liquid != compTile.liquid)
            return false;
        if (liquid > 0 && liquidType() != compTile.liquidType())
            return false;
        if (bTileHeader3 != compTile.bTileHeader3)
            return false;
        if (active() && (type != compTile.type || Main.tileFrameImportant[type] && (frameX != compTile.frameX || frameY != compTile.frameY)))
            return false;
        return true;
    }

    public int blockType() => halfBrick() ? 1 : slope() is var s && s > 0 ? s + 1 : 0;

    public bool nactive() => active() && !inActive();

    public bool active() => live ? World.HasTile : (data.sTileHeader & ActiveBit) != 0;
    public void active(bool value) => SetS(ActiveBit, value, t => t.HasTile = value);

    public bool inActive() => live ? World.IsActuated : (data.sTileHeader & InActiveBit) != 0;
    public void inActive(bool value) => SetS(InActiveBit, value, t => t.IsActuated = value);

    public bool actuator() => live ? World.HasActuator : (data.sTileHeader & ActuatorBit) != 0;
    public void actuator(bool value) => SetS(ActuatorBit, value, t => t.HasActuator = value);

    public bool halfBrick() => live ? World.IsHalfBlock : (data.sTileHeader & HalfBrickBit) != 0;
    public void halfBrick(bool value) => SetS(HalfBrickBit, value, t => t.IsHalfBlock = value);

    public bool wire() => live ? World.RedWire : (data.sTileHeader & WireBit) != 0;
    public void wire(bool value) => SetS(WireBit, value, t => t.RedWire = value);

    public bool wire2() => live ? World.BlueWire : (data.sTileHeader & Wire2Bit) != 0;
    public void wire2(bool value) => SetS(Wire2Bit, value, t => t.BlueWire = value);

    public bool wire3() => live ? World.GreenWire : (data.sTileHeader & Wire3Bit) != 0;
    public void wire3(bool value) => SetS(Wire3Bit, value, t => t.GreenWire = value);

    public bool wire4() => live ? World.YellowWire : (data.bTileHeader & 0x80) != 0;
    public void wire4(bool value)
    {
        if (live)
            Mutate(t => t.YellowWire = value);
        else
            data.bTileHeader = value ? (byte)(data.bTileHeader | 0x80) : (byte)(data.bTileHeader & 0x7F);
    }

    public byte slope() => live ? (byte)World.Slope : (byte)((data.sTileHeader & SlopeMask) >> 12);
    public void slope(byte slope)
    {
        if (live)
            Mutate(t => t.Slope = (SlopeType)slope);
        else
            data.sTileHeader = (ushort)((data.sTileHeader & ~SlopeMask) | ((slope & 7) << 12));
    }

    public byte color() => live ? World.TileColor : (byte)(data.sTileHeader & ColorMask);
    public void color(byte color)
    {
        if (live)
            Mutate(t => t.TileColor = color);
        else
            data.sTileHeader = (ushort)((data.sTileHeader & ~ColorMask) | (color & ColorMask));
    }

    public byte wallColor() => live ? World.WallColor : (byte)(data.bTileHeader & 0x1F);
    public void wallColor(byte wallColor)
    {
        if (live)
            Mutate(t => t.WallColor = wallColor);
        else
            data.bTileHeader = (byte)((data.bTileHeader & 0xE0) | (wallColor & 0x1F));
    }

    public byte liquidType() => live ? (byte)World.LiquidType : DecodeLiquid(data.bTileHeader);
    public void liquidType(int liquidType)
    {
        if (live)
            Mutate(t => t.LiquidType = liquidType);
        else
            data.bTileHeader = EncodeLiquid(data.bTileHeader, (byte)liquidType);
    }

    public bool lava() => liquidType() == LiquidID.Lava;
    public void lava(bool lava) => SetLiquid(LiquidID.Lava, lava);

    public bool honey() => liquidType() == LiquidID.Honey;
    public void honey(bool honey) => SetLiquid(LiquidID.Honey, honey);

    public bool shimmer() => liquidType() == LiquidID.Shimmer;
    public void shimmer(bool shimmer) => SetLiquid(LiquidID.Shimmer, shimmer);

    public bool skipLiquid() => live ? World.SkipLiquid : (data.bTileHeader3 & 8) != 0;
    public void skipLiquid(bool value) => SetB3(8, value, t => t.SkipLiquid = value);

    public bool checkingLiquid() => live ? World.CheckingLiquid : (data.bTileHeader3 & 16) != 0;
    public void checkingLiquid(bool value) => SetB3(16, value, t => t.CheckingLiquid = value);

    public bool invisibleBlock() => live ? World.IsTileInvisible : (data.bTileHeader3 & 32) != 0;
    public void invisibleBlock(bool value) => SetB3(32, value, t => t.IsTileInvisible = value);

    public bool invisibleWall() => live ? World.IsWallInvisible : (data.bTileHeader3 & 64) != 0;
    public void invisibleWall(bool value) => SetB3(64, value, t => t.IsWallInvisible = value);

    public bool fullbrightBlock() => live ? World.IsTileFullbright : (data.bTileHeader3 & 128) != 0;
    public void fullbrightBlock(bool value) => SetB3(128, value, t => t.IsTileFullbright = value);

    public bool fullbrightWall() => live ? World.IsWallFullbright : (data.sTileHeader & FullbrightWallBit) != 0;
    public void fullbrightWall(bool value) => SetS(FullbrightWallBit, value, t => t.IsWallFullbright = value);

    public int wallFrameX() => live ? World.WallFrameX : (data.bTileHeader2 & 0xF) * 36;
    public void wallFrameX(int wallFrameX)
    {
        if (live)
            Mutate(t => t.WallFrameX = wallFrameX);
        else
            data.bTileHeader2 = (byte)((data.bTileHeader2 & 0xF0) | ((wallFrameX / 36) & 0xF));
    }

    public int wallFrameY() => live ? World.WallFrameY : (data.bTileHeader3 & 7) * 36;
    public void wallFrameY(int wallFrameY)
    {
        if (live)
            Mutate(t => t.WallFrameY = wallFrameY);
        else
            data.bTileHeader3 = (byte)((data.bTileHeader3 & 0xF8) | ((wallFrameY / 36) & 7));
    }

    public byte frameNumber() => live ? (byte)World.TileFrameNumber : (byte)((data.bTileHeader2 & 0x30) >> 4);
    public void frameNumber(byte frameNumber)
    {
        if (live)
            Mutate(t => t.TileFrameNumber = frameNumber);
        else
            data.bTileHeader2 = (byte)((data.bTileHeader2 & 0xCF) | ((frameNumber & 3) << 4));
    }

    public byte wallFrameNumber() => live ? (byte)World.WallFrameNumber : (byte)((data.bTileHeader2 & 0xC0) >> 6);
    public void wallFrameNumber(byte wallFrameNumber)
    {
        if (live)
            Mutate(t => t.WallFrameNumber = wallFrameNumber);
        else
            data.bTileHeader2 = (byte)((data.bTileHeader2 & 0x3F) | ((wallFrameNumber & 3) << 6));
    }

    public bool topSlope() => slope() is 1 or 2;
    public bool bottomSlope() => slope() is 3 or 4;
    public bool leftSlope() => slope() is 2 or 4;
    public bool rightSlope() => slope() is 1 or 3;
    public bool HasSameSlope(Terraria.ITile tile) => tile is not null && slope() == tile.slope();

    public Color actColor(Color oldColor)
    {
        if (!inActive())
            return oldColor;
        return new Color((byte)(0.4 * oldColor.R), (byte)(0.4 * oldColor.G), (byte)(0.4 * oldColor.B), oldColor.A);
    }

    public void actColor(ref Vector3 oldColor)
    {
        if (inActive())
            oldColor *= 0.4f;
    }

    public void CopyPaintAndCoating(Terraria.ITile other)
    {
        if (other is null)
            return;
        color(other.color());
        fullbrightBlock(other.fullbrightBlock());
        invisibleBlock(other.invisibleBlock());
    }

    public TileColorCache BlockColorAndCoating() => live
        ? World.BlockColorAndCoating()
        : new TileColorCache { Color = color(), FullBright = fullbrightBlock(), Invisible = invisibleBlock() };

    public TileColorCache WallColorAndCoating() => live
        ? World.WallColorAndCoating()
        : new TileColorCache { Color = wallColor(), FullBright = fullbrightWall(), Invisible = invisibleWall() };

    public void UseBlockColors(TileColorCache cache)
    {
        color(cache.Color);
        fullbrightBlock(cache.FullBright);
        invisibleBlock(cache.Invisible);
    }

    public void UseWallColors(TileColorCache cache)
    {
        wallColor(cache.Color);
        fullbrightWall(cache.FullBright);
        invisibleWall(cache.Invisible);
    }

    public void ClearBlockPaintAndCoating()
    {
        color(0);
        fullbrightBlock(false);
        invisibleBlock(false);
    }

    public void ClearWallPaintAndCoating()
    {
        wallColor(0);
        fullbrightWall(false);
        invisibleWall(false);
    }

    void SetS(ushort bit, bool value, Action<Terraria.Tile> liveSet)
    {
        if (live)
            liveSet(World);
        else
            data.sTileHeader = value ? (ushort)(data.sTileHeader | bit) : (ushort)(data.sTileHeader & ~bit);
    }

    void SetB3(byte bit, bool value, Action<Terraria.Tile> liveSet)
    {
        if (live)
            liveSet(World);
        else
            data.bTileHeader3 = value ? (byte)(data.bTileHeader3 | bit) : (byte)(data.bTileHeader3 & ~bit);
    }

    void SetLiquid(int id, bool value)
    {
        if (value)
            liquidType(id);
        else if (liquidType() == id)
            liquidType(LiquidID.Water);
    }

    struct Data
    {
        public ushort type;
        public ushort wall;
        public byte liquid;
        public ushort sTileHeader;
        public byte bTileHeader;
        public byte bTileHeader2;
        public byte bTileHeader3;
        public short frameX;
        public short frameY;
    }
}
