namespace MojoShaderDotNet.Types;

/// <summary>
/// Data types for vertex attribute streams.
/// [MOJOSHADER_attributeType; mojoshader.h]
/// </summary>
public enum MojoShaderAttributeType
{
    /// <summary>
    /// MOJOSHADER_ATTRIBUTE_UNKNOWN
    /// housekeeping; not returned.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// MOJOSHADER_ATTRIBUTE_BYTE
    /// </summary>
    Byte,

    /// <summary>
    /// MOJOSHADER_ATTRIBUTE_UBYTE
    /// </summary>
    Ubyte,

    /// <summary>
    /// MOJOSHADER_ATTRIBUTE_SHORT
    /// </summary>
    Short,

    /// <summary>
    /// MOJOSHADER_ATTRIBUTE_USHORT
    /// </summary>
    Ushort,

    /// <summary>
    /// MOJOSHADER_ATTRIBUTE_INT
    /// </summary>
    Int,

    /// <summary>
    /// MOJOSHADER_ATTRIBUTE_UINT
    /// </summary>
    Uint,

    /// <summary>
    /// MOJOSHADER_ATTRIBUTE_FLOAT
    /// </summary>
    Float,

    /// <summary>
    /// MOJOSHADER_ATTRIBUTE_DOUBLE
    /// </summary>
    Double,

    /// <summary>
    /// MOJOSHADER_ATTRIBUTE_HALF_FLOAT
    /// MAYBE available in your OpenGL!
    /// </summary>
    HalfFloat
}