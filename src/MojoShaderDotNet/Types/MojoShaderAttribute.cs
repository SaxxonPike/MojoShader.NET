namespace MojoShaderDotNet.Types;

/// <summary>
/// These are the attributes to be set for a shader. "Attributes" are what
///  Direct3D calls "Vertex Declarations Usages" ...
///  IDirect3DDevice::CreateVertexDeclaration() would need this data, for
///  example. Each attribute is associated with an array of data that uses one
///  element per-vertex. So if usage==MOJOSHADER_USAGE_COLOR and index==1, that
///  means we'd expect a secondary color array to be bound to this shader
///  before drawing.
/// [MOJOSHADER_attribute; mojoshader.h]
/// </summary>
public class MojoShaderAttribute
{
    public MojoShaderUsage Usage { get; set; }
    public int Index { get; set; }

    /// <summary>
    /// A profile-specific variable name; it may be NULL if it isn't
    ///  applicable to the requested profile.
    /// </summary>
    public string? Name { get; set; }
}