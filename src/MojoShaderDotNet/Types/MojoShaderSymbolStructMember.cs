namespace MojoShaderDotNet.Types;

/// <summary>
/// [MOJOSHADER_symbolStructMember; mojoshader.h]
/// </summary>
public class MojoShaderSymbolStructMember
{
    public string? Name { get; set; }
    public MojoShaderSymbolTypeInfo? Info { get; set; } = new();
}