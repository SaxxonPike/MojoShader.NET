namespace MojoShaderDotNet.Profiles.Arb1.Nv3;

public class MojoShaderArb1Nv3Profile : MojoShaderArb1Profile
{
    public override IMojoShaderContext CreateContext() => 
        new MojoShaderArb1Nv3Context();
}