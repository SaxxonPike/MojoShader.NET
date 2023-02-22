using MojoShaderDotNet.Types;

namespace MojoShaderDotNet.Profiles.Metal;

/// <summary>
/// [mojoshader_profile_metal.c]
/// </summary>
public class MojoShaderMetalProfile : MojoShaderProfile
{
    public override string Name => MojoShaderProfiles.Metal;

    public override IMojoShaderContext CreateContext() => 
        new MojoShaderMetalContext();
}