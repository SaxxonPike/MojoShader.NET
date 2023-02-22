using MojoShaderDotNet.Types;

namespace MojoShaderDotNet.Profiles.Hlsl;

/// <summary>
/// [mojoshader_profile_hlsl.c]
/// </summary>
public class MojoShaderHlslProfile : MojoShaderProfile
{
    public override string Name => MojoShaderProfiles.Hlsl;

    public override IMojoShaderContext CreateContext() => 
        new MojoShaderHlslContext();
}