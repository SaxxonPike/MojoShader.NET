using MojoShaderDotNet.Types;

namespace MojoShaderDotNet;

/// <summary>
/// Contains global methods that help simplify MojoShaderDotNet.
/// </summary>
internal static class MojoShaderInternalExtensions
{
    /// <summary>
    /// [floatstr; mojoshader_profile_common.c]
    /// </summary>
    public static string FloatStr(
        this float f,
        bool leaveDecimal) =>
        leaveDecimal && float.IsInteger(f)
            ? $"{(decimal)f}.0"
            : $"{(decimal)f}";

    /// <summary>
    /// [cvtMojoToD3DSamplerType; mojoshader.c]
    /// </summary>
    public static MojoShaderTextureType CvtMojoToD3DSamplerType(
        this MojoShaderSamplerType type) =>
        type switch
        {
            MojoShaderSamplerType.Unknown => MojoShaderTextureType.Unknown,
            MojoShaderSamplerType.TwoD => MojoShaderTextureType.TwoD,
            MojoShaderSamplerType.Cube => MojoShaderTextureType.Cube,
            MojoShaderSamplerType.Volume => MojoShaderTextureType.Volume,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    /// <summary>
    /// [cvtD3DToMojoSamplerType; mojoshader.c]
    /// </summary>
    public static MojoShaderSamplerType CvtD3dToMojoSamplerType(
        this MojoShaderTextureType type) =>
        type switch
        {
            MojoShaderTextureType.Unknown => MojoShaderSamplerType.Unknown,
            MojoShaderTextureType.TwoD => MojoShaderSamplerType.TwoD,
            MojoShaderTextureType.Cube => MojoShaderSamplerType.Cube,
            MojoShaderTextureType.Volume => MojoShaderSamplerType.Volume,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}