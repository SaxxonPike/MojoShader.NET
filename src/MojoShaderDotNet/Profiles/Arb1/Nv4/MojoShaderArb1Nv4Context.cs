namespace MojoShaderDotNet.Profiles.Arb1.Nv4;

public class MojoShaderArb1Nv4Context : MojoShaderArb1Context
{
    public override bool ProfileSupportsNv2 => true;
    public override bool ProfileSupportsNv3 => true;
    public override bool ProfileSupportsNv4 => true;
}