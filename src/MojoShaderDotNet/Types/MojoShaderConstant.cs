using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace MojoShaderDotNet.Types;

/// <summary>
/// These are the constants defined in a shader. These are data values
///  hardcoded in a shader (with the DEF, DEFI, DEFB instructions), which
///  override your Uniforms. This data is largely for informational purposes,
///  since they are compiled in and can't be changed, like Uniforms can be.
/// These integers are register indexes. So if index==6 and
///  type==MOJOSHADER_UNIFORM_FLOAT, that means we'd expect a 4-float vector
///  to be specified for what would be register "c6" in D3D assembly language,
///  before drawing with the shader.
/// [MOJOSHADER_constant; mojoshader.h]
/// </summary>
public class MojoShaderConstant
{
    /// <summary>
    /// Uniform type.
    /// </summary>
    public MojoShaderUniformType Type { get; set; }
    
    /// <summary>
    /// Register number.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Value of the constant as a 32-bit int. For floats, use <see cref="ValueAsFloat"/>.
    /// </summary>
    public int[] Value { get; set; } = new int[4];

    /// <summary>
    /// if type==MOJOSHADER_UNIFORM_FLOAT
    /// </summary>
    [JsonIgnore]
    public Span<float> ValueAsFloat =>
        MemoryMarshal.Cast<int, float>(Value.AsSpan());

    public MojoShaderConstant Clone() =>
        new()
        {
            Type = Type,
            Index = Index,
            Value = Value.ToArray()
        };
}