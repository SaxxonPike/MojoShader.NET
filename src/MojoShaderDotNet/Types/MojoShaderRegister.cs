namespace MojoShaderDotNet.Types;

/// <summary>
/// [RegisterList; mojoshader_profile.h]
/// </summary>
public class MojoShaderRegister
{
    public MojoShaderRegisterType RegType { get; set; }
    public int RegNum { get; set; }
    public MojoShaderUsage Usage { get; set; }
    public int Index { get; set; }
    public int WriteMask { get; set; }
    public int Misc { get; set; }
    public bool Written { get; set; }
    public List<MojoShaderVariable> Array { get; set; } = new();
}