using MojoShaderDotNet.Types;

namespace MojoShaderDotNet.Profiles.Glsl.Es;

public class MojoShaderGlslEsProfile : MojoShaderGlslProfile
{
    public override IMojoShaderContext CreateContext() => 
        new MojoShaderGlslEsContext();

    protected override bool EmitStartInternal(IMojoShaderContext ctx)
    {
        if (ctx is not MojoShaderGlslEsContext validCtx) 
            return false;

        validCtx.PushOutput(MojoShaderProfileOutput.Preflight);
        validCtx.OutputLine("#version 100");
        if (validCtx.ShaderIsVertex())
            validCtx.OutputLine("precision highp float;");
        else
            validCtx.OutputLine("precision mediump float;");
        validCtx.OutputLine("precision mediump int;");
        validCtx.PopOutput();

        return true;
    }
}