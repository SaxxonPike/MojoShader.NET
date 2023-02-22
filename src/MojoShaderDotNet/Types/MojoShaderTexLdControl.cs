namespace MojoShaderDotNet.Types;

/// <summary>
/// TEXLD becomes a different instruction with these instruction controls.
/// [mojoshader_internal.h]
/// </summary>
public enum MojoShaderTexLdControl
{
    TexLd = 0,
    TexLdP = 1,
    TexLdB = 2
}

public enum MojoShaderComparisonControl
{
    None = 0,
    Greater = 1,
    Equal = 2,
    GreaterOrEqual = 3,
    Less = 4,
    Not = 5,
    LessOrEqual = 6
}