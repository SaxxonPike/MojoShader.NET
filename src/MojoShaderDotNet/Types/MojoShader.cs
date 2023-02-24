using MojoShaderDotNet.Profiles;

namespace MojoShaderDotNet.Types;

public class MojoShader : IMojoShader
{
    /// <summary>
    /// !!! FIXME:
    /// MSDN: "Shader validation will fail CreatePixelShader on any shader that
    ///  attempts to read from a temporary register that has not been written by a
    ///  previous instruction."  (true for ps_1_*, maybe others). Check this.
    /// </summary>
    public MojoShaderParseData? Parse(string profileStr, string mainFn, Stream stream, MojoShaderParseOptions? options)
    {
        MojoShaderParseData? result = null;
        var failed = false;

        // Try and find which profile class corresponds to the profile string.
        if (MojoShaderProfiles.Find(profileStr) is not { } mappedProfileString ||
            !MojoShaderProfiles.All.TryGetValue(mappedProfileString, out var profileType))
            return null;

        // Reflection magic to create the profile.
        if (Activator.CreateInstance(profileType) is not IMojoShaderProfile profile)
            return null;

        var swizzles = options?.Swiz.ToList() ?? new List<MojoShaderSwizzle>();
        var samplerMaps = options?.SMap.ToList() ?? new List<MojoShaderSamplerMap>();

        if (profile.BuildContext(mainFn, stream, (int)stream.Length, swizzles, samplerMaps) is not { } ctx)
            return null;

        ctx.Log = Log;

        if (ctx.IsFail)
            return result;

        if (string.IsNullOrEmpty(mainFn))
            mainFn = "main";

        // Version token always comes first.
        var rc = ctx.ParseVersionToken(profileStr);

        // drop out now if this definitely isn't bytecode. Saves lots of
        //  meaningless errors flooding through.
        if (rc < 0)
            return result;

        if (rc > ctx.TokensRemaining)
            ctx.Fail("Corrupted or truncated shader");

        ctx.AdjustTokenPosition(rc);

        // parse out the rest of the tokens after the version token...
        while (ctx.TokensRemaining > 0)
        {
            // reset for each token.
            if (ctx.IsFail)
            {
                failed = true;
                ctx.IsFail = false;
            }

            rc = ctx.ParseToken();
            if (rc > ctx.TokensRemaining)
            {
                ctx.Fail("Corrupted or truncated shader");
                break;
            }

            ctx.AdjustTokenPosition(rc);
        }

        ctx.ErrorPosition = MojoShaderPosition.After;

        // for ps_1_*, the output color is written to r0...throw an
        //  error if this register was never written. This isn't
        //  important for vertex shaders, or shader model 2+.
        if (ctx.ShaderIsPixel() && !ctx.ShaderVersionAtLeast(2, 0))
        {
            if (!ctx.RegisterWasWritten(MojoShaderRegisterType.Temp, 0))
                ctx.Fail("r0 (pixel shader 1.x color output) never written to");
        }

        if (!failed)
        {
            ctx.ProcessDefinitions();
            failed = ctx.IsFail;
        }

        if (!failed)
            profile.EmitFinalize(ctx);

        ctx.IsFail = failed;
        result = ctx.BuildParseData();
        return result;
    }

    public MojoShaderParseData Assemble(string fileName, string source, MojoShaderAssembleOptions options)
    {
        throw new NotImplementedException();
    }

    public MojoShaderCompileData Compile(string sourceProfile, string fileName, string source,
        MojoShaderCompileOptions options)
    {
        throw new NotImplementedException();
    }

    public void SpirvLinkAttributes(MojoShaderParseData vertex, MojoShaderParseData pixel)
    {
        throw new NotImplementedException();
    }

    public TextWriter? Log { get; set; }
}