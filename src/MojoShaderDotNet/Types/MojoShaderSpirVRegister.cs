namespace MojoShaderDotNet.Types;

/// <summary>
/// [RegisterList; mojoshader_profile.h]
/// </summary>
public class MojoShaderSpirVRegister : MojoShaderRegister
{
    public int IdDecl { get; set; }
    public int IsSsa { get; set; }
}