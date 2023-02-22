namespace MojoShaderDotNet.Types;

/// <summary>
/// [MOJOSHADER_preshaderOperandType; mojoshader.h]
/// </summary>
public enum MojoShaderPreshaderOperandType
{
    /// <summary>
    /// MOJOSHADER_PRESHADEROPERAND_INPUT.
    /// </summary>
    Input,

    /// <summary>
    /// MOJOSHADER_PRESHADEROPERAND_OUTPUT.
    /// </summary>
    Output,

    /// <summary>
    /// MOJOSHADER_PRESHADEROPERAND_LITERAL.
    /// </summary>
    Literal,

    /// <summary>
    /// MOJOSHADER_PRESHADEROPERAND_TEMP.
    /// </summary>
    Temp
}