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
    
    /// <summary>
    /// [get_ARB1_register_string; mojoshader_profile_arb1.c]
    /// </summary>
    public (string name, string number)? GetRegisterString(IMojoShaderContext ctx, MojoShaderRegisterType regType,
        int regNum) =>
        // turns out these are identical at the moment.
        GetD3DRegisterString(ctx, regType, regNum);
}