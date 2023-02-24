namespace MojoShaderDotNet.Profiles.Glsl;

public class MojoShaderGlslContext : MojoShaderContext
{
    public bool GlslGeneratedLitHelper { get; set; }
    public bool GlslGeneratedTexLodSetup { get; set; }
    public bool GlslGeneratedTexM3x3SpecHelper { get; set; }
    public virtual bool ProfileSupportsGlsl120 => false;
    public virtual bool ProfileSupportsGlsl400 => false;
    public virtual bool ProfileSupportsGlslEs => false;
}