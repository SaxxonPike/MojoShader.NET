using MojoShaderDotNet.Types;

namespace MojoShaderDotNet.Profiles.Glsl.V400;

public class MojoShaderGlsl400Profile : MojoShaderGlslProfile
{
    public override string InputName => "in";

    public override string OutputName => "out";

    protected override bool EmitStartInternal(IMojoShaderContext ctx)
    {
        if (ctx is not MojoShaderGlsl400Context validCtx) 
            return false;

        validCtx.PushOutput(MojoShaderProfileOutput.Preflight);
        validCtx.OutputLine("#version 400");
        validCtx.PopOutput();

        // GLSL 4.0+ doesn't define all that we will need for us unless we use the compatibility profile. However,
        // for some reason, the compatibility profile doesn't seem to work on all shader compilers. Therefore, we
        // will just declare everything we could possibly need, and let the driver figure it out.

        validCtx.PushOutput(MojoShaderProfileOutput.Inputs);
        validCtx.PopOutput();

        return true;
    }
}