using System.Buffers.Binary;
using MojoShaderDotNet.Types;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CommentTypo

namespace MojoShaderDotNet.Profiles;

/// <summary>
/// [mojoshader_profile_common.c]
/// </summary>
public abstract class MojoShaderProfile : IMojoShaderProfile
{
    public abstract string Name { get; }

    public abstract IMojoShaderContext CreateContext();

    public virtual void EmitFunction(IMojoShaderContext ctx, MojoShaderOpcode op)
    {
    }

    public virtual void EmitStart(IMojoShaderContext ctx, string profileStr)
    {
    }

    public virtual void EmitEnd(IMojoShaderContext ctx)
    {
    }

    public virtual void EmitPhase(IMojoShaderContext ctx)
    {
    }

    public virtual void EmitFinalize(IMojoShaderContext ctx)
    {
    }

    public virtual void EmitGlobal(IMojoShaderContext ctx, MojoShaderRegisterType regType, int regNum)
    {
    }

    public virtual void EmitArray(IMojoShaderContext ctx, MojoShaderVariable var)
    {
    }

    public virtual void EmitConstArray(IMojoShaderContext ctx, IList<MojoShaderConstant> constList, int @base, int size)
    {
    }

    public virtual void EmitUniform(IMojoShaderContext ctx, MojoShaderRegisterType regType, int regNum,
        MojoShaderVariable var)
    {
    }

    public virtual void EmitSampler(IMojoShaderContext ctx, int stage, MojoShaderTextureType tType, bool texBem)
    {
    }

    public virtual void EmitAttribute(IMojoShaderContext ctx, MojoShaderRegisterType regType, int regNum,
        MojoShaderUsage usage, int index, int wMask, int flags)
    {
    }

    public virtual string? GetVarName(IMojoShaderContext ctx, MojoShaderRegisterType regType, int regNum) => null;

    public virtual string? GetConstArrayVarName(IMojoShaderContext ctx, int @base, int size) => null;

    public IMojoShaderContext? BuildContext(
        string mainFn,
        Stream input,
        int inputLength,
        IEnumerable<MojoShaderSwizzle> swizzles,
        IEnumerable<MojoShaderSamplerMap> samplerMaps)
    {
        var ctx = CreateContext(input, inputLength);
        if (ctx == null)
            return null;
        ctx.Output = new StreamWriter(new MemoryStream());
        ctx.Preflight = new StreamWriter(new MemoryStream());
        ctx.Globals = new StreamWriter(new MemoryStream());
        ctx.Inputs = new StreamWriter(new MemoryStream());
        ctx.Outputs = new StreamWriter(new MemoryStream());
        ctx.Helpers = new StreamWriter(new MemoryStream());
        ctx.Subroutines = new StreamWriter(new MemoryStream());
        ctx.MainLineIntro = new StreamWriter(new MemoryStream());
        ctx.MainLineArguments = new StreamWriter(new MemoryStream());
        ctx.MainLineTop = new StreamWriter(new MemoryStream());
        ctx.MainLine = new StreamWriter(new MemoryStream());
        ctx.Postflight = new StreamWriter(new MemoryStream());
        ctx.Ignore = new StreamWriter(new MemoryStream());
        ctx.Profile = this;
        ctx.MainFn = mainFn;
        ctx.KnowShaderSize = inputLength > 0;
        ctx.ShaderSize = inputLength > 0 ? inputLength / 4 : 0;
        ctx.Swizzles.AddRange(swizzles);
        ctx.SamplerMap.AddRange(samplerMaps);
        ctx.EndLine = Environment.NewLine;
        ctx.LastAddressRegComponent = -1;
        ctx.ErrorPosition = MojoShaderPosition.Before;
        ctx.TexM3X2PadDst0 = -1;
        ctx.TexM3X2PadSrc0 = -1;
        ctx.TexM3X3PadDst0 = -1;
        ctx.TexM3X3PadSrc0 = -1;
        ctx.TexM3X3PadDst1 = -1;
        ctx.TexM3X3PadSrc1 = -1;
        ctx.Instructions = GetInstructionTable();
        ctx.SetOutput(MojoShaderProfileOutput.MainLine);
        return ctx;
    }

    public IMojoShaderContext? CreateContext(
        Stream input,
        int inputLength)
    {
        // Do we know the size?
        MemoryStream? mem = null;
        Span<byte> buf;
        if (inputLength < 1)
        {
            // No; load to the end of the stream.
            mem = new MemoryStream();
            input.CopyTo(mem);
            mem.Flush();
            buf = mem.GetBuffer();
            inputLength = buf.Length;
        }
        else
        {
            // Yes; load the exact size.
            buf = new byte[inputLength];
            input.ReadExactly(buf);
        }

        // Initialize the context.
        if (CreateContext() is not { } ctx)
            return null;

        // Create the buffer and ensure endianness.
        var tokens = new int[inputLength / 4];
        for (int i = 0, j = 0; i < inputLength; i += 4, j++)
            tokens[j] = BinaryPrimitives.ReadInt32LittleEndian(buf[i..]);

        // Clean up.
        mem?.Dispose();

        // Populate the context.
        ctx.OrigTokens = tokens;
        return ctx;
    }

