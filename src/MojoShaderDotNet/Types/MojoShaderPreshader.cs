namespace MojoShaderDotNet.Types;

/// <summary>
/// [MOJOSHADER_preshader; mojoshader.h]
/// </summary>
public class MojoShaderPreshader
{
    public List<double> Literals { get; set; } = new();

    /// <summary>
    /// scalar, not vector!
    /// </summary>
    public int TempCount { get; set; }

    public List<MojoShaderSymbol> Symbols { get; set; } = new();
    public List<MojoShaderPreshaderInstruction> Instructions { get; set; } = new();
    public List<float> Registers { get; set; } = new();
}