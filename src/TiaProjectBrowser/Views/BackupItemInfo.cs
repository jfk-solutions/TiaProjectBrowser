using System.IO;
using System.Linq;
using System.Text;
using TiaFileFormat.BaseTypes;
using TiaFileFormat.Database.File;

namespace TiaAvaloniaProjectBrowser.Views
{
    public class BackupItemInfo
    {
        public string Name { get; set; }
        public int BlockNumber { get; set; }
        public OamBlockType BlockType { get; set; }
        public byte[] Data { get; set; }

        public BackupItemDataInfo BackupItemDataInfo { get; set; }

        public string DataAsString => Encoding.UTF8.GetString(Data);
        public string DataAsHexString => string.Join("", Data.Select(x => x.ToString("X").PadLeft(2, '0')));
    }

    public class BackupItemDataInfo : IBinaryDeserializable
    {
        public byte[] Head { get; set; }
        public ushort Number1 { get; set; }
        public ushort Number2 { get; set; }
        public ushort Number3 { get; set; }
        public ushort Number4 { get; set; }
        public ushort Size { get; set; }

        public byte[] Data { get; set; }

        public void ParseFromBinaryReader(BinaryReader binaryReader, FileFormat format)
        {
            Head = binaryReader.ReadBytes(4);
            Number1 = binaryReader.ReadUInt16();
            Number2 = binaryReader.ReadUInt16();
            Number3 = binaryReader.ReadUInt16();
            Number4 = binaryReader.ReadUInt16();
            Size = binaryReader.ReadUInt16();
            Data = binaryReader.ReadBytes(Size);
        }

        public override string ToString()
        {
            return "Head: " + string.Join("", Head.Select(x => x.ToString("X").PadLeft(2, '0'))) + ",Nr1: " + Number1 + ",Nr2: " + Number2 + ",Nr3: " + Number3 + ",Nr4: " + Number4 + ",Size:" + Size;
        }
    }

    public enum OamBlockType
    {
        None = 0,
        OB = 8,
        DB = 10,
        Sdb = 11,
        FC = 12,
        Sfc = 13,
        FB = 14,
        Sfb = 15
    }
}
