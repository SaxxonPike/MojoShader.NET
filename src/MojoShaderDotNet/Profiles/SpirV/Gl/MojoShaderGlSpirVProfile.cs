namespace MojoShaderDotNet.Profiles.SpirV.Gl;

public class MojoShaderGlSpirVProfile : MojoShaderSpirVProfile
{
    public override IMojoShaderContext CreateContext() => 
        new MojoShaderGlSpirVContext();
}