    /// <summary>
    /// [get_D3D_register_string; mojoshader_profile_common.c]
    /// </summary>
    public (string name, string number)? GetD3DRegisterString(
        IMojoShaderContext ctx,
        MojoShaderRegisterType regType,
        int regNum)
    {
        string retVal;
        var hasNumber = true;

        switch (regType)
        {
            case MojoShaderRegisterType.Temp:
                retVal = "r";
                break;
            case MojoShaderRegisterType.Input:
                retVal = "v";
                break;
            case MojoShaderRegisterType.Const:
                retVal = "c";
                break;
            case MojoShaderRegisterType.Address:
                retVal = ctx.ShaderIsVertex() ? "a" : "t";
                break;
            case MojoShaderRegisterType.RastOut:
                switch ((MojoShaderRastOutType)regNum)
                {
                    case MojoShaderRastOutType.Position:
                        retVal = "oPos";
                        break;
                    case MojoShaderRastOutType.Fog:
                        retVal = "oFog";
                        break;
                    case MojoShaderRastOutType.PointSize:
                        retVal = "oPts";
                        break;
                    default:
                        ctx.Fail("unknown rastout type");
                        retVal = "???";
                        break;
                }

                hasNumber = false;
                break;
            case MojoShaderRegisterType.AttrOut:
                retVal = "oD";
                break;
            case MojoShaderRegisterType.Output:
                retVal = ctx.ShaderIsVertex() && ctx.ShaderVersionAtLeast(3, 0)
                    ? "o"
                    : "oT";
                break;
            case MojoShaderRegisterType.ConstInt:
                retVal = "i";
                break;
            case MojoShaderRegisterType.ColorOut:
                retVal = "oC";
                break;
            case MojoShaderRegisterType.DepthOut:
                retVal = "oDepth";
                hasNumber = false;
                break;
            case MojoShaderRegisterType.Sampler:
                retVal = "s";
                break;
            case MojoShaderRegisterType.ConstBool:
                retVal = "b";
                break;
            case MojoShaderRegisterType.Loop:
                retVal = "aL";
                hasNumber = false;
                break;
            case MojoShaderRegisterType.MiscType:
                switch ((MojoShaderMiscTypeType)regNum)
                {
                    case MojoShaderMiscTypeType.Position:
                        retVal = "vPos";
                        break;
                    case MojoShaderMiscTypeType.Face:
                        retVal = "vFace";
                        break;
                    default:
                        ctx.Fail("unknown misc type");
                        retVal = "???";
                        hasNumber = false;
                        break;
                }

                break;
            case MojoShaderRegisterType.Label:
                retVal = "l";
                break;
            case MojoShaderRegisterType.Predicate:
                retVal = "p";
                break;
            default:
                ctx.Fail("unknown register type");
                retVal = "???";
                hasNumber = false;
                break;
        }

        return (retVal, hasNumber ? $"{regNum}" : string.Empty);
    }

    /// <summary>
    /// !!! FIXME: These should stay in the mojoshader_profile_d3d file
    /// but ARB1 relies on them, so we have to move them here.
    /// If/when we kill off ARB1, we can move these back.
    /// [get_D3D_varname; mojoshader_profile_common.c]
    /// </summary>
    /// <returns></returns>
    public string? GetD3DVarName(
        IMojoShaderContext ctx,
        MojoShaderRegisterType rt,
        int regNum)
    {
        var regTypeStr = GetD3DRegisterString(ctx, rt, regNum);
        return regTypeStr == null
            ? null
            : $"{regTypeStr.Value.name}{regTypeStr.Value.number}";
    }

