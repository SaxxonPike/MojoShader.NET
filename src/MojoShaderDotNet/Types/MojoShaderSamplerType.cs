namespace MojoShaderDotNet.Types;

/// <summary>
/// Data types for samplers. See <see cref="MojoShaderSampler"/> for more information.
/// [MOJOSHADER_samplerType; mojoshader.h]
/// </summary>
public enum MojoShaderSamplerType
{
    /// <summary>
    /// MOJOSHADER_SAMPLER_UNKNOWN
    /// housekeeping value; never returned.
    /// </summary>
    Unknown = -1, /* housekeeping value; never returned. */

    /// <summary>
    /// MOJOSHADER_SAMPLER_2D
    /// </summary>
    TwoD,

    /// <summary>
    /// MOJOSHADER_SAMPLER_CUBE
    /// </summary>
    Cube,

    /// <summary>
    /// MOJOSHADER_SAMPLER_VOLUME
    /// </summary>
    Volume
}