namespace MojoShaderDotNet.Profiles.Arb1.Nv2;

public class MojoShaderArb1Nv2Profile : MojoShaderArb1Profile
{
    public override IMojoShaderContext CreateContext() => 
        new MojoShaderArb1Nv2Context();
}