    /// <summary>
    /// Bit of a nasty hack, but somewhat similar to how the original runs.
    /// </summary>
    public List<MojoShaderInstruction> GetInstructionTable()
    {
        MojoShaderInstruction GetInstruction(
            MojoShaderOpcode op,
            string opStr,
            int slots,
            MojoShaderInstructionArgs args,
            MojoShaderShaderType type,
            int writeMask) =>
            new()
            {
                Opcode = op,
                OpcodeString = opStr,
                Args = args,
                Slots = slots,
                ShaderTypes = type,
                WriteMask = writeMask
            };

        // These have to be in the right order! Arrays are indexed by the value
        //  of the instruction token.
        return new List<MojoShaderInstruction>
        {
            GetInstruction(MojoShaderOpcode.Nop, "NOP", 1,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Mov, "MOV", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Add, "ADD", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Sub, "SUB", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Mad, "MAD", 1,
                MojoShaderInstructionArgs.Dsss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Mul, "MUL", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Rcp, "RCP", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Rsq, "RSQ", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Dp3, "DP3", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Dp4, "DP4", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Min, "MIN", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Max, "MAX", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Slt, "SLT", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Sge, "SGE", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Exp, "EXP", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Log, "LOG", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Lit, "LIT", 3,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Dst, "DST", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Vertex, 0xF),
            GetInstruction(MojoShaderOpcode.Lrp, "LRP", 2,
                MojoShaderInstructionArgs.Dsss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Frc, "FRC", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.M4x4, "M4X4", 4,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.M4x3, "M4X3", 3,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0x7),
            GetInstruction(MojoShaderOpcode.M3x4, "M3X4", 4,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.M3x3, "M3X3", 3,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0x7),
            GetInstruction(MojoShaderOpcode.M3x2, "M3X2", 2,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0x3),
            GetInstruction(MojoShaderOpcode.Call, "CALL", 2,
                MojoShaderInstructionArgs.S, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.CallNz, "CALLNZ", 3,
                MojoShaderInstructionArgs.Ss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Loop, "LOOP", 3,
                MojoShaderInstructionArgs.Ss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Ret, "RET", 1,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.EndLoop, "ENDLOOP", 2,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Label, "LABEL", 0,
                MojoShaderInstructionArgs.S, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Dcl, "DCL", 0,
                MojoShaderInstructionArgs.Dcl, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Pow, "POW", 3,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Crs, "CRS", 2,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Sgn, "SGN", 3,
                MojoShaderInstructionArgs.Dsss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Abs, "ABS", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Nrm, "NRM", 3,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.SinCos, "SINCOS", 8,
                MojoShaderInstructionArgs.SinCos, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Rep, "REP", 3,
                MojoShaderInstructionArgs.S, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.EndRep, "ENDREP", 2,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.If, "IF", 3,
                MojoShaderInstructionArgs.S, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Ifc, "IF", 3,
                MojoShaderInstructionArgs.Ss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Else, "ELSE", 1,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Any, 0xF), // !!! FIXME: state!
            GetInstruction(MojoShaderOpcode.EndIf, "ENDIF", 1,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Any, 0xF), // !!! FIXME: state!
            GetInstruction(MojoShaderOpcode.Break, "BREAK", 1,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.BreakC, "BREAK", 3,
                MojoShaderInstructionArgs.Ss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.MovA, "MOVA", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Vertex, 0xF),
            GetInstruction(MojoShaderOpcode.DefB, "DEFB", 0,
                MojoShaderInstructionArgs.DefB, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.DefI, "DEFI", 0,
                MojoShaderInstructionArgs.DefI, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.TexCrd, "TEXCRD", 1,
                MojoShaderInstructionArgs.TexCrd, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexKill, "TEXKILL", 2,
                MojoShaderInstructionArgs.D, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexLd, "TEXLD", 1,
                MojoShaderInstructionArgs.TexLd, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexBem, "TEXBEM", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexBeml, "TEXBEML", 2,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexReg2Ar, "TEXREG2AR", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexReg2Gb, "TEXREG2GB", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexM3x2Pad, "TEXM3X2PAD", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexM3x2Tex, "TEXM3X2TEX", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexM3x3Pad, "TEXM3X3PAD", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexM3x3Tex, "TEXM3X3TEX", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.Reserved, string.Empty, 0,
                MojoShaderInstructionArgs.Null, MojoShaderShaderType.Unknown, 0xF),
            GetInstruction(MojoShaderOpcode.TexM3x3Spec, "TEXM3X3SPEC", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexM3x3Vspec, "TEXM3X3VSPEC", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.ExpP, "EXPP", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.LogP, "LOGP", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.Cnd, "CND", 1,
                MojoShaderInstructionArgs.Dsss, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.Def, "DEF", 0,
                MojoShaderInstructionArgs.Def, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.TexReg2Rgb, "TEXREG2RGB", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexDp3Tex, "TEXDP3TEX", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexM3x2Depth, "TEXM3X2DEPTH", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexDp3, "TEXDP3", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexM3x3, "TEXM3X3", 1,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexDepth, "TEXDEPTH", 1,
                MojoShaderInstructionArgs.D, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.Cmp, "CMP", 1,
                MojoShaderInstructionArgs.Dsss, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.Bem, "BEM", 2,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.Dp2Add, "DP2ADD", 2,
                MojoShaderInstructionArgs.Dsss, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.Dsx, "DSX", 2,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.Dsy, "DSY", 2,
                MojoShaderInstructionArgs.Ds, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.TexLdd, "TEXLDD", 3,
                MojoShaderInstructionArgs.Dssss, MojoShaderShaderType.Pixel, 0xF),
            GetInstruction(MojoShaderOpcode.SetP, "SETP", 1,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.TexLdl, "TEXLDL", 2,
                MojoShaderInstructionArgs.Dss, MojoShaderShaderType.Any, 0xF),
            GetInstruction(MojoShaderOpcode.BreakP, "BREAKP", 3,
                MojoShaderInstructionArgs.S, MojoShaderShaderType.Any, 0xF)
        };
    }
}