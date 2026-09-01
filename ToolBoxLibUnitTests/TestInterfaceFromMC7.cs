using System.Collections.Generic;
using System.Linq;
using DotNetSiemensPLCToolBoxLibrary.DataTypes;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.PLCs.S7_xxx.MC7;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ToolBoxLibUnitTests
{
    /// <summary>
    /// Tests for parsing the binary block interface (SSBPART / "interface in MC7") with Parameter.GetInterface.
    /// The byte layout is: 7 header bytes (byte 3-4 = length of the declarations) followed by
    /// [datatype, parametertype] pairs; STRUCT = 0x11 [paratype] [childcount]; multi instance = 0x15/0x1b [FB number LSB, MSB].
    /// </summary>
    [TestFixture]
    public class TestInterfaceFromMC7
    {
        private static byte[] BuildInterface(params byte[] declarations)
        {
            var bytes = new List<byte> { 0x01, 0x00, 0x00, (byte)(declarations.Length & 0xff), (byte)(declarations.Length >> 8), 0x00, 0x00 };
            bytes.AddRange(declarations);
            return bytes.ToArray();
        }

        private static S7DataRow Section(S7DataRow root, string name)
        {
            return root.Children.Cast<S7DataRow>().First(r => r.Name == name);
        }

        [Test(Description = "A multi instance FB whose block number low byte is a valid ParameterType (FB10 -> 0x0a = OUT_Init) must still be placed in the STATIC section")]
        public void MultiInstanceFbWithParameterTypeLikeBlockNumberIsStatic()
        {
            byte[] interfaceBytes = BuildInterface(
                0x05, 0x04,             // STAT0 : INT
                0x15, 0x0a, 0x00,       // STAT1 : FB10   (multi instance, 0x0a would be ParameterType.OUT_Init)
                0x11, 0x04, 0x02,       // STAT2 : STRUCT (2 children) -> the instance data of FB10
                0x01, 0x04,             //     BOOL
                0x05, 0x04,             //     INT
                0x01, 0x05);            // TEMP0 : BOOL

            List<string> paraList = new List<string>();
            S7DataRow root = Parameter.GetInterface(interfaceBytes, null, null, ref paraList, PLCBlockType.FB, false, new S7FunctionBlock());

            S7DataRow stat = Section(root, "STATIC");
            S7DataRow outSection = Section(root, "OUT");

            ClassicAssert.AreEqual(0, outSection.Children.Count, "the multi instance must not end up in the OUT section");
            ClassicAssert.AreEqual(3, stat.Children.Count);

            S7DataRow instance = (S7DataRow)stat.Children[1];
            ClassicAssert.AreEqual(S7DataRowType.MultiInst_FB, instance.DataType);
            ClassicAssert.AreEqual(10, instance.DataTypeBlockNumber);
            ClassicAssert.AreEqual(S7DataRowType.STRUCT, ((S7DataRow)stat.Children[2]).DataType);
            ClassicAssert.AreEqual(1, Section(root, "TEMP").Children.Count);
        }

        [Test(Description = "A multi instance SFB whose block number low byte is a valid ParameterType (SFB1 -> 0x01 = IN) must still be placed in the STATIC section")]
        public void MultiInstanceSfbWithParameterTypeLikeBlockNumberIsStatic()
        {
            byte[] interfaceBytes = BuildInterface(
                0x01, 0x01,             // IN0 : BOOL
                0x1b, 0x01, 0x00,       // STAT0 : SFB1   (0x01 would be ParameterType.IN)
                0x11, 0x04, 0x01,       // STAT1 : STRUCT (1 child)
                0x04, 0x04);            //     WORD

            List<string> paraList = new List<string>();
            S7DataRow root = Parameter.GetInterface(interfaceBytes, null, null, ref paraList, PLCBlockType.FB, false, new S7FunctionBlock());

            ClassicAssert.AreEqual(1, Section(root, "IN").Children.Count);
            S7DataRow stat = Section(root, "STATIC");
            ClassicAssert.AreEqual(2, stat.Children.Count);
            ClassicAssert.AreEqual(S7DataRowType.MultiInst_SFB, ((S7DataRow)stat.Children[0]).DataType);
            ClassicAssert.AreEqual(1, ((S7DataRow)stat.Children[0]).DataTypeBlockNumber);
        }

        [Test(Description = "Parses an FC interface with IN, OUT, nested IN_OUT structs and TEMP; the parameter order must match the order S7 uses for parameter numbers")]
        public void FunctionInterfaceWithAllSections()
        {
            byte[] interfaceBytes = BuildInterface(
                0x01, 0x01,             // IN0 : BOOL
                0x05, 0x01,             // IN1 : INT
                0x01, 0x02,             // Out0 : BOOL
                0x11, 0x03, 0x02,       // IN_OUT0 : STRUCT (2 children)
                0x05, 0x03,             //     INT
                0x06, 0x03,             //     DWORD
                0x05, 0x05);            // TEMP0 : INT

            List<string> paraList = new List<string>();
            S7DataRow root = Parameter.GetInterface(interfaceBytes, null, null, ref paraList, PLCBlockType.FC, false, new S7FunctionBlock());

            ClassicAssert.AreEqual(2, Section(root, "IN").Children.Count);
            ClassicAssert.AreEqual(1, Section(root, "OUT").Children.Count);
            ClassicAssert.AreEqual(1, Section(root, "IN_OUT").Children.Count);
            ClassicAssert.AreEqual(2, ((S7DataRow)Section(root, "IN_OUT").Children[0]).Children.Count);
            ClassicAssert.AreEqual(1, Section(root, "TEMP").Children.Count);

            // parameter indexes (used by the MC7 code) count IN, OUT, IN_OUT in declaration order
            ClassicAssert.AreEqual("IN0", Parameter.GetFunctionParameterFromNumber(root, 0).Name);
            ClassicAssert.AreEqual("IN1", Parameter.GetFunctionParameterFromNumber(root, 1).Name);
            ClassicAssert.AreEqual("Out0", Parameter.GetFunctionParameterFromNumber(root, 2).Name);
            ClassicAssert.AreEqual("IN_OUT0", Parameter.GetFunctionParameterFromNumber(root, 3).Name);
        }
    }
}
