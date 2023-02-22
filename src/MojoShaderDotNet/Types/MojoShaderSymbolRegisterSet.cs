namespace MojoShaderDotNet.Types;

/// <summary>
/// MOJOSHADER_symbol data.
/// 
/// These are used to expose high-level information in shader bytecode.
///  They associate HLSL variables with registers. This data is used for both
///  debugging and optimization.
/// [MOJOSHADER_symbolRegisterSet; mojoshader.h]
/// </summary>
public enum MojoShaderSymbolRegisterSet
{
    /// <summary>
    /// MOJOSHADER_SYMREGSET_BOOL.
    /// </summary>
    Bool = 0,

    /// <summary>
    /// MOJOSHADER_SYMREGSET_INT4.
    /// </summary>
    Int4,

    /// <summary>
    /// MOJOSHADER_SYMREGSET_FLOAT4.
    /// </summary>
    Float4,

    /// <summary>
    /// MOJOSHADER_SYMREGSET_SAMPLER.
    /// </summary>
    Sampler,

    /// <summary>
    /// MOJOSHADER_SYMREGSET_TOTAL.
    /// housekeeping value; never returned.
    /// </summary>
    Total
}