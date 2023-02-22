using System.Buffers.Binary;
using MojoShaderDotNet.Types;

namespace MojoShaderDotNet.Profiles.Bytecode;

/// <summary>
/// [mojoshader_profile_bytecode.c]
/// </summary>
public class MojoShaderBytecodeProfile : MojoShaderProfile
{
    public override string Name => 
        MojoShaderProfiles.ByteCode;

    public override IMojoShaderContext CreateContext() =>
        new MojoShaderContext();

    public override void EmitStart(IMojoShaderContext ctx, string profileStr) => 
        ctx.IgnoresCtab = true;

    public override void EmitFinalize(IMojoShaderContext ctx)
    {
        // just copy the whole token stream and make all other emitters no-ops.
        ctx.SetOutput(MojoShaderProfileOutput.MainLine);
        Span<byte> buffer = new byte[ctx.OrigTokens.Length * sizeof(int)];
        var idx = 0;
        foreach (var token in ctx.OrigTokens)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer[idx..], token);
            idx += 4;
        }

        ctx.Output.BaseStream.Write(buffer);
    }

    public override string? GetVarName(IMojoShaderContext ctx, MojoShaderRegisterType regType, int regNum) =>
        GetD3DRegisterString(ctx, regType, regNum) is { } regTypeStr
            ? $"{regTypeStr.name}{regTypeStr.number}"
            : null;

    public override string? GetConstArrayVarName(IMojoShaderContext ctx, int @base, int size) => 
        $"c_array_{@base}_{size}";
}