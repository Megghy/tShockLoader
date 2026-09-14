using System.IO;

namespace OTAPI
{
    public class PacketWriter : BinaryWriter
    {
        public PacketWriter(Stream output)
            : base(output)
        {
        }
    }
}
