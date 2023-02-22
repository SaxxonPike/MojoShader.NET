using MojoShaderDotNet.Types;

namespace MojoShaderDotNet.Profiles.Glsl.V120;

public class MojoShaderGlsl120Profile : MojoShaderGlslProfile
{
    public override IMojoShaderContext CreateContext() => 
        new MojoShaderGlsl120Context();

    protected override bool EmitStartInternal(IMojoShaderContext ctx)
    {
        if (ctx is not MojoShaderGlsl120Context validCtx) 
            return false;

        validCtx.PushOutput(MojoShaderProfileOutput.Preflight);
        validCtx.OutputLine("#version 120");
        validCtx.PopOutput();

        return true;
    }
}