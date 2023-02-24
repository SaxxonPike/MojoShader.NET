namespace MojoShaderDotNet.Profiles.Glsl.V400;

public class MojoShaderGlsl400Context : MojoShaderGlslContext
{
    public override bool ProfileSupportsGlsl120 => true;
    public override bool ProfileSupportsGlsl400 => true;
}