namespace MojoShaderDotNet.Types;

/// <summary>
/// [MOJOSHADER_symbolType; mojoshader.h]
/// </summary>
public enum MojoShaderSymbolType
{
    /// <summary>
    /// MOJOSHADER_SYMTYPE_VOID.
    /// </summary>
    Void = 0,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_BOOL.
    /// </summary>
    Bool,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_INT.
    /// </summary>
    Int,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_FLOAT.
    /// </summary>
    Float,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_STRING.
    /// </summary>
    String,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_TEXTURE.
    /// </summary>
    Texture,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_TEXTURE1D.
    /// </summary>
    Texture1D,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_TEXTURE2D.
    /// </summary>
    Texture2D,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_TEXTURE3D.
    /// </summary>
    Texture3D,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_TEXTURECUBE.
    /// </summary>
    TextureCube,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_SAMPLER.
    /// </summary>
    Sampler,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_SAMPLER1D.
    /// </summary>
    Sampler1D,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_SAMPLER2D.
    /// </summary>
    Sampler2D,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_SAMPLER3D.
    /// </summary>
    Sampler3D,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_SAMPLERCUBE.
    /// </summary>
    SamplerCube,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_PIXELSHADER.
    /// </summary>
    PixelShader,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_VERTEXSHADER.
    /// </summary>
    VertexShader,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_PIXELFRAGMENT.
    /// </summary>
    PixelFragment,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_VERTEXFRAGMENT.
    /// </summary>
    VertexFragment,

    /// <summary>
    /// MOJOSHADER_SYMTYPE_UNSUPPORTED.
    /// </summary>
    Unsupported,

    /// <summary>
    /// housekeeping value; never returned.
    /// </summary>
    Total
}