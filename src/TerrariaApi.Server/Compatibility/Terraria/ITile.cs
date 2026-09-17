using Microsoft.Xna.Framework;
using Terraria.DataStructures;

namespace Terraria
{
    public interface ITile
    {
        int collisionType { get; }

        ushort type { get; set; }

        ushort wall { get; set; }

        byte liquid { get; set; }

        ushort sTileHeader { get; set; }

        byte bTileHeader { get; set; }

        byte bTileHeader2 { get; set; }

        byte bTileHeader3 { get; set; }

        short frameX { get; set; }

        short frameY { get; set; }

        object Clone();

        void ClearEverything();

        void ClearTile();

        void CopyFrom(ITile from);

        bool isTheSameAs(ITile compTile);

        int blockType();

        void liquidType(int liquidType);

        byte liquidType();

        bool nactive();

        void ResetToType(ushort type);

        void ClearMetadata();

        Color actColor(Color oldColor);

        void actColor(ref Vector3 oldColor);

        bool topSlope();

        bool bottomSlope();

        bool leftSlope();

        bool rightSlope();

        bool HasSameSlope(ITile tile);

        byte wallColor();

        void wallColor(byte wallColor);

        bool lava();

        void lava(bool lava);

        bool honey();

        void honey(bool honey);

        bool shimmer();

        void shimmer(bool shimmer);

        bool wire4();

        void wire4(bool wire4);

        int wallFrameX();

        void wallFrameX(int wallFrameX);

        byte frameNumber();

        void frameNumber(byte frameNumber);

        byte wallFrameNumber();

        void wallFrameNumber(byte wallFrameNumber);

        int wallFrameY();

        void wallFrameY(int wallFrameY);

        bool checkingLiquid();

        void checkingLiquid(bool checkingLiquid);

        bool skipLiquid();

        void skipLiquid(bool skipLiquid);

        bool invisibleBlock();

        void invisibleBlock(bool invisibleBlock);

        bool invisibleWall();

        void invisibleWall(bool invisibleWall);

        bool fullbrightBlock();

        void fullbrightBlock(bool fullbrightBlock);

        byte color();

        void color(byte color);

        bool active();

        void active(bool active);

        bool inActive();

        void inActive(bool inActive);

        bool wire();

        void wire(bool wire);

        bool wire2();

        void wire2(bool wire2);

        bool wire3();

        void wire3(bool wire3);

        bool halfBrick();

        void halfBrick(bool halfBrick);

        bool actuator();

        void actuator(bool actuator);

        byte slope();

        void slope(byte slope);

        bool fullbrightWall();

        void fullbrightWall(bool fullbrightWall);

        void Clear(TileDataType types);

        void CopyPaintAndCoating(ITile other);

        TileColorCache BlockColorAndCoating();

        TileColorCache WallColorAndCoating();

        void UseBlockColors(TileColorCache cache);

        void UseWallColors(TileColorCache cache);

        void ClearBlockPaintAndCoating();

        void ClearWallPaintAndCoating();

        bool HasTile { get => active(); set => active(value); }

        ushort TileType { get => type; set => type = value; }

        ushort WallType { get => wall; set => wall = value; }

        byte LiquidAmount { get => liquid; set => liquid = value; }

        bool IsActuated { get => inActive(); set => inActive(value); }
    }
}
