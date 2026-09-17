using Terraria;
using TerrariaApi.Server;

namespace OTAPI.Tile
{
    public interface ITileCollection
    {
        ITile this[int x, int y] { get; set; }

        int Width { get; }

        int Height { get; }
    }
}

namespace ModFramework
{
    public interface ICollection<T>
    {
        T this[int x, int y] { get; set; }

        int Width { get; }

        int Height { get; }
    }
}

namespace TerrariaApi.Server
{
    public static class TileAdapter
    {
        static readonly WorldTiles tiles = new();

        public static OTAPI.Tile.ITileCollection Tiles
        {
            get => tiles;
            set => throw new InvalidOperationException("TML Tilemap cannot be replaced; ITile forwards to Main.tile.");
        }
    }

    sealed class WorldTiles : OTAPI.Tile.ITileCollection, ModFramework.ICollection<ITile>
    {
        TileRef last;

        public int Width => Main.tile.Width;

        public int Height => Main.tile.Height;

        public ITile this[int x, int y]
        {
            get
            {
                if (last is { Live: true } && last.X == x && last.Y == y)
                    return last;
                return last = TileRef.At(x, y);
            }
            set
            {
                var dest = TileRef.At(x, y);
                if (value is null)
                    dest.ClearEverything();
                else
                    dest.CopyFrom(value);
                last = dest;
            }
        }
    }
}
