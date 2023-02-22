using MojoShaderDotNet.Types;

namespace MojoShaderDotNet;

/// <summary>
/// Contains global methods that make MojoShaderDotNet easier to use for consumers of the API.
/// </summary>
public static class MojoShaderExtensions
{
    public static MojoShaderParseData? Parse(this IMojoShader mojoShader, string profile, Stream input) => 
        mojoShader.Parse(profile, "", input, null);
}