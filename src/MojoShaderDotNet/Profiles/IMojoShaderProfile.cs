using MojoShaderDotNet.Types;

namespace MojoShaderDotNet.Profiles;

/// <summary>
/// [mojoshader_internal.h]
/// </summary>
public interface IMojoShaderProfile
{
    string Name { get; }

    void EmitFunction(IMojoShaderContext ctx, MojoShaderOpcode op);

    void EmitStart(IMojoShaderContext ctx,
        string profileStr);

    void EmitEnd(IMojoShaderContext ctx);

    void EmitPhase(IMojoShaderContext ctx);

    void EmitFinalize(IMojoShaderContext ctx);

    void EmitGlobal(IMojoShaderContext ctx,
        MojoShaderRegisterType regType,
        int regNum);

    void EmitArray(IMojoShaderContext ctx,
        MojoShaderVariable @var);

    void EmitConstArray(IMojoShaderContext ctx,
        IList<MojoShaderConstant> constList,
        int @base,
        int size);

    void EmitUniform(IMojoShaderContext ctx,
        MojoShaderRegisterType regType,
        int regNum,
        MojoShaderVariable @var);

    void EmitSampler(IMojoShaderContext ctx,
        int stage,
        MojoShaderTextureType tType,
        bool texBem);

    void EmitAttribute(IMojoShaderContext ctx,
        MojoShaderRegisterType regType,
        int regNum,
        MojoShaderUsage usage,
        int index,
        int wMask,
        int flags);

    string? GetConstArrayVarName(IMojoShaderContext ctx, int @base, int size);

    string? GetVarName(IMojoShaderContext ctx, MojoShaderRegisterType regType, int regNum);

    (string name, string number)? GetD3DRegisterString(
        IMojoShaderContext ctx, MojoShaderRegisterType regType, int regNum);

    string? GetD3DVarName(IMojoShaderContext ctx, MojoShaderRegisterType rt, int regNum);

    IMojoShaderContext? BuildContext(string mainFn, Stream input, int inputLength, 
        IEnumerable<MojoShaderSwizzle> swizzles, IEnumerable<MojoShaderSamplerMap> samplerMaps);
}