namespace MojoShaderDotNet.Types;

/// <summary>
/// These are used with <see cref="MojoShaderError"/> as special case positions.
/// [mojoshader.h]
/// </summary>
public enum MojoShaderPosition
{
    /// <summary>
    /// MOJOSHADER_POSITION_NONE.
    /// </summary>
    None = -3,

    /// <summary>
    /// MOJOSHADER_POSITION_BEFORE.
    /// </summary>
    Before = -2,

    /// <summary>
    /// MOJOSHADER_POSITION_AFTER.
    /// </summary>
    After = -1
}