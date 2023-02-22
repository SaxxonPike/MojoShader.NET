using MojoShaderDotNet.Types;

namespace MojoShaderDotNet.Profiles.D3D;

/// <summary>
/// [mojoshader_profile_d3d.c]
/// </summary>
public class MojoShaderD3dProfile : MojoShaderProfile
{
    public override string Name => MojoShaderProfiles.D3D;

    public override IMojoShaderContext CreateContext() => 
        new MojoShaderContext();
}