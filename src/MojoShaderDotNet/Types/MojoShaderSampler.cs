namespace MojoShaderDotNet.Types;

/// <summary>
/// These are the samplers to be set for a shader. ...
///  IDirect3DDevice::SetTexture() would need this data, for example.
/// These integers are the sampler "stage". So if index==6 and
///  type==MOJOSHADER_SAMPLER_2D, that means we'd expect a regular 2D texture
///  to be specified for what would be register "s6" in D3D assembly language,
///  before drawing with the shader.
/// [MOJOSHADER_sampler; mojoshader.h]
/// </summary>
public class MojoShaderSampler
{
    public MojoShaderSamplerType Type { get; set; }
    public int Index { get; set; }

    /// <summary>
    /// A profile-specific variable name; it may be NULL if it isn't
    ///  applicable to the requested profile.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Will be non-zero if a TEXBEM opcode references this sampler. This
    ///  is only used in legacy shaders (ps_1_1 through ps_1_3), but it needs some
    ///  special support to work, as we have to load a magic uniform behind the
    ///  scenes to support it. Most code can ignore this field in general, and no
    ///  one has to touch it unless they really know what they're doing.
    /// </summary>
    public int TexBem { get; set; }
}