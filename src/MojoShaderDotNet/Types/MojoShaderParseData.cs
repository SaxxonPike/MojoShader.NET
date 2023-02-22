using System.Text;

namespace MojoShaderDotNet.Types;

/// <summary>
/// Structure used to return data from parsing of a shader...
/// !!! FIXME: most of these ints should be unsigned.
/// [MOJOSHADER_parseData; mojoshader.h]
/// </summary>
public class MojoShaderParseData
{
    /// <summary>
    /// Elements of data that specify errors that were generated
    ///  by parsing this shader.
    /// </summary>
    public List<MojoShaderError> Errors { get; set; } =
        new();

    /// <summary>
    /// The name of the profile used to parse the shader. Will be NULL on error.
    /// </summary>
    public string? Profile { get; set; }

    /// <summary>
    /// Bytes of output from parsing. Most profiles produce a string of source
    ///  code, but profiles that do binary output may not be text at all.
    ///  Will be NULL on error.
    /// </summary>
    public byte[]? Output { get; set; }

    /// <summary>
    /// Count of Direct3D instruction slots used. This is meaningless in terms
    ///  of the actual output, as the profile will probably grow or reduce
    ///  the count (or for high-level languages, not have that information at
    ///  all). Also, as with Microsoft's own assembler, this value is just a
    ///  rough estimate, as unpredictable real-world factors make the actual
    ///  value vary at least a little from this count. Still, it can give you
    ///  a rough idea of the size of your shader. Will be zero on error.
    /// </summary>
    public int InstructionCount { get; set; }

    /// <summary>
    /// The type of shader we parsed. Will be MOJOSHADER_TYPE_UNKNOWN on error.
    /// </summary>
    public MojoShaderShaderType ShaderType { get; set; } = MojoShaderShaderType.Unknown;

    /// <summary>
    /// The shader's major version. If this was a "vs_3_0", this would be 3.
    /// </summary>
    public int MajorVer { get; set; }

    /// <summary>
    /// The shader's minor version. If this was a "ps_1_4", this would be 4.
    ///  Two notes: for "vs_2_x", this is 1, and for "vs_3_sw", this is 255.
    /// </summary>
    public int MinorVer { get; set; }

    /// <summary>
    /// This is the main function name of the shader. This will be the
    ///  caller-supplied string even if a given profile ignores it (GLSL,
    ///  for example, always uses "main" in the shader output out of necessity,
    ///  and Direct3D assembly has no concept of a "main function", etc).
    ///  Otherwise, it'll be a default name chosen by the profile ("main") or
    ///  whatnot.
    /// </summary>
    public string? MainFn { get; set; }

    /// <summary>
    /// Elements of data that specify Uniforms to be set for
    ///  this shader. See discussion on MOJOSHADER_uniform for details.
    /// </summary>
    public List<MojoShaderUniform> Uniforms { get; set; } =
        new();

    /// <summary>
    /// Elements of data that specify constants used in
    ///  this shader. See discussion on MOJOSHADER_constant for details.
    /// This is largely informational: constants are hardcoded into a shader.
    ///  The constants that you can set like parameters are in the "uniforms"
    ///  list.
    /// </summary>
    public List<MojoShaderConstant> Constants { get; set; } =
        new();

    /// <summary>
    /// Elements of data that specify Samplers to be set for
    ///  this shader. See discussion on MOJOSHADER_sampler for details.
    /// </summary>
    public List<MojoShaderSampler> Samplers { get; set; } =
        new();

    /// <summary>
    /// Elements of data that specify Attributes to be set
    ///  for this shader. See discussion on MOJOSHADER_attribute for details.
    /// </summary>
    public List<MojoShaderAttribute> Attributes { get; set; } =
        new();

    /// <summary>
    /// Elements of data that specify outputs this shader
    ///  writes to. See discussion on MOJOSHADER_attribute for details.
    /// </summary>
    public List<MojoShaderAttribute> Outputs { get; set; } =
        new();

    /// <summary>
    /// Elements of data that specify swizzles the shader will
    ///  apply to incoming attributes. This is a copy of what was passed to
    ///  MOJOSHADER_parseData().
    /// </summary>
    public List<MojoShaderSwizzle> Swizzles { get; set; } =
        new();

    /// <summary>
    /// Elements of data that specify high-level symbol data
    ///  for the shader. This will be parsed from the CTAB section
    ///  in bytecode, and will be a copy of what you provide to
    ///  MOJOSHADER_assemble(). This data is optional.
    /// </summary>
    public List<MojoShaderSymbol> Symbols { get; set; } =
        new();

    /// <summary>
    /// This can be NULL on error or if no preshader was available.
    /// </summary>
    public MojoShaderPreshader? Preshader { get; set; }

    /// <summary>
    /// Returns the output as a UTF8 string.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => 
        Encoding.UTF8.GetString(Output.AsSpan());
}