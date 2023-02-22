namespace MojoShaderDotNet.Types;

/// <summary>
/// [CtabData; mojoshader_profile.h]
/// </summary>
public class MojoShaderCtabData
{
    public bool HaveCtab { get; set; }
    public List<MojoShaderSymbol> Symbols { get; set; } = new();
}