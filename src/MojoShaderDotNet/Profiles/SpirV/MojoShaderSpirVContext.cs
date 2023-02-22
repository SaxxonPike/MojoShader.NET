namespace MojoShaderDotNet.Profiles.SpirV;

public class MojoShaderSpirVContext : MojoShaderContext
{
    public Stack<int> BranchLabelsPatchStack { get; set; } = new();
    public MojoShaderSpirVContext? Spirv { get; set; }
    public virtual bool ProfileSupportsGlSpirV => false;
}