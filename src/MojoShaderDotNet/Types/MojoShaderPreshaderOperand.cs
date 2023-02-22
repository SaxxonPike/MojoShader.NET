namespace MojoShaderDotNet.Types;

/// <summary>
/// [MOJOSHADER_preshaderOperand; mojoshader.h]
/// </summary>
public class MojoShaderPreshaderOperand
{
    public MojoShaderPreshaderOperandType Type { get; set; }
    public int Index { get; set; }
    public List<int> ArrayRegisters { get; set; } = new();
}