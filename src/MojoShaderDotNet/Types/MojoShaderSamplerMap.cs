namespace MojoShaderDotNet.Types;

/// <summary>
/// This struct is used if you have to force a sampler to a specific type.
///  Generally, you can ignore this, but if you have, say, a ps_1_1
///  shader, you might need to specify what the samplers are meant to be
///  to get correct results, as Shader Model 1 samples textures according
///  to what is bound to a sampler at the moment instead of what the shader
///  is hardcoded to expect.
/// [MOJOSHADER_samplerMap; mojoshader.h]
/// </summary>
public class MojoShaderSamplerMap
{
    public int Index { get; set; }
    public MojoShaderSamplerType Type { get; set; }
}