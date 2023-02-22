namespace MojoShaderDotNet.Profiles.Arb1;

public class MojoShaderArb1Context : MojoShaderContext
{
    public int Arb1WrotePosition { get; set; }
    public virtual bool ProfileSupportsNv2 => false;
    public virtual bool ProfileSupportsNv3 => false;
    public virtual bool ProfileSupportsNv4 => false;
}