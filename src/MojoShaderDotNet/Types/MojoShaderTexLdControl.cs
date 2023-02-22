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