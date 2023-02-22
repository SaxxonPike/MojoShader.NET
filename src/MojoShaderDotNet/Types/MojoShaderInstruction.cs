namespace MojoShaderDotNet.Types;

/// <summary>
/// [Instruction; mojoshader.c]
/// </summary>
public class MojoShaderInstruction
{
    public MojoShaderOpcode Opcode { get; set; }
    public string? OpcodeString { get; set; }

    /// <summary>
    /// Number of instruction slots this opcode eats.
    /// </summary>
    public int Slots { get; set; }

    /// <summary>
    /// Mask of types that can use opcode.
    /// </summary>
    public MojoShaderShaderType ShaderTypes { get; set; }

    public MojoShaderInstructionArgs Args { get; set; }
    public int WriteMask { get; set; }
}