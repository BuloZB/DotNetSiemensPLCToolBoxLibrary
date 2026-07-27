using System;
using System.Collections.Generic;
using DotNetSiemensPLCToolBoxLibrary.DataTypes;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.AWL.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders.Step7V5;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ToolBoxLibUnitTests
{
    [TestFixture]
    public class TestAwlSourceExport
    {
        [Test]
        public void SymbolicOperandsAreTheDefaultForCodeAndInterface()
        {
            S7FunctionBlock block = CreateFunctionBlock();

            string source = block.GetSourceBlock();

            StringAssert.Contains("FUNCTION_BLOCK \"LICHT_EG\"", source);
            StringAssert.Contains("LIGHT_INSTANCE : \"LICHT\" ;", source);
            StringAssert.Contains("UC    \"HELPER\";", source);
            ClassicAssert.AreEqual(source, block.GetSourceBlock(S7SourceOperandMode.Symbolic));
            ClassicAssert.AreEqual(source, block.GetSourceBlock(true));
        }

        [Test]
        public void AbsoluteOperandModeAppliesToCodeAndInterface()
        {
            S7FunctionBlock block = CreateFunctionBlock();

            string source = block.GetSourceBlock(S7SourceOperandMode.Absolute);

            StringAssert.Contains("FUNCTION_BLOCK FB41", source);
            StringAssert.Contains("LIGHT_INSTANCE : FB101 ;", source);
            StringAssert.Contains("UC    FC10;", source);
            StringAssert.DoesNotContain("\"LICHT\"", source);
            ClassicAssert.AreEqual(source, block.GetSourceBlock(false));
        }

        private static S7FunctionBlock CreateFunctionBlock()
        {
            var symbolTable = new TestSymbolTable();
            symbolTable.Add("FB41", "LICHT_EG");
            symbolTable.Add("FB101", "LICHT");
            symbolTable.Add("FC10", "HELPER");

            var programFolder = new S7ProgrammFolder { SymbolTable = symbolTable };
            var blocksFolder = new BlocksOfflineFolder { Parent = programFolder };
            var block = new S7FunctionBlock
            {
                BlockType = PLCBlockType.FB,
                BlockNumber = 41,
                MnemonicLanguage = MnemonicLanguage.German,
                ParentFolder = blocksFolder,
                Networks = new List<Network>()
            };

            var root = new S7DataRow("ROOTNODE", S7DataRowType.STRUCT, block);
            var staticParameters = new S7DataRow("STATIC", S7DataRowType.STRUCT, block);
            staticParameters.Add(new S7DataRow("LIGHT_INSTANCE", S7DataRowType.FB, block)
            {
                DataTypeBlockNumber = 101
            });
            root.Add(staticParameters);
            block.Parameter = root;

            var network = new S7FunctionBlockNetwork { Name = "Call helper" };
            network.AWLCode.Add(new S7FunctionBlockRow
            {
                Command = "UC",
                Parameter = "FC10",
                Parent = block,
                MnemonicLanguage = MnemonicLanguage.German
            });
            block.Networks.Add(network);

            return block;
        }

        private class TestSymbolTable : ProjectFolder, ISymbolTable
        {
            private readonly Dictionary<string, SymbolTableEntry> entriesByOperand =
                new Dictionary<string, SymbolTableEntry>(StringComparer.OrdinalIgnoreCase);

            public string Folder { get; set; }

            public List<SymbolTableEntry> SymbolTableEntrys { get; set; } =
                new List<SymbolTableEntry>();

            public void Add(string operand, string symbol)
            {
                var entry = new SymbolTableEntry
                {
                    Operand = operand,
                    OperandIEC = operand,
                    Symbol = symbol
                };
                entriesByOperand.Add(operand, entry);
                SymbolTableEntrys.Add(entry);
            }

            public SymbolTableEntry GetEntryFromOperand(string operand)
            {
                SymbolTableEntry entry;
                entriesByOperand.TryGetValue(operand.Replace(" ", ""), out entry);
                return entry;
            }

            public SymbolTableEntry GetEntryFromSymbol(string symbol)
            {
                return SymbolTableEntrys.Find(
                    entry => string.Equals(entry.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
