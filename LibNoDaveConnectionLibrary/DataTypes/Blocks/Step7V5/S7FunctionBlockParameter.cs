using System;
using System.Text.RegularExpressions;

using DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.PLCs.S7_xxx.MC7;

namespace DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks.Step7V5
{
    public class S7FunctionBlockParameter
    {
        private static readonly Regex DbAccessPattern = new Regex(
            @"^(?:DB|DI)\s*(\d+)\.(?:DB|DI)([XBWD])\s*(\d+(?:\.\d+)?)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex DirectOperandPattern = new Regex(
            @"^(PEB|PEW|PED|PAB|PAW|PAD|EB|EW|ED|AB|AW|AD|MB|MW|MD|IB|IW|ID|QB|QW|QD|E|A|M|I|Q)\s*(\d+(?:\.\d+)?)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public S7FunctionBlockParameter(S7FunctionBlockRow Parent)
        {
            this.Parent = Parent;
        }

        public S7FunctionBlockRow Parent { get; set; }
        public string Comment { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public S7DataRowType ParameterDataType { get; set; }
        public S7FunctionBlockParameterDirection ParameterType { get; set; }

        public string GetValue(bool Symbolic)
        {
            if (!Symbolic)
                return Value;

            ISymbolTable symbolTable = null;
            Func<string, string, string> structuredNameResolver = null;
            if (Parent != null && Parent.Parent != null)
            {
                symbolTable = Parent.Parent.SymbolTable;
                var blocksFolder = Parent.Parent.ParentFolder as BlocksOfflineFolder;
                if (blocksFolder != null)
                {
                    structuredNameResolver = (operand, address) =>
                        Helper.TryGetStructuredName(blocksFolder, operand, address);
                }
            }

            return GetSymbolicValue(Value, symbolTable, structuredNameResolver);
        }

        internal static string GetSymbolicValue(
            string value,
            ISymbolTable symbolTable,
            Func<string, string, string> structuredNameResolver)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (symbolTable != null)
            {
                var entry = symbolTable.GetEntryFromOperand(value);
                if (entry != null)
                    return "\"" + entry.Symbol + "\"";
            }

            Match dbAccess = DbAccessPattern.Match(value);
            if (dbAccess.Success && symbolTable != null)
            {
                string dbOperand = "DB" + dbAccess.Groups[1].Value;
                var dbEntry = symbolTable.GetEntryFromOperand(dbOperand);
                if (dbEntry != null)
                {
                    string dbAddress = "DB" +
                        dbAccess.Groups[2].Value.ToUpperInvariant() + " " +
                        dbAccess.Groups[3].Value;
                    string structuredName = structuredNameResolver == null
                        ? dbAddress
                        : structuredNameResolver(dbOperand, dbAddress);
                    if (string.IsNullOrEmpty(structuredName))
                        structuredName = dbAddress;

                    return "\"" + dbEntry.Symbol + "\"." + structuredName;
                }
            }

            Match directOperand = DirectOperandPattern.Match(value);
            if (directOperand.Success)
            {
                return directOperand.Groups[1].Value.ToUpperInvariant().PadRight(7) +
                    directOperand.Groups[2].Value;
            }

            return value;
        }
    }
}
