namespace MojoShaderDotNet.Types;

/// <summary>
/// Data types for uniforms. See <see cref="MojoShaderUniform"/> for more information.
/// [MOJOSHADER_uniformType; mojoshader.h]
/// </summary>
public enum MojoShaderUniformType
{
    /// <summary>
    /// MOJOSHADER_UNIFORM_UNKNOWN
    /// housekeeping value; never returned.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// MOJOSHADER_UNIFORM_FLOAT
    /// </summary>
    Float,

    /// <summary>
    /// MOJOSHADER_UNIFORM_INT
    /// </summary>
    Int,

    /// <summary>
    /// MOJOSHADER_UNIFORM_BOOL
    /// </summary>
    Bool
}