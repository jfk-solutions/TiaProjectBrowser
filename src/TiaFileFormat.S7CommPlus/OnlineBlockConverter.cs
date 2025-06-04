using S7CommPlusDriver;
using Siemens.Simatic.Lang.Model.Idents;
using TiaFileFormat.Wrappers.CodeBlocks;

namespace TiaFileFormat.S7CommPlus
{
    public class OnlineBlockConverter
    {
        static CodeBlockConverter _codeBlockConverter = new CodeBlockConverter();

        public static CodeBlock GetOnlineCodeBlock(S7CommPlusConnection connection, uint blockRelId)
        {
            var res = connection.GetBlockXml(blockRelId, out var name, out var lang, out var number, out var blocktype, out var lineComments, out var comments, out var interfaceDescription, out var blockBody, out var functionalCode, out var intRef, out var extRef);
            var cbpd = new CodeBlockPlcData()
            {
                Name = name,
                BlockLang = (BlockLang)lang,
                BlockType = blocktype switch {
                    S7CommPlusConnection.BlockType.DB => BlockType.DB,
                    S7CommPlusConnection.BlockType.OB => BlockType.OB,
                    S7CommPlusConnection.BlockType.FB => BlockType.FB,
                    S7CommPlusConnection.BlockType.FC => BlockType.FC,
                    S7CommPlusConnection.BlockType.UDT => BlockType.UDT,
                    _ => BlockType.Undef
                },
                Number = number,
                LineComments = lineComments,
                Comment = comments,
                InterfaceDescription = interfaceDescription,
                BlockBodyDescription = blockBody,
                IntRefData = intRef,
                ExtRefData = extRef,
            };
            var cBlk = _codeBlockConverter.ParseFromPlcData(cbpd);
            return cBlk;
        }
    }
}
