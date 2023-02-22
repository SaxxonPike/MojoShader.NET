namespace MojoShaderDotNet.Types;

/// <summary>
/// [MOJOSHADER_symbolClass; mojoshader.h]
/// </summary>
public enum MojoShaderSymbolClass
{
    /// <summary>
    /// MOJOSHADER_SYMCLASS_SCALAR.
    /// </summary>
    Scalar = 0,

    /// <summary>
    /// MOJOSHADER_SYMCLASS_VECTOR.
    /// </summary>
    Vector,

    /// <summary>
    /// MOJOSHADER_SYMCLASS_MATRIX_ROWS.
    /// </summary>
    MatrixRows,

    /// <summary>
    /// MOJOSHADER_SYMCLASS_MATRIX_COLUMNS.
    /// </summary>
    MatrixColumns,

    /// <summary>
    /// MOJOSHADER_SYMCLASS_OBJECT.
    /// </summary>
    Object,

    /// <summary>
    /// MOJOSHADER_SYMCLASS_STRUCT.
    /// </summary>
    Struct,

    /// <summary>
    /// MOJOSHADER_SYMCLASS_TOTAL.
    /// housekeeping value; never returned.
    /// </summary>
    Total
}