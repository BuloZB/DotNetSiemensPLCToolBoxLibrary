namespace DotNetSiemensPLCToolBoxLibrary.DataTypes.AWL.Step7V5
{
    /// <summary>
    /// Controls how block and address operands are rendered in an exported S7 AWL source.
    /// </summary>
    public enum S7SourceOperandMode
    {
        /// <summary>
        /// Uses quoted symbol-table names where available and falls back to absolute operands.
        /// </summary>
        Symbolic,

        /// <summary>
        /// Uses absolute operands such as FB101, FC10, and DB20.
        /// </summary>
        Absolute
    }
}
