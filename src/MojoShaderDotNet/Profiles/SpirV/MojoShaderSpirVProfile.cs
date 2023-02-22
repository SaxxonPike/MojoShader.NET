using MojoShaderDotNet.Types;

namespace MojoShaderDotNet.Profiles.SpirV;

/// <summary>
/// [mojoshader_profile_spirv.c]
/// </summary>
public class MojoShaderSpirVProfile : MojoShaderProfile
{
    public override string Name => MojoShaderProfiles.SpirV;

    public override IMojoShaderContext CreateContext() => 
        new MojoShaderSpirVContext();
}