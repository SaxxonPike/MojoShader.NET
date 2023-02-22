namespace MojoShaderDotNet.Types;

/// <summary>
/// Options associated with <see cref="IMojoShader.Parse"/>
/// </summary>
public class MojoShaderParseOptions
{
    /// <summary>
    /// You can tell the generated program to swizzle certain inputs. If you know
    ///  that COLOR0 should be RGBA but you're passing in ARGB, you can specify
    ///  a swizzle of { MOJOSHADER_USAGE_COLOR, 0, {1,2,3,0} } to (swiz). If the
    ///  input register in the code would produce reg.ywzx, that swizzle would
    ///  change it to reg.wzxy ... (swiz) can be NULL.
    /// </summary>
    public List<MojoShaderSwizzle> Swiz { get; set; } =
        new();

    /// <summary>
    /// You can force the shader to expect samplers of certain types. Generally
    ///  you don't need this, as Shader Model 2 and later always specify what they
    ///  expect samplers to be (2D, cubemap, etc). Shader Model 1, however, just
    ///  uses whatever is bound to a given sampler at draw time, but this doesn't
    ///  work in OpenGL, etc. In these cases, MojoShader will default to
    ///  2D texture sampling (or cubemap sampling, in cases where it makes sense,
    ///  like the TEXM3X3TEX opcode), which works 75% of the time, but if you
    ///  really needed something else, you'll need to specify it here. This can
    ///  also be used, at your own risk, to override DCL opcodes in shaders: if
    ///  the shader explicit says 2D, but you want Cubemap, for example, you can
    ///  use this to override. If you aren't sure about any of this stuff, you can
    ///  (and should) almost certainly ignore it: (smap) can be NULL.
    /// </summary>
    public List<MojoShaderSamplerMap> SMap { get; set; } =
        new();

    /// <summary>
    /// See <see cref="MojoShaderIncludeFunction"/> for more details.
    /// If null, includes will not be processed.
    /// </summary>
    public MojoShaderIncludeFunction? Include { get; set; }

    /// <summary>
    /// See <see cref="MojoShaderPreprocessFunction"/> for more details.
    /// If null, the preprocessor will be skipped.
    /// </summary>
    public MojoShaderPreprocessFunction? Preprocess { get; set; }
}