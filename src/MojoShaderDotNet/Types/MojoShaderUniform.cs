namespace MojoShaderDotNet.Types;

/// <summary>
/// These are the uniforms to be set for a shader. "Uniforms" are what Direct3D
///  calls "Constants" ... IDirect3DDevice::SetVertexShaderConstantF() would
///  need this data, for example. These integers are register indexes. So if
///  index==6 and type==MOJOSHADER_UNIFORM_FLOAT, that means we'd expect a
///  4-float vector to be specified for what would be register "c6" in D3D
///  assembly language, before drawing with the shader.
/// [MOJOSHADER_uniform; mojoshader.h]
/// </summary>
public class MojoShaderUniform
{
    public MojoShaderUniformType Type;
    public int Index { get; set; }

    /// <summary>
    /// Means this is an array of uniforms...this happens in some
    ///  profiles when we see a relative address ("c0[a0.x]", not the usual "c0").
    ///  In those cases, the shader was built to set some range of constant
    ///  registers as an array. You should set this array with (array_count)
    ///  elements from the constant register file, starting at (index) instead of
    ///  just a single uniform. To be extra difficult, you'll need to fill in the
    ///  correct values from the MOJOSHADER_constant data into the appropriate
    ///  parts of the array, overriding the constant register file. Fun!
    /// </summary>
    public int ArrayCount { get; set; }

    /// <summary>
    /// Says whether this is a constant array; these need to be loaded
    ///  once at creation time, from the constant list and not ever updated from
    ///  the constant register file. This is a workaround for limitations in some
    ///  profiles.
    /// </summary>
    public bool Constant { get; set; }

    /// <summary>
    /// A profile-specific variable name; it may be NULL if it isn't
    ///  applicable to the requested profile.
    /// </summary>
    public string? Name { get; set; }
}