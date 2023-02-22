namespace MojoShaderDotNet.Types;

/// <summary>
/// [MOJOSHADER_symbol; mojoshader.h]
/// </summary>
public class MojoShaderSymbol
{
    public string? Name { get; set; }
    public MojoShaderSymbolRegisterSet RegisterSet { get; set; }
    public int RegisterIndex { get; set; }
    public int RegisterCount { get; set; }
    public MojoShaderSymbolTypeInfo Info { get; set; } = new();
}