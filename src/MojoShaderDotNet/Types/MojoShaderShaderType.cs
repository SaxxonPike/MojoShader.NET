namespace MojoShaderDotNet.Types;

/// <summary>
/// These are enum values, but they also can be used in bitmasks, so we can
///  test if an opcode is acceptable: if (op->shader_types & ourtype) {} ...
/// [MOJOSHADER_shaderType; mojoshader.h]
/// </summary>
[Flags]
public enum MojoShaderShaderType
{
    /// <summary>
    /// MOJOSHADER_TYPE_UNKNOWN
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// MOJOSHADER_TYPE_PIXEL
    /// </summary>
    Pixel = 1 << 0,

    /// <summary>
    /// MOJOSHADER_TYPE_VERTEX
    /// </summary>
    Vertex = 1 << 1,

    /// <summary>
    /// MOJOSHADER_TYPE_GEOMETRY
    /// (not supported yet.)
    /// </summary>
    Geometry = 1 << 2,

    /// <summary>
    /// MOJOSHADER_TYPE_ANY
    /// used for bitmasks
    /// </summary>
    Any = 0x7FFFFFFF
}