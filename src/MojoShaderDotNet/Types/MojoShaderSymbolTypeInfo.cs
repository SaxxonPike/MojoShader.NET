namespace MojoShaderDotNet.Types;

/// <summary>
/// [MOJOSHADER_symbolTypeInfo; mojoshader.h]
/// </summary>
public class MojoShaderSymbolTypeInfo
{
    public MojoShaderSymbolClass ParameterClass { get; set; }
    public MojoShaderSymbolType ParameterType { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
    public int Elements { get; set; }

    public List<MojoShaderSymbolStructMember> Members { get; set; } = new();
}