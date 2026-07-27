using DotNetSiemensPLCToolBoxLibrary.DataTypes;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.PLCs.S7_xxx.MC7;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ToolBoxLibUnitTests
{
    [TestFixture]
    public class TestCallConverter
    {
        [Test]
        public void ResolveMultiInstanceCallByStaticOffset()
        {
            var block = new S7FunctionBlock { BlockType = PLCBlockType.FB };
            var root = new S7DataRow("ROOTNODE", S7DataRowType.STRUCT, block);
            var staticParameters = new S7DataRow("STATIC", S7DataRowType.STRUCT, block);
            root.Add(staticParameters);

            staticParameters.Add(CreateMultiInstance(block, "FIRST_LIGHT", 101));
            staticParameters.Add(CreateMultiInstance(block, "SECOND_LIGHT", 101));
            block.Parameter = root;

            ClassicAssert.AreEqual(
                "#FIRST_LIGHT",
                CallConverter.GetMultiInstanceParameter(block, "FB101", 0));
            ClassicAssert.AreEqual(
                "#SECOND_LIGHT",
                CallConverter.GetMultiInstanceParameter(block, "FB101", 2));
        }

        [Test]
        public void DoesNotResolveDifferentCalledBlockAtSameOffset()
        {
            var block = new S7FunctionBlock { BlockType = PLCBlockType.FB };
            var root = new S7DataRow("ROOTNODE", S7DataRowType.STRUCT, block);
            var staticParameters = new S7DataRow("STATIC", S7DataRowType.STRUCT, block);
            root.Add(staticParameters);
            staticParameters.Add(CreateMultiInstance(block, "LIGHT", 101));
            block.Parameter = root;

            ClassicAssert.IsNull(CallConverter.GetMultiInstanceParameter(block, "FB102", 0));
        }

        private static S7DataRow CreateMultiInstance(S7FunctionBlock block, string name, int blockNumber)
        {
            var instance = new S7DataRow(name, S7DataRowType.FB, block)
            {
                DataTypeBlockNumber = blockNumber
            };
            instance.Add(new S7DataRow("VALUE", S7DataRowType.INT, block));
            return instance;
        }
    }
}
