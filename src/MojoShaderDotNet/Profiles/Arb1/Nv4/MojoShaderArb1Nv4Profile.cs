namespace MojoShaderDotNet.Profiles.Arb1.Nv4;

public class MojoShaderArb1Nv4Profile : MojoShaderArb1Profile
{
    public override IMojoShaderContext CreateContext() => 
        new MojoShaderArb1Nv4Context();
}