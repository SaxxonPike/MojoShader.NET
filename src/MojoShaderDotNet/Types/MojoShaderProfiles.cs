using MojoShaderDotNet.Profiles;
using MojoShaderDotNet.Profiles.Arb1;
using MojoShaderDotNet.Profiles.Arb1.Nv2;
using MojoShaderDotNet.Profiles.Arb1.Nv3;
using MojoShaderDotNet.Profiles.Arb1.Nv4;
using MojoShaderDotNet.Profiles.Bytecode;
using MojoShaderDotNet.Profiles.D3D;
using MojoShaderDotNet.Profiles.Glsl;
using MojoShaderDotNet.Profiles.Glsl.Es;
using MojoShaderDotNet.Profiles.Glsl.V120;
using MojoShaderDotNet.Profiles.Glsl.V400;
using MojoShaderDotNet.Profiles.Hlsl;
using MojoShaderDotNet.Profiles.Metal;
using MojoShaderDotNet.Profiles.SpirV;
using MojoShaderDotNet.Profiles.SpirV.Gl;

namespace MojoShaderDotNet.Types;

/// <summary>
/// Profile strings for output.
/// </summary>
public static class MojoShaderProfiles
{
    /// <summary>
    /// Profile string for Direct3D assembly language output.
    /// </summary>
    public const string D3D = "d3d";

    /// <summary>
    /// Profile string for passthrough of the original bytecode, unchanged.
    /// </summary>
    public const string ByteCode = "bytecode";

    /// <summary>
    /// Profile string for HLSL Shader Model 4 output.
    /// </summary>
    public const string Hlsl = "hlsl";

    /// <summary>
    /// Profile string for GLSL: OpenGL high-level shader language output.
    /// </summary>
    public const string Glsl = "glsl";

    /// <summary>
    /// Profile string for GLSL 1.20: minor improvements to base GLSL spec.
    /// </summary>
    public const string Glsl120 = "glsl120";

    /// <summary>
    /// Profile string for GLSL 4.00: major changes to base GLSL spec.
    /// </summary>
    public const string Glsl400 = "glsl400";

    /// <summary>
    /// Profile string for GLSL ES: minor changes to GLSL output for ES compliance.
    /// </summary>
    public const string GlslEs = "glsles";

    /// <summary>
    /// Profile string for OpenGL ARB 1.0 shaders: GL_ARB_(vertex|fragment)_program.
    /// </summary>
    public const string Arb1 = "arb1";

    /// <summary>
    /// Profile string for OpenGL ARB 1.0 shaders with Nvidia 2.0 extensions:
    /// GL_NV_vertex_program2_option and GL_NV_fragment_program2
    /// </summary>
    public const string Nv2 = "nv2";

    /// <summary>
    /// Profile string for OpenGL ARB 1.0 shaders with Nvidia 3.0 extensions:
    /// GL_NV_vertex_program3 and GL_NV_fragment_program2
    /// </summary>
    public const string Nv3 = "nv3";

    /// <summary>
    /// Profile string for OpenGL ARB 1.0 shaders with Nvidia 4.0 extensions:
    /// GL_NV_gpu_program4
    /// </summary>
    public const string Nv4 = "nv4";

    /// <summary>
    /// Profile string for Metal: Apple's low-level API's high-level shader language.
    /// </summary>
    public const string Metal = "metal";

    /// <summary>
    /// Profile string for SPIR-V binary output
    /// </summary>
    public const string SpirV = "spirv";

    /// <summary>
    /// Profile string for ARB_gl_spirv-friendly SPIR-V binary output
    /// </summary>
    public const string GlSpirV = "glspirv";

    public static IReadOnlyDictionary<string, Type> All { get; }
        = new Dictionary<string, Type>
        {
            { D3D, typeof(MojoShaderD3dProfile) },
            { ByteCode, typeof(MojoShaderBytecodeProfile) },
            { Hlsl, typeof(MojoShaderHlslProfile) },
            { Glsl, typeof(MojoShaderGlslProfile) },
            { Glsl120, typeof(MojoShaderGlsl120Profile) },
            { Glsl400, typeof(MojoShaderGlsl400Profile) },
            { GlslEs, typeof(MojoShaderGlslEsProfile) },
            { Arb1, typeof(MojoShaderArb1Profile) },
            { Nv2, typeof(MojoShaderArb1Nv2Profile) },
            { Nv3, typeof(MojoShaderArb1Nv3Profile) },
            { Nv4, typeof(MojoShaderArb1Nv4Profile) },
            { Metal, typeof(MojoShaderMetalProfile) },
            { SpirV, typeof(MojoShaderSpirVProfile) },
            { GlSpirV, typeof(MojoShaderGlSpirVProfile) }
        };

    /// <summary>
    /// [find_profile_id; mojoshader.c]
    /// </summary>
    public static string? Find(string profile) =>
        All
            .Where(kv => string.Equals(profile, kv.Key, StringComparison.InvariantCultureIgnoreCase))
            .Select(kv => kv.Key)
            .FirstOrDefault();
}