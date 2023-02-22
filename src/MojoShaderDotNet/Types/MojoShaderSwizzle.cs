using System.Text.Json.Serialization;

namespace MojoShaderDotNet.Types;

/// <summary>
/// Use this if you want to specify newly-parsed code to swizzle incoming
///  data. This can be useful if you know, at parse time, that a shader
///  will be processing data on COLOR0 that should be RGBA, but you'll
///  be passing it a vertex array full of ARGB instead.
/// [MOJOSHADER_swizzle; mojoshader.h]
/// </summary>
public class MojoShaderSwizzle
{
    public MojoShaderUsage Usage { get; set; }
    
    public int Index { get; set; }

    /// <summary>
    /// {0,1,2,3} == .xyzw, {2,2,2,2} == .zzzz
    /// </summary>
    [JsonIgnore]
    public ReadOnlySpan<byte> Swizzles => new[]
    {
        Value[0], Value[1], Value[2], Value[3]
    };
    
    public MojoShaderSwizzleValue Value { get; set; }

    public MojoShaderSwizzle Clone() => 
        (MojoShaderSwizzle)MemberwiseClone();
}