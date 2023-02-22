using MojoShaderDotNet.Types;

namespace MojoShaderDotNet.Profiles.Arb1;

/// <summary>
/// [mojoshader_profile_arb1.c]
/// </summary>
public class MojoShaderArb1Profile : MojoShaderProfile
{
    public override string Name => MojoShaderProfiles.Arb1;

    public override IMojoShaderContext CreateContext() =>
        new MojoShaderArb1Context();
}