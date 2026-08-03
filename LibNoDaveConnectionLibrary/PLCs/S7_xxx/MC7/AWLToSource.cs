using System;
using System.Text;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.AWL.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders;

namespace DotNetSiemensPLCToolBoxLibrary.PLCs.S7_xxx.MC7
{
    public static class AWLToSource
    {
        /// <summary>
        /// This is a Helper Function wich is used by GetSourceBlock
        /// </summary>
        /// <param name="datrw"></param>
        /// <param name="leerz"></param>
        /// <returns></returns>
        public static string DataRowToSource(S7DataRow datrw, string leerz, bool withoutStartValue = false)
        {
            return DataRowToSource(
                datrw,
                leerz,
                withoutStartValue,
                S7SourceOperandMode.Absolute,
                null);
        }

        public static string DataRowToSource(
            S7DataRow datrw,
            string leerz,
            bool withoutStartValue,
            S7SourceOperandMode operandMode,
            ISymbolTable symbolTable)
        {
            string retval = "";
            foreach (S7DataRow s7DataRow in datrw.Children)
            {
                string arr = "";
                string val = "";
                string cmt = "";
                if (s7DataRow.IsArray)
                {
                    arr += "ARRAY  [";
                    for (int i = 0; i < s7DataRow.ArrayStart.Count; i++)
                    {
                        if (i > 1) arr += ", ";
                        arr += s7DataRow.ArrayStart[i].ToString() + " .. " + s7DataRow.ArrayStop[i].ToString() + " ";
                    }
                    arr += "] OF ";
                }
                if (s7DataRow.DataType == S7DataRowType.STRING)
                {
                    if (s7DataRow.StartValue != null && s7DataRow.StartValue.ToString() != "" && !withoutStartValue)
                    {
                        val += " := " + s7DataRow.StartValue.ToString() + "";
                    }
                }
                else if (s7DataRow.StartValue != null && !withoutStartValue)
                {
                    string valuePrefix = "";
                    switch (s7DataRow.DataType)
                    {
                        case S7DataRowType.DWORD:
                        case S7DataRowType.WORD:
                        case S7DataRowType.BYTE:
                            valuePrefix = "16#";
                            break;
                    }
                    val += " := " + valuePrefix;

                    if ((s7DataRow.DataType == S7DataRowType.REAL))
                        val += s7DataRow.StartValueAsString;
                    else
                        val += s7DataRow.StartValue.ToString();
                }

                if (!string.IsNullOrEmpty(s7DataRow.Comment))
                    cmt += "    //" + s7DataRow.Comment;
                if (s7DataRow.DataType == S7DataRowType.STRUCT)
                {
                    retval += leerz + s7DataRow.Name + " : " + arr + s7DataRow.DataType + cmt + Environment.NewLine;
                    retval += DataRowToSource(
                        s7DataRow,
                        leerz + " ",
                        withoutStartValue,
                        operandMode,
                        symbolTable);
                    retval += leerz + "END_STRUCT ;" + Environment.NewLine;
                }
                else
                {
                    retval += leerz + s7DataRow.Name + " : " + arr;
                    retval += GetDataTypeName(s7DataRow, operandMode, symbolTable);
                    if (s7DataRow.DataType == S7DataRowType.STRING)
                    {
                        retval += " [" + s7DataRow.StringSize + "]";
                    }
                    retval += " " + val + ";" + cmt + Environment.NewLine;
                }
            }
            return retval;
        }

        private static string GetDataTypeName(
            S7DataRow dataRow,
            S7SourceOperandMode operandMode,
            ISymbolTable symbolTable)
        {
            string absoluteName;
            switch (dataRow.DataType)
            {
                case S7DataRowType.MultiInst_FB:
                    absoluteName = "FB" + dataRow.DataTypeBlockNumber;
                    break;
                case S7DataRowType.MultiInst_SFB:
                    absoluteName = "SFB" + dataRow.DataTypeBlockNumber;
                    break;
                default:
                    absoluteName = dataRow.DataType +
                        (dataRow.DataTypeBlockNumber != 0
                            ? dataRow.DataTypeBlockNumber.ToString()
                            : "");
                    break;
            }

            if (operandMode != S7SourceOperandMode.Symbolic ||
                symbolTable == null ||
                !IsBlockDataType(dataRow.DataType))
            {
                return absoluteName;
            }

            SymbolTableEntry symbol = symbolTable.GetEntryFromOperand(absoluteName);
            if (symbol == null || string.IsNullOrEmpty(symbol.Symbol))
                return absoluteName;

            return "\"" + symbol.Symbol + "\"";
        }

        private static bool IsBlockDataType(S7DataRowType dataType)
        {
            return dataType == S7DataRowType.FB ||
                dataType == S7DataRowType.SFB ||
                dataType == S7DataRowType.UDT ||
                dataType == S7DataRowType.MultiInst_FB ||
                dataType == S7DataRowType.MultiInst_SFB;
        }

        public static string DataRowValueToSource(S7DataRow datrw, string leerz)
        {
            var retval = new StringBuilder();
            foreach (S7DataRow s7DataRow in datrw.Children)
            {
                if (s7DataRow.DataType == S7DataRowType.STRUCT)
                {
                    retval.Append(DataRowValueToSource(s7DataRow, leerz + " "));
                }
                else
                {
                    retval.AppendLine(leerz + s7DataRow.Name + " := " + s7DataRow.ValueAsString + ";");
                }
            }
            return retval.ToString();
        }
    }
}
