namespace MojoShaderDotNet.Types;

/// <summary>
/// Data types for attributes. See <see cref="MojoShaderAttribute"/> for more information.
/// [MOJOSHADER_usage; mojoshader.h]
/// </summary>
public enum MojoShaderUsage
{
    /// <summary>
    /// MOJOSHADER_USAGE_UNKNOWN.
    /// housekeeping value; never returned.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// MOJOSHADER_USAGE_POSITION.
    /// 0-15 for Vertex, 1-15 for Pixel
    /// </summary>
    Position,

    /// <summary>
    /// MOJOSHADER_USAGE_BLENDWEIGHT.
    /// 0-15
    /// </summary>
    BlendWeight,

    /// <summary>
    /// MOJOSHADER_USAGE_BLENDINDICES.
    /// 0-15
    /// </summary>
    BlendIndices,

    /// <summary>
    /// MOJOSHADER_USAGE_NORMAL.
    /// 0-15
    /// </summary>
    Normal,

    /// <summary>
    /// MOJOSHADER_USAGE_POINTSIZE.
    /// 0-15
    /// </summary>
    PointSize,

    /// <summary>
    /// MOJOSHADER_USAGE_TEXCOORD.
    /// 0-15
    /// </summary>
    TexCoord,

    /// <summary>
    /// MOJOSHADER_USAGE_TANGENT.
    /// 0-15
    /// </summary>
    Tangent,

    /// <summary>
    /// MOJOSHADER_USAGE_BINORMAL.
    /// 0-15
    /// </summary>
    Binormal,

    /// <summary>
    /// MOJOSHADER_USAGE_TESSFACTOR.
    /// 0 only
    /// </summary>
    TessFactor,

    /// <summary>
    /// MOJOSHADER_USAGE_POSITIONT.
    /// 0-15 for Vertex, 1-15 for Pixel
    /// </summary>
    PositionT,

    /// <summary>
    /// MOJOSHADER_USAGE_COLOR.
    /// 0-15 but depends on MRT support
    /// </summary>
    Color,

    /// <summary>
    /// MOJOSHADER_USAGE_FOG.
    /// 0-15
    /// </summary>
    Fog,

    /// <summary>
    /// MOJOSHADER_USAGE_DEPTH.
    /// 0-15
    /// </summary>
    Depth,

    /// <summary>
    /// MOJOSHADER_USAGE_SAMPLE.
    /// </summary>
    Sample,

    /// <summary>
    /// MOJOSHADER_USAGE_TOTAL.
    /// housekeeping value; never returned.
    /// </summary>
    Total
}