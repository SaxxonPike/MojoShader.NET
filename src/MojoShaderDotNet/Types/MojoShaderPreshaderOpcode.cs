namespace MojoShaderDotNet.Types;

/// <summary>
/// FIXME: document me.
/// [MOJOSHADER_preshaderOpcode; mojoshader.h]
/// </summary>
public enum MojoShaderPreshaderOpcode
{
    /// <summary>
    /// MOJOSHADER_PRESHADEROP_NOP.
    /// </summary>
    Nop,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_MOV.
    /// </summary>
    Mov,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_NEG.
    /// </summary>
    Neg,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_RCP.
    /// </summary>
    Rcp,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_FRC.
    /// </summary>
    Frc,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_EXP.
    /// </summary>
    Exp,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_LOG.
    /// </summary>
    Log,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_RSQ.
    /// </summary>
    Rsq,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_SIN.
    /// </summary>
    Sin,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_COS.
    /// </summary>
    Cos,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_ASIN.
    /// </summary>
    Asin,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_ACOS.
    /// </summary>
    Acos,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_ATAN.
    /// </summary>
    Atan,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_MIN.
    /// </summary>
    Min,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_MAX.
    /// </summary>
    Max,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_LT.
    /// </summary>
    Lt,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_GE.
    /// </summary>
    Ge,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_ADD.
    /// </summary>
    Add,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_MUL.
    /// </summary>
    Mul,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_ATAN2.
    /// </summary>
    Atan2,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_DIV.
    /// </summary>
    Div,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_CMP.
    /// </summary>
    Cmp,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_MOVC.
    /// </summary>
    MovC,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_DOT.
    /// </summary>
    Dot,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_NOISE.
    /// </summary>
    Noise,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_SCALAR_OPS.
    /// </summary>
    ScalarOps,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_MIN_SCALAR.
    /// </summary>
    MinScalar = ScalarOps,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_MAX_SCALAR.
    /// </summary>
    MaxScalar,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_LT_SCALAR.
    /// </summary>
    LtScalar,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_GE_SCALAR.
    /// </summary>
    GeScalar,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_ADD_SCALAR.
    /// </summary>
    AddScalar,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_MUL_SCALAR.
    /// </summary>
    MulScalar,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_ATAN2_SCALAR.
    /// </summary>
    Atan2Scalar,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_DIV_SCALAR.
    /// </summary>
    DivScalar,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_DOT_SCALAR.
    /// </summary>
    DotScalar,

    /// <summary>
    /// MOJOSHADER_PRESHADEROP_NOISE_SCALAR.
    /// </summary>
    NoiseScalar
}