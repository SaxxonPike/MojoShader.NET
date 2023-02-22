namespace MojoShaderDotNet.Types;

/// <summary>
/// [MOJOSHADER_preshaderInstruction; mojoshader.h]
/// </summary>
public class MojoShaderPreshaderInstruction
{
    public MojoShaderPreshaderOpcode Opcode { get; set; }
    public int ElementCount { get; set; }
    public List<MojoShaderPreshaderOperand> Operands { get; set; } = new();
}