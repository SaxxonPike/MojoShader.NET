using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using MojoShaderDotNet.Profiles.SpirV;
using MojoShaderDotNet.Types;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CommentTypo

namespace MojoShaderDotNet.Profiles;

/// <summary>
/// [Context; mojoshader_profile.h]
/// </summary>
public class MojoShaderContext : IMojoShaderContext
{
    public bool IsFail { get; set; }
    public int CurrentPosition { get; set; }
    public MojoShaderPosition ErrorPosition { get; set; }

    [JsonIgnore] public Span<int> Tokens => OrigTokens.AsSpan(CurrentPosition..);

    public int[] OrigTokens { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Tokens remaining; represents "tokencount" field.
    /// </summary>
    public int TokensRemaining => (KnowShaderSize ? ShaderSize : OrigTokens.Length) - CurrentPosition;

    public bool KnowShaderSize { get; set; }
    public List<MojoShaderSwizzle> Swizzles { get; set; } = new();
    public List<MojoShaderSamplerMap> SamplerMap { get; set; } = new();
    public StreamWriter Output { get; set; }
    public StreamWriter Preflight { get; set; }
    public StreamWriter Globals { get; set; }
    public StreamWriter Inputs { get; set; }
    public StreamWriter Outputs { get; set; }
    public StreamWriter Helpers { get; set; }
    public StreamWriter Subroutines { get; set; }
    public StreamWriter MainLineIntro { get; set; }
    public StreamWriter MainLineArguments { get; set; }
    public StreamWriter MainLineTop { get; set; }
    public StreamWriter MainLine { get; set; }
    public StreamWriter Postflight { get; set; }
    public StreamWriter Ignore { get; set; }
    public Stack<StreamWriter> OutputStack { get; set; } = new();
    public Stack<int> IndentStack { get; set; } = new();
    public int Indent { get; set; }
    public string ShaderTypeStr { get; set; }
    public string EndLine { get; set; }
    public string MainFn { get; set; }
    public string ProfileId { get; set; }
    public IMojoShaderProfile Profile { get; set; }
    public MojoShaderShaderType ShaderType { get; set; }
    public int MajorVer { get; set; }
    public int MinorVer { get; set; }
    public MojoShaderDestArgInfo? DestArg { get; set; }
    public MojoShaderSourceArgInfo?[] SourceArgs { get; set; } = new MojoShaderSourceArgInfo[4];
    public MojoShaderSourceArgInfo? PredicateArg { get; set; }
    public int[] Dwords { get; set; } = new int[4];
    public int VersionToken { get; set; }
    public int InstructionCount { get; set; }
    public int InstructionControls { get; set; }
    public MojoShaderOpcode CurrentOpcode { get; set; }
    public MojoShaderOpcode PreviousOpcode { get; set; }
    public bool CoIssue { get; set; }
    public int Loops { get; set; }
    public int Reps { get; set; }
    public int MaxReps { get; set; }
    public int Cmps { get; set; }
    public int ScratchRegisters { get; set; }
    public int MaxScratchRegisters { get; set; }
    public Stack<int> BranchLabels { get; set; } = new();
    public int AssignedBranchLabels { get; set; }
    public int AssignedVertexAttributes { get; set; }
    public int LastAddressRegComponent { get; set; }
    public List<MojoShaderRegister> UsedRegisters { get; set; } = new();
    public List<MojoShaderRegister> DefinedRegisters { get; set; } = new();
    public List<MojoShaderError> Errors { get; set; } = new();
    public List<MojoShaderConstant> Constants { get; set; } = new();
    public int UniformFloat4Count { get; set; }
    public int UniformInt4Count { get; set; }
    public int UniformBoolCount { get; set; }
    public List<MojoShaderRegister> Uniforms { get; set; } = new();
    public List<MojoShaderRegister> Attributes { get; set; } = new();
    public List<MojoShaderRegister> Samplers { get; set; } = new();
    public List<MojoShaderVariable> Variables { get; set; } = new();
    public bool CentroidAllowed { get; set; }
    public MojoShaderCtabData? Ctab { get; set; }
    public bool HaveRelativeInputRegisters { get; set; }
    public bool HaveMultiColorOutputs { get; set; }
    public bool DeterminedConstantsArrays { get; set; }
    public bool Predicated { get; set; }
    public bool UsesPointSize { get; set; }
    public bool UsesFog { get; set; }
    public bool NeedsMaxFloat { get; set; }
    public bool HavePreshader { get; set; }
    public bool IgnoresCtab { get; set; }
    public bool ResetTexMpad { get; set; }
    public int TexM3X2PadDst0 { get; set; }
    public int TexM3X2PadSrc0 { get; set; }
    public int TexM3X3PadDst0 { get; set; }
    public int TexM3X3PadSrc0 { get; set; }
    public int TexM3X3PadDst1 { get; set; }
    public int TexM3X3PadSrc1 { get; set; }

    public MojoShaderPreshader? Preshader { get; set; }
    public List<MojoShaderInstruction> Instructions { get; set; } = new();
    public int ShaderSize { get; set; }
    public TextWriter? Log { get; set; }

    public bool FlipRenderTargetOption { get; set; }
    public bool DepthClippingOption { get; set; }

    /// <summary>
    /// !!! FIXME: this code is sort of hard to follow:
    ///  "var->used" only applies to arrays (at the moment, at least,
    ///  but this might be buggy at a later time?), and this code
    ///  relies on that.
    /// "variables" means "things we found in a CTAB" but it's not
    ///  all registers, etc.
    /// "const_array" means an array for d3d "const" registers (c0, c1,
    ///  etc), but not a constant array, although they _can_ be.
    /// It's just a mess.  :/
    /// [build_uniforms; mojoshader.c]
    /// </summary>
    public List<MojoShaderUniform> BuildUniforms()
    {
        var variables = Variables
            .Where(x => x.Used)
            .Select(var =>
            {
                var name = Profile.GetConstArrayVarName(this, var.Index, var.Count);
                if (name != null)
                {
                    return new MojoShaderUniform
                    {
                        Type = MojoShaderUniformType.Float,
                        Index = var.Index,
                        ArrayCount = var.Count,
                        Constant = var.Constants.Any(),
                        Name = name
                    };
                }

                return null;
            });

        var uniforms = Uniforms
            .Select(item =>
            {
                var type = MojoShaderUniformType.Float;
                var skip = false;
                var index = item.RegNum;

                switch (item.RegType)
                {
                    case MojoShaderRegisterType.Const:
                        skip = item.Array.Any();
                        type = MojoShaderUniformType.Float;
                        break;

                    case MojoShaderRegisterType.ConstInt:
                        type = MojoShaderUniformType.Int;
                        break;

                    case MojoShaderRegisterType.ConstBool:
                        type = MojoShaderUniformType.Bool;
                        break;

                    default:
                        Fail("unknown uniform datatype");
                        break;
                }

                if (!skip)
                {
                    return new MojoShaderUniform
                    {
                        Type = type,
                        Index = index,
                        ArrayCount = 0,
                        Name = AllocVarName(item)
                    };
                }

                return null;
            });

        return new List<MojoShaderUniform>(variables
            .Concat(uniforms)
            .Where(x => x != null)
            .Select(x => x!));
    }

    /// <summary>
    /// [build_constants; mojoshader.c]
    /// </summary>
    public List<MojoShaderConstant> BuildConstants() =>
        new(Constants.Select(x => x.Clone()));

    /// <summary>
    /// [build_samplers; mojoshader.c]
    /// </summary>
    public List<MojoShaderSampler> BuildSamplers() =>
        new(Samplers.Select(item => new MojoShaderSampler
        {
            Type = ((MojoShaderTextureType)item.Index).CvtD3dToMojoSamplerType(),
            Index = item.RegNum,
            Name = AllocVarName(item),
            TexBem = item.Misc != 0 ? 1 : 0
        }));

    /// <summary>
    /// [build_attributes; mojoshader.c]
    /// </summary>
    public List<MojoShaderAttribute> BuildAttributes() =>
        new(Attributes
            .Where(item => item.RegType is not (MojoShaderRegisterType.RastOut or MojoShaderRegisterType.AttrOut
                or MojoShaderRegisterType.TexCrdOut or MojoShaderRegisterType.ColorOut
                or MojoShaderRegisterType.DepthOut))
            .Select(item => new MojoShaderAttribute
            {
                Usage = item.Usage,
                Index = item.Index,
                Name = AllocVarName(item)
            }));

    /// <summary>
    /// [build_outputs; mojoshader.c]
    /// </summary>
    public List<MojoShaderAttribute> BuildOutputs() =>
        new(Attributes
            .Where(item => item.RegType is MojoShaderRegisterType.RastOut or MojoShaderRegisterType.AttrOut
                or MojoShaderRegisterType.TexCrdOut or MojoShaderRegisterType.ColorOut
                or MojoShaderRegisterType.DepthOut)
            .Select(item => new MojoShaderAttribute
            {
                Usage = item.Usage,
                Index = item.Index,
                Name = AllocVarName(item)
            }));

    /// <summary>
    /// [build_parsedata; mojoshader.c]
    /// </summary>
    public MojoShaderParseData BuildParseData()
    {
        var output = Array.Empty<byte>();
        var constants = new List<MojoShaderConstant>();
        var uniforms = new List<MojoShaderUniform>();
        var attributes = new List<MojoShaderAttribute>();
        var outputs = new List<MojoShaderAttribute>();
        var samplers = new List<MojoShaderSampler>();
        var swizzles = new List<MojoShaderSwizzle>();
        var retval = new MojoShaderParseData();

        if (!IsFail)
            output = Encoding.UTF8.GetBytes(BuildOutput());

        if (!IsFail)
            constants = BuildConstants();

        if (!IsFail)
            uniforms = BuildUniforms();

        if (!IsFail)
            attributes = BuildAttributes();

        if (!IsFail)
            outputs = BuildOutputs();

        if (!IsFail)
            samplers = BuildSamplers();

        List<MojoShaderError> errors = new(Errors.Select(x => x.Clone()));

        if (!IsFail)
            if (Swizzles.Count > 0)
                swizzles = new List<MojoShaderSwizzle>(Swizzles.Select(x => x.Clone()));

        // check again, in case build_output, etc, ran out of memory.
        if (!IsFail)
        {
            retval.Profile = Profile.Name;
            retval.Output = output;
            retval.InstructionCount = InstructionCount;
            retval.ShaderType = ShaderType;
            retval.MajorVer = MajorVer;
            retval.MinorVer = MinorVer;
            retval.Uniforms.AddRange(uniforms);
            retval.Constants.AddRange(constants);
            retval.Samplers.AddRange(samplers);
            retval.Attributes.AddRange(attributes);
            retval.Outputs.AddRange(outputs);
            retval.Swizzles.AddRange(swizzles);
            retval.Symbols.AddRange(Ctab?.Symbols ?? Enumerable.Empty<MojoShaderSymbol>());
            retval.Preshader = Preshader;
            retval.MainFn = MainFn;

            // [Saxxon] original uses string comparison here
            if (this is MojoShaderSpirVContext)
            {
                // [Saxxon] TODO
                throw new NotImplementedException();
            }
        }

        retval.Errors.AddRange(errors);
        return retval;
    }

    /// <summary>
    /// [build_output; mojoshader.c]
    /// </summary>
    public virtual string BuildOutput()
    {
        void Append(Stream s, StreamWriter sw)
        {
            sw.Flush();
            sw.BaseStream.Position = 0;
            sw.BaseStream.CopyTo(s);
        }

        var sb = new MemoryStream();
        Append(sb, Preflight);
        Append(sb, Globals);
        Append(sb, Inputs);
        Append(sb, Outputs);
        Append(sb, Helpers);
        Append(sb, Subroutines);
        Append(sb, MainLineIntro);
        Append(sb, MainLineArguments);
        Append(sb, MainLineTop);
        Append(sb, MainLine);
        Append(sb, Postflight);
        sb.Flush();
        return Encoding.UTF8.GetString(sb.GetBuffer().AsSpan(..(int)sb.Length));
    }

    /// <summary>
    /// [ver_ui32; mojoshader_profile_common.c]
    /// </summary>
    public int GetVerUi32(
        int major,
        int minor) =>
        (major << 16) | (minor == 0xFF ? 1 : minor);

    /// <summary>
    /// [shader_version_supported; mojoshader_profile_common.c]
    /// </summary>
    public bool ShaderVersionSupported(
        int maj,
        int min) =>
        GetVerUi32(maj, min) <=
        GetVerUi32(MojoShaderConstants.MaxShaderMajor, MojoShaderConstants.MaxShaderMinor);

    /// <summary>
    /// [shader_version_atleast; mojoshader_profile_common.c]
    /// </summary>
    public bool ShaderVersionAtLeast(
        int maj,
        int min) =>
        GetVerUi32(MajorVer, MinorVer) >= GetVerUi32(maj, min);

    /// <summary>
    /// [shader_version_exactly; mojoshader_profile_common.c]
    /// </summary>
    public bool ShaderVersionExactly(
        int maj,
        int min) =>
        MajorVer == maj && MinorVer == min;

    /// <summary>
    /// [shader_is_pixel; mojoshader_profile_common.c]
    /// </summary>
    public bool ShaderIsPixel() =>
        ShaderType == MojoShaderShaderType.Pixel;

    /// <summary>
    /// [shader_is_vertex; mojoshader_profile_common.c]
    /// </summary>
    public bool ShaderIsVertex() =>
        ShaderType == MojoShaderShaderType.Vertex;

    /// <summary>
    /// [failf; mojoshader_profile_common.c]
    /// </summary>
    public void Fail(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
        string format,
        params object?[] items)
    {
        IsFail = true;
        Errors.Add(new MojoShaderError
        {
            Error = string.Format(format, items),
            ErrorPosition = (int)ErrorPosition
        });
    }

    /// <summary>
    /// [fail; mojoshader_profile_common.c]
    /// </summary>
    public void Fail(string reason) =>
        Fail("{0}", reason);

    /// <summary>
    /// [add_sampler; mojoshader.c]
    /// </summary>
    public void AddSampler(
        int regNum,
        MojoShaderTextureType tType,
        int texBem)
    {
        // !!! FIXME: make sure it doesn't exist?
        // !!! FIXME:  (ps_1_1 assume we can add it multiple times...)
        var item = RegListInsert(Samplers, MojoShaderRegisterType.Sampler, regNum);

        if (SamplerMap.FirstOrDefault(x => x.Index == regNum) is { } entry)
            tType = entry.Type.CvtMojoToD3DSamplerType();

        item.Index = (int)tType;
        item.Misc |= texBem;
    }

    /// <summary>
    /// [parse_destination_token; mojoshader.c]
    /// </summary>
    public (MojoShaderDestArgInfo? Info, int Size) ParseDestinationToken()
    {
        if (TokensRemaining < 1)
        {
            Fail("Out of tokens in destination parameter");
            return (null, 0);
        }

        var token = Tokens[0];
        // bits 14 through 15
        var reserved1 = (token >> 14) & 0x3;
        // bit 31
        var reserved2 = (token >> 31) & 0x1;

        var info = new MojoShaderDestArgInfo
        {
            Token = OrigTokens[CurrentPosition],
            RegNum = token & 0x7FF,
            Relative = ((token >> 13) & 0x1) != 0,
            OrigWriteMask = (token >> 16) & 0xF,
            ResultMod = (MojoShaderMod)((token >> 20) & 0xF),
            ResultShift = (token >> 24) & 0xF,
            // bits 28-30, 11-12
            RegType = (MojoShaderRegisterType)
                (((token >> 28) & 0x7) | ((token >> 8) & 0x18))
        };

        var writeMask = IsScalar(ShaderType, info.RegType, info.RegNum)
            ? MojoShaderWriteMaskValue.X
            : info.OrigWriteMask;

        SetDstArgWriteMask(info, writeMask);

        // all the REG_TYPE_CONSTx types are the same register type, it's just
        //  split up so its regnum can be > 2047 in the bytecode. Clean it up.
        switch (info.RegType)
        {
            case MojoShaderRegisterType.Const2:
                info.RegType = MojoShaderRegisterType.Const;
                info.RegNum += 2048;
                break;
            case MojoShaderRegisterType.Const3:
                info.RegType = MojoShaderRegisterType.Const;
                info.RegNum += 4096;
                break;
            case MojoShaderRegisterType.Const4:
                info.RegType = MojoShaderRegisterType.Const;
                info.RegNum += 6144;
                break;
        }

        // swallow token for now, for multiple calls in a row.
        AdjustTokenPosition(1);

        if (reserved1 != 0)
            Fail("Reserved bit #1 in destination token must be zero");
        if (reserved2 != 1)
            Fail("Reserved bit #2 in destination token must be one");

        if (info.Relative)
        {
            if (!ShaderIsVertex())
                Fail("Relative addressing in non-vertex shader");
            if (!ShaderVersionAtLeast(3, 0))
                Fail("Relative addressing in vertex shader version < 3.0");

            // it's hard to do this efficiently without!
            if (!Ctab?.HaveCtab ?? false)
                Fail("relative addressing unsupported without a CTAB");

            // !!! FIXME: I don't have a shader that has a relative dest currently.
            Fail("Relative addressing of dest tokens is unsupported");
            return (info, 1);
        }

        var s = info.ResultShift;
        if (s != 0)
        {
            if (!ShaderIsPixel())
                Fail("Result shift scale in non-pixel shader");
            if (ShaderVersionAtLeast(2, 0))
                Fail("Result shift scale in pixel shader version >= 2.0");
            if (s is not ((>= 1 and <= 3) or (>= 13 and <= 15)))
                Fail("Result shift scale isn't 1 to 3, or 13 to 15.");
        }

        if ((info.ResultMod & MojoShaderMod.Pp) != 0)
        {
            if (!ShaderIsPixel())
                Fail("Partial precision result mod in non-pixel shader");
        }

        if ((info.ResultMod & MojoShaderMod.Centroid) != 0)
        {
            if (!ShaderIsPixel())
                Fail("Centroid result mod in non-pixel shader");
            else if (!CentroidAllowed)
                Fail("Centroid modifier not allowed here");
        }

        if (info.RegType > MojoShaderRegisterType.Max)
            Fail("Register type is out of range");

        if (!IsFail)
            SetUsedRegister(info.RegType, info.RegNum, true);

        return (info, 1);
    }

    /// <summary>
    /// [set_dstarg_writemask; mojoshader_profile_common.c]
    /// </summary>
    public void SetDstArgWriteMask(
        MojoShaderDestArgInfo dst,
        int mask) =>
        dst.WriteMask = mask;

    /// <summary>
    /// [set_used_register; mojoshader_profile_common.c]
    /// </summary>
    public MojoShaderRegister SetUsedRegister(
        MojoShaderRegisterType regType,
        int regNum,
        bool written)
    {
        if (regType == MojoShaderRegisterType.ColorOut && regNum > 0)
            HaveMultiColorOutputs = true;

        var reg = RegListInsert(UsedRegisters, regType, regNum);
        if (written)
            reg.Written = true;

        return reg;
    }

    /// <summary>
    /// [reglist_insert; mojoshader_profile_common.c]
    /// </summary>
    public MojoShaderRegister RegListInsert(
        List<MojoShaderRegister> registers,
        MojoShaderRegisterType regType,
        int regNum)
    {
        var item = RegListFind(registers, regType, regNum);
        if (item != null)
            return item;

        item = CreateRegister(regType, regNum);
        registers.Add(item);
        return item;
    }

    /// <summary>
    /// [reg_to_ui32; mojoshader_profile_common.c]
    /// </summary>
    public int RegToUi32(
        MojoShaderRegisterType regType, int regNum) =>
        regNum | ((int)regType << 16);

    /// <summary>
    /// [set_defined_register; mojoshader_profile_common.c]
    /// </summary>
    public void SetDefinedRegister(
        MojoShaderRegisterType regType,
        int regNum) =>
        RegListInsert(DefinedRegisters, regType, regNum);

    /// <summary>
    /// [reglist_find; mojoshader_profile_common.c]
    /// </summary>
    public MojoShaderRegister? RegListFind(
        List<MojoShaderRegister> registers,
        MojoShaderRegisterType regType,
        int regNum)
    {
        var newVal = RegToUi32(regType, regNum);
        return registers.FirstOrDefault(r => newVal == RegToUi32(r.RegType, r.RegNum));
    }

    /// <summary>
    /// Create a register (helper function.)
    /// </summary>
    public MojoShaderRegister CreateRegister(
        MojoShaderRegisterType regType,
        int regNum) =>
        new()
        {
            RegType = regType,
            RegNum = regNum,
            Usage = MojoShaderUsage.Unknown
        };

    /// <summary>
    /// [scalar_register; mojoshader_internal.h]
    /// </summary>
    public bool ScalarRegister(MojoShaderShaderType shaderType, MojoShaderRegisterType regType,
        int regNum) =>
        regType switch
        {
            MojoShaderRegisterType.RastOut => (MojoShaderRastOutType)regNum switch
            {
                MojoShaderRastOutType.Fog => true,
                MojoShaderRastOutType.PointSize => true,
                _ => false
            },
            MojoShaderRegisterType.DepthOut => true,
            MojoShaderRegisterType.ConstBool => true,
            MojoShaderRegisterType.Loop => true,
            MojoShaderRegisterType.MiscType => (MojoShaderMiscTypeType)regNum == MojoShaderMiscTypeType.Face,
            MojoShaderRegisterType.Predicate => shaderType == MojoShaderShaderType.Pixel,
            _ => false
        };

    /// <summary>
    /// [isscalar; mojoshader_profile_common.c]
    /// </summary>
    public bool IsScalar(
        MojoShaderShaderType shaderType,
        MojoShaderRegisterType rType,
        int rNum)
    {
        var usesPSize = UsesPointSize;
        var usesFog = UsesFog;

        if (rType == MojoShaderRegisterType.Output && (usesPSize || usesFog) &&
            RegListFind(Attributes, rType, rNum) is { } reg)
        {
            var usage = reg.Usage;
            return (usesPSize && usage == MojoShaderUsage.PointSize) ||
                   (usesFog && usage == MojoShaderUsage.Fog);
        }

        return ScalarRegister(shaderType, rType, rNum);
    }

    /// <summary>
    /// [set_output; mojoshader_profile_common.c]
    /// </summary>
    public void SetOutput(
        MojoShaderProfileOutput output) =>
        Output = output switch
        {
            MojoShaderProfileOutput.Preflight => Preflight,
            MojoShaderProfileOutput.Globals => Globals,
            MojoShaderProfileOutput.Inputs => Inputs,
            MojoShaderProfileOutput.Outputs => Outputs,
            MojoShaderProfileOutput.Helpers => Helpers,
            MojoShaderProfileOutput.Subroutines => Subroutines,
            MojoShaderProfileOutput.MainLineIntro => MainLineIntro,
            MojoShaderProfileOutput.MainLineArguments => MainLineArguments,
            MojoShaderProfileOutput.MainLineTop => MainLineTop,
            MojoShaderProfileOutput.MainLine => MainLine,
            MojoShaderProfileOutput.Postflight => Postflight,
            MojoShaderProfileOutput.Ignore => Ignore,
            _ => throw new ArgumentOutOfRangeException(nameof(output), output, null)
        };

    /// <summary>
    /// [push_output; mojoshader_profile_common.c]
    /// </summary>
    public void PushOutput(
        MojoShaderProfileOutput output)
    {
        OutputStack.Push(Output);
        IndentStack.Push(Indent);
        SetOutput(output);
        Indent = 0;
    }

    /// <summary>
    /// [push_output; mojoshader_profile_common.c]
    /// </summary>
    public void PopOutput()
    {
        Output = OutputStack.Pop();
        Indent = IndentStack.Pop();
    }

    /// <summary>
    /// This forwards to the formatted one and is here for convenience
    /// (and syntax checker sanity)
    /// [output_line; mojoshader_profile_common.c]
    /// </summary>
    public void OutputLine(
        string? line) =>
        OutputLine(line, Array.Empty<object?>());

    /// <summary>
    /// [output_line; mojoshader_profile_common.c]
    /// </summary>
    public void OutputLine(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
        string? format,
        params object?[] items)
    {
        // If we failed previously, don't go on...
        if (IsFail)
            return;

        var indent = Indent;
        if (indent > 0)
            Output.Write(new string('\t', indent));

        if (items.Length > 0)
            Output.WriteLine(format ?? string.Empty, items);
        else
            Output.WriteLine(format ?? string.Empty);
    }

    /// <summary>
    /// [output_blank_line; mojoshader_profile_common.c]
    /// </summary>
    public void OutputBlankLine() =>
        Output.WriteLine();

    /// <summary>
    /// [adjust_token_position; mojoshader.c]
    /// </summary>
    public void AdjustTokenPosition(
        int incr)
    {
        CurrentPosition += incr;
        ErrorPosition = (MojoShaderPosition)(CurrentPosition * 4);
    }

    /// <summary>
    /// [parse_comment_token; mojoshader.c]
    /// </summary>
    public int ParseCommentToken(
    )
    {
        var (isCommentToken, commentTokens) = IsCommentToken(Tokens[0]);
        if (isCommentToken)
        {
            if (commentTokens >= 2 && commentTokens < TokensRemaining)
            {
                var id = Tokens[1];
                if (id == MojoShaderConstants.PresId)
                    ParsePreshader(Tokens[2..], commentTokens - 2);
                else if (id == MojoShaderConstants.CTabId)
                    ParseConstantTable(Tokens, VersionToken, true, Ctab);
            }

            return commentTokens + 1; // comment data plus the initial token.
        }

        return 0; // not a comment token.
    }

    /// <summary>
    /// [parse_predicated_token; mojoshader.c]
    /// </summary>
    public int ParsePredicatedToken(
    )
    {
        var (arg, _) = ParseSourceToken();
        PredicateArg = arg;

        if (arg != null)
        {
            if (arg.RegType != MojoShaderRegisterType.Predicate)
                Fail("Predicated instruction but not predicate register!");
            if (arg.SrcMod != MojoShaderSourceMod.None && arg.SrcMod != MojoShaderSourceMod.Not)
                Fail("Predicated instruction register is not NONE or NOT");
            if (arg.Swizzle is { IsNone: false, IsReplicate: false })
                Fail("Predicated instruction register has wrong swizzle");
            if (arg.Relative) // I'm pretty sure this is illegal...?
                Fail("relative addressing in predicated token");
        }

        return 1;
    }

    /// <summary>
    /// [parse_args_NULL; mojoshader.c]
    /// </summary>
    public int ParseArgsNull(
    ) => 1;

    /// <summary>
    /// [parse_args_DEF; mojoshader.c
    /// </summary>
    public int ParseArgsDef(
    )
    {
        (DestArg, _) = ParseDestinationToken();
        if (DestArg?.RegType != MojoShaderRegisterType.Const)
            Fail("DEF using non-CONST register");
        if (DestArg?.Relative ?? false)
            Fail("relative addressing in DEF");

        Tokens[..4].CopyTo(Dwords);

        return 6;
    }

    /// <summary>
    /// [parse_args_DEFI; mojoshader.c]
    /// </summary>
    public int ParseArgsDefI(
    )
    {
        (DestArg, _) = ParseDestinationToken();
        if (DestArg?.RegType != MojoShaderRegisterType.ConstInt)
            Fail("DEFI using non-CONSTING register");
        if (DestArg?.Relative ?? false) // I'm pretty sure this is illegal...?
            Fail("relative addressing in DEFI");

        Tokens[..4].CopyTo(Dwords);

        return 6;
    }

    /// <summary>
    /// [parse_args_DEFB: mojoshader.c]
    /// </summary>
    public int ParseArgsDefB(
    )
    {
        (DestArg, _) = ParseDestinationToken();
        if (DestArg?.RegType != MojoShaderRegisterType.ConstBool)
            Fail("DEFB using non-CONSTBOOL register");
        if (DestArg?.Relative ?? false) // I'm pretty sure this is illegal...?
            Fail("relative addressing in DEFB");

        Dwords[0] = Tokens[0] != 0 ? ~0 : 0;

        return 3;
    }

    /// <summary>
    /// !!! FIXME: this function is kind of a mess.
    /// [parse_args_DCL; mojoshader.c]
    /// </summary>
    public int ParseArgsDcl(
    )
    {
        var unsupported = false;
        var token = Tokens[0];
        var reserved1 = (token >> 31) & 0x1;
        var reservedMask = 0x00000000;

        if (reserved1 != 0x1)
            Fail("Bit #31 in DCL token must be one");

        CentroidAllowed = true;
        AdjustTokenPosition(1);
        var (destArg, _) = ParseDestinationToken();
        DestArg = destArg;
        destArg ??= new MojoShaderDestArgInfo();
        CentroidAllowed = false;

        if (destArg.ResultShift != 0) // I'm pretty sure this is illegal...?
            Fail("shift scale in DCL");
        if (destArg.Relative) // I'm pretty sure this is illegal...?
            Fail("relative addressing in DCL");

        var regType = destArg.RegType;
        var regNum = destArg.RegNum;
        if (ShaderIsPixel() && ShaderVersionAtLeast(3, 0))
        {
            if (regType == MojoShaderRegisterType.Input)
            {
                var usage = token & 0xF;
                var index = (token >> 16) & 0xF;
                reservedMask = 0x7FF0FFE0;
                Dwords[0] = usage;
                Dwords[1] = index;
            }

            else if (regType == MojoShaderRegisterType.MiscType)
            {
                var mt = (MojoShaderMiscTypeType)regNum;
                if (mt == MojoShaderMiscTypeType.Position)
                    reservedMask = 0x7FFFFFFF;
                else if (mt == MojoShaderMiscTypeType.Face)
                {
                    reservedMask = 0x7FFFFFFF;
                    if (!destArg.OrigWriteMask.IsXyzw)
                        Fail("DCL face writemask must be full");
                    if (destArg.ResultMod != 0)
                        Fail("DCL face result modifier must be zero");
                    if (destArg.ResultShift != 0)
                        Fail("DCL face shift scale must be zero");
                }
                else
                {
                    unsupported = true;
                }

                Dwords[0] = (int)MojoShaderUsage.Unknown;
                Dwords[1] = 0;
            }

            else if (regType == MojoShaderRegisterType.Texture)
            {
                var usage = (MojoShaderUsage)(token & 0xF);
                var index = (token >> 16) & 0xF;
                if (usage == MojoShaderUsage.TexCoord)
                {
                    if (index > 7)
                        Fail("DCL texcoord usage must have 0-7 index");
                }
                else if (usage == MojoShaderUsage.Color)
                {
                    if (index != 0)
                        Fail("DCL color usage must have 0 index");
                }
                else
                {
                    Fail("Invalid DCL texture usage");
                }

                reservedMask = 0x7FF0FFE0;
                Dwords[0] = (int)usage;
                Dwords[1] = index;
            }

            else if (regType == MojoShaderRegisterType.Sampler)
            {
                var tType = (token >> 27) & 0xF;
                if (!ValidTextureType(tType))
                    Fail("unknown sampler texture type");
                reservedMask = 0x7FFFFFF;
                Dwords[0] = tType;
            }

            else
            {
                unsupported = true;
            }
        }

        else if (ShaderIsPixel() && ShaderVersionAtLeast(2, 0))
        {
            if (regType == MojoShaderRegisterType.Input)
            {
                Dwords[0] = (int)MojoShaderUsage.Color;
                Dwords[1] = regNum;
                reservedMask = 0x7FFFFFFF;
            }
            else if (regType == MojoShaderRegisterType.Texture)
            {
                Dwords[0] = (int)MojoShaderUsage.TexCoord;
                Dwords[1] = regNum;
                reservedMask = 0x7FFFFFFF;
            }
            else if (regType == MojoShaderRegisterType.Sampler)
            {
                var tType = (token >> 27) & 0xF;
                if (!ValidTextureType(tType))
                    Fail("unknown sampler texture type");
                reservedMask = 0x7FFFFFF;
                Dwords[0] = tType;
            }
            else
            {
                unsupported = true;
            }
        }

        else if (ShaderIsVertex() && ShaderVersionAtLeast(3, 0))
        {
            if (regType is MojoShaderRegisterType.Input or MojoShaderRegisterType.Output)
            {
                var usage = token & 0xF;
                var index = (token >> 16) & 0xF;
                reservedMask = 0x7FF0FFE0;
                Dwords[0] = usage;
                Dwords[1] = index;
            }

            else if (regType == MojoShaderRegisterType.Texture)
            {
                var usage = (MojoShaderUsage)(token & 0xF);
                var index = (token >> 16) & 0xF;
                if (usage == MojoShaderUsage.TexCoord)
                {
                    if (index > 7)
                        Fail("DCL texcoord usage must have 0-7 index");
                } // if
                else if (usage == MojoShaderUsage.Color)
                {
                    if (index != 0)
                        Fail("DCL texcoord usage must have 0 index");
                } // else if
                else
                    Fail("Invalid DCL texture usage");

                reservedMask = 0x7FF0FFE0;
                Dwords[0] = (int)usage;
                Dwords[1] = index;
            }

            else if (regType == MojoShaderRegisterType.Sampler)
            {
                var tType = (token >> 27) & 0xF;
                if (!ValidTextureType(tType))
                    Fail("Unknown sampler texture type");
                reservedMask = 0x0FFFFFFF;
                Dwords[0] = tType;
            }

            else
            {
                unsupported = true;
            }
        }

        else if (ShaderIsVertex() && ShaderVersionAtLeast(1, 1))
        {
            if (regType == MojoShaderRegisterType.Input)
            {
                var usage = (token & 0xF);
                var index = ((token >> 16) & 0xF);
                reservedMask = 0x7FF0FFE0;
                Dwords[0] = usage;
                Dwords[1] = index;
            }
            else
            {
                unsupported = true;
            }
        }

        else
        {
            unsupported = true;
        }

        if (unsupported)
            Fail("invalid DCL register type for this shader model");

        if ((token & reservedMask) != 0)
            Fail("reserved bits in DCL dword aren't zero");

        return 3;
    }

    /// <summary>
    /// Wraps parse_args_*.
    /// </summary>
    public int ParseArgs(MojoShaderInstructionArgs args)
    {
        return args switch
        {
            MojoShaderInstructionArgs.Null => ParseArgs(0, 0),
            MojoShaderInstructionArgs.D => ParseArgs(1, 0),
            MojoShaderInstructionArgs.Ds => ParseArgs(1, 1),
            MojoShaderInstructionArgs.Dss => ParseArgs(1, 2),
            MojoShaderInstructionArgs.Dsss => ParseArgs(1, 3),
            MojoShaderInstructionArgs.Dssss => ParseArgs(1, 4),
            MojoShaderInstructionArgs.S => ParseArgs(0, 1),
            MojoShaderInstructionArgs.Ss => ParseArgs(0, 2),
            MojoShaderInstructionArgs.Dcl => ParseArgsDcl(),
            MojoShaderInstructionArgs.SinCos => ParseArgsSinCos(),
            MojoShaderInstructionArgs.DefB => ParseArgsDefB(),
            MojoShaderInstructionArgs.DefI => ParseArgsDefI(),
            MojoShaderInstructionArgs.TexCrd => ParseArgsTexCrd(),
            MojoShaderInstructionArgs.Def => ParseArgsDef(),
            MojoShaderInstructionArgs.TexLd => ParseArgsTexLd(),
            _ => throw new ArgumentOutOfRangeException(nameof(args), args, null)
        };
    }

    /// <summary>
    /// Simplifies parse_args_*.
    /// [parse_args_NULL; mojoshader.c]
    /// [parse_args_D; mojoshader.c]
    /// [parse_args_DS; mojoshader.c]
    /// [parse_args_DSS; mojoshader.c]
    /// [parse_args_DSSS; mojoshader.c]
    /// [parse_args_DSSSS; mojoshader.c]
    /// [parse_args_S; mojoshader.c]
    /// [parse_args_SS; mojoshader.c]
    /// </summary>
    public int ParseArgs(int dstCount, int srcCount)
    {
        // This clear was not in the original code; this is to keep
        // the context tidy.
        DestArg = null;
        SourceArgs.AsSpan().Clear();

        var total = 1;

        // Read destination tokens.
        for (var i = 0; i < dstCount; i++)
        {
            var (dst, size) = ParseDestinationToken();
            DestArg = dst;
            total += size;
        }

        // Read source tokens.
        for (var i = 0; i < srcCount; i++)
        {
            var (src, size) = ParseSourceToken();
            SourceArgs[i] = src;
            total += size;
        }

        return total;
    }

    /// <summary>
    /// [parse_args_SINCOS; mojoshader.c]
    /// </summary>
    public int ParseArgsSinCos(
    )
    {
        // this opcode needs extra registers for sm2 and lower.
        return !ShaderVersionAtLeast(3, 0)
            ? ParseArgs(1, 3)
            : ParseArgs(1, 1);
    }

    /// <summary>
    /// [parse_args_TEXCRD; mojoshader.c]
    /// </summary>
    public int ParseArgsTexCrd(
    )
    {
        // added extra register in ps_1_4.
        return ShaderVersionAtLeast(1, 4)
            ? ParseArgs(1, 1)
            : ParseArgs(1, 0);
    }

    /// <summary>
    /// [parse_args_TEXLD; mojoshader.c]
    /// </summary>
    public int ParseArgsTexLd(
    )
    {
        // different registers in px_1_3, ps_1_4, and ps_2_0!
        return ShaderVersionAtLeast(2, 0)
            ? ParseArgs(1, 2)
            : ShaderVersionAtLeast(1, 4)
                ? ParseArgs(1, 1)
                : ParseArgs(1, 0);
    }

    /// <summary>
    /// [parse_instruction_token; mojoshader.c]
    /// </summary>
    public int ParseInstructionToken(
    )
    {
        Log?.WriteLine(
            "[ParseInstructionToken] tok=0x{0:X8} idx=0x{1:X4} pos=0x{2:X4}",
            Tokens[0], CurrentPosition, (int)ErrorPosition);
        var startPosition = CurrentPosition;
        var token = Tokens[0];
        var opcode = token & 0xFFFF;
        var controls = (token >> 16) & 0xFF;
        var instTokens = (token >> 24) & 0xF;
        var coIssue = (token & 0x40000000) != 0;
        var predicated = (token & 0x10000000) != 0;

        if (opcode >= Instructions.Count)
            return 0;

        var instruction = Instructions[opcode];

        // check bit 31
        if (token < 0)
            Fail("instruction token high bit must be zero."); // so says msdn.

        if (instruction.OpcodeString == null)
        {
            Fail("Unknown opcode.");
            return instTokens + 1; // pray that you resync later.
        }

        CoIssue = coIssue;
        if (coIssue)
        {
            if (!ShaderIsPixel())
                Fail("coissue instruction on non-pixel shader");
            if (ShaderVersionAtLeast(2, 0))
                Fail("coissue instruction in Shader Model >= 2.0");
        }

        if ((ShaderType & instruction.ShaderTypes) == 0)
        {
            Fail("opcode '{0}' not available in this shader type.", instruction.OpcodeString);
        }

        Dwords.AsSpan().Clear();
        InstructionControls = controls;
        Predicated = predicated;

        // Update the context with instruction's arguments.
        AdjustTokenPosition(1);

        var result = ParseArgs(instruction.Args);

        if (predicated)
            result += ParsePredicatedToken();

        // parse_args() moves these forward for convenience...reset them.
        CurrentPosition = startPosition;

        State(instruction.Opcode);

        InstructionCount += instruction.Slots;

        if (!IsFail)
            Profile.EmitFunction(this, instruction.Opcode); // call the profile's emitter.

        if (ResetTexMpad)
        {
            TexM3X2PadDst0 = -1;
            TexM3X2PadSrc0 = -1;
            TexM3X3PadDst0 = -1;
            TexM3X3PadSrc0 = -1;
            TexM3X3PadDst1 = -1;
            TexM3X3PadSrc1 = -1;
            ResetTexMpad = false;
        }

        PreviousOpcode = instruction.Opcode;
        ScratchRegisters = 0; // reset after every instruction.

        if (!ShaderVersionAtLeast(2, 0))
        {
            if (instTokens != 0) // reserved field in shaders < 2.0 ...
                Fail("instruction token count must be zero");
        }
        else
        {
            if (result != instTokens + 1)
            {
                Fail("wrong token count ({0}, not {1}) for opcode '{2}'.",
                    result, instTokens + 1,
                    instruction.OpcodeString);
                result = instTokens + 1; // try to keep sync.
            }
        }

        return result;
    }

    /// <summary>
    /// [parse_version_token; mojoshader.c]
    /// </summary>
    public int ParseVersionToken(
        string profileStr)
    {
        if (TokensRemaining < 1)
        {
            Fail("Expected version token, got none at all.");
            return 0;
        }

        var token = Tokens[0];
        var shaderType = (token >> 16) & 0xFFFF;
        var major = (token >> 8) & 0xFF;
        var minor = token & 0xFF;

        VersionToken = token;

        // 0xFFFF == pixel shader, 0xFFFE == vertex shader
        if (shaderType == 0xFFFF)
        {
            ShaderType = MojoShaderShaderType.Pixel;
            ShaderTypeStr = "ps";
        }
        else if (shaderType == 0xFFFE)
        {
            ShaderType = MojoShaderShaderType.Vertex;
            ShaderTypeStr = "vs";
        }
        else // geometry shader? Bogus data?
        {
            Fail("Unsupported shader type or not a shader at all");
            return -1;
        }

        MajorVer = major;
        MinorVer = minor;

        if (!ShaderVersionSupported(major, minor))
            Fail("Shader Model {0}.{1} is currently unsupported.", major, minor);

        if (!IsFail)
            Profile.EmitStart(this, profileStr);

        return 1; // ate one token.
    }

    /// <summary>
    /// [parse_ctab_string; mojoshader.c]
    /// </summary>
    public string? ParseCTabString(
        Span<byte> start, int offset)
    {
        // Make sure strings don't overflow the CTAB buffer...
        if (offset < start.Length)
        {
            var name = start[offset..];
            var length = name.IndexOf((byte)0);
            if (length >= 0)
                name = name[..length];
            return Encoding.UTF8.GetString(name);
        }

        return null;
    }

    /// <summary>
    /// [parse_ctab_typeinfo; mojoshader.c]
    /// </summary>
    public bool ParseCTabTypeInfo(
        Span<byte> start,
        int bytes,
        int pos,
        MojoShaderSymbolTypeInfo info,
        int depth)
    {
        if ((bytes <= pos) || ((bytes - pos) < 16))
            return false; // corrupt CTAB.

        var typePtr = start[pos..];

        info.ParameterClass = (MojoShaderSymbolClass)BinaryPrimitives.ReadInt16LittleEndian(typePtr);
        info.ParameterType = (MojoShaderSymbolType)BinaryPrimitives.ReadInt16LittleEndian(typePtr[2..]);
        info.Rows = BinaryPrimitives.ReadInt16LittleEndian(typePtr[4..]);
        info.Columns = BinaryPrimitives.ReadInt16LittleEndian(typePtr[6..]);
        info.Elements = BinaryPrimitives.ReadInt16LittleEndian(typePtr[8..]);

        if (info.ParameterClass >= MojoShaderSymbolClass.Total)
        {
            Fail("Unknown parameter class (0x{0:X})", info.ParameterClass);
            info.ParameterClass = MojoShaderSymbolClass.Scalar;
        }

        if (info.ParameterType >= MojoShaderSymbolType.Total)
        {
            Fail("Unknown parameter type (0x{0:X})", info.ParameterType);
            info.ParameterType = MojoShaderSymbolType.Int;
        }

        var memberCount = BinaryPrimitives.ReadInt16LittleEndian(typePtr[10..]);
        info.Members.Clear();

        if (pos + 16 + memberCount * 8 >= bytes)
            return false; // corrupt CTAB.

        if (memberCount > 0)
        {
            if (depth > 300) // make sure we aren't in an infinite loop here.
            {
                Fail("Possible infinite loop in CTAB structure.");
                return false;
            }
        }

        int i;
        var memberOffset = BinaryPrimitives.ReadInt16LittleEndian(typePtr[12..]);
        var member = start[memberOffset..];
        for (i = 0; i < memberCount; i++)
        {
            var mbr = new MojoShaderSymbolStructMember
            {
                Info = new MojoShaderSymbolTypeInfo()
            };
            info.Members.Add(mbr);
            var name = BinaryPrimitives.ReadInt16LittleEndian(member);
            var memberInfoPos = BinaryPrimitives.ReadInt16LittleEndian(member[2..]);
            member = member[4..];

            mbr.Name = ParseCTabString(start, name);
            if (!ParseCTabTypeInfo(start, bytes, memberInfoPos, mbr.Info, depth + 1))
                return false;
        }

        return true;
    }

    /// <summary>
    /// [parse_constant_table; mojoshader.c]
    /// </summary>
    public void ParseConstantTable(
        Span<int> tokens,
        int okayVersion,
        bool setVariables,
        MojoShaderCtabData? cTab)
    {
        if (cTab == null)
            return;

        const int cTabId = 0x42415443; // 0x42415443 == 'CTAB'
        const int cTabSize = 28; // sizeof (D3DXSHADER_CONSTANTTABLE).
        const int cInfoSize = 20; // sizeof (D3DXSHADER_CONSTANTINFO).

        void CorruptCTab() =>
            Fail("Shader has corrupt CTAB data");

        var id = tokens[1];
        if (id != cTabId)
            return; // not the constant table.

        if (cTab.HaveCtab) // !!! FIXME: can you have more than one?
        {
            Fail("Shader has multiple CTAB sections");
            return;
        }

        cTab.HaveCtab = true;

        Span<byte> start = new byte[(tokens.Length - 2) * 4];
        for (int j = 0, k = 0; j < tokens.Length; j++, k += 4)
        {
            BinaryPrimitives.WriteInt32LittleEndian(start[k..], tokens[j]);
        }

        if (tokens.Length < 8)
        {
            Fail("Truncated CTAB data");
            return;
        }

        var bytes = tokens.Length * 4;
        var size = tokens[2];
        var creator = tokens[3];
        var version = tokens[4];
        var constants = tokens[5];
        var constantInfo = tokens[6];
        var target = tokens[8];

        if (size != cTabSize) // CTAB_SIZE
        {
            CorruptCTab();
            return;
        }

        if (constants > 1000000) // sanity check.
        {
            CorruptCTab();
            return;
        }

        if (version != okayVersion)
        {
            CorruptCTab();
            return;
        }

        if (creator >= bytes)
        {
            CorruptCTab();
            return;
        }

        if (constantInfo >= bytes)
        {
            CorruptCTab();
            return;
        }

        if ((bytes - constantInfo) < (constants * cInfoSize))
        {
            CorruptCTab();
            return;
        }

        if (target >= bytes)
        {
            CorruptCTab();
            return;
        }

        if (ParseCTabString(start, target) is not { })
        {
            CorruptCTab();
            return;
        }

        // !!! FIXME: check that (start+target) points to "ps_3_0", etc.

        cTab.Symbols.Clear();
        for (var i = 0; i < constants; i++)
        {
            var ptr = start[(constantInfo + i * cInfoSize)..];
            var name = BinaryPrimitives.ReadInt32LittleEndian(ptr);
            var regSet = BinaryPrimitives.ReadInt16LittleEndian(ptr[4..]);
            var regIdx = BinaryPrimitives.ReadInt16LittleEndian(ptr[6..]);
            var regCnt = BinaryPrimitives.ReadInt16LittleEndian(ptr[8..]);
            var typeInf = BinaryPrimitives.ReadInt32LittleEndian(ptr[12..]);
            var defVal = BinaryPrimitives.ReadInt32LittleEndian(ptr[16..]);
            var type = MojoShaderUniformType.Unknown;

            if (ParseCTabString(start, name) is not { } symbolString)
            {
                CorruptCTab();
                return;
            }

            if (defVal >= bytes)
            {
                CorruptCTab();
                return;
            }

            switch (regSet)
            {
                case 0:
                    type = MojoShaderUniformType.Bool;
                    break;
                case 1:
                    type = MojoShaderUniformType.Int;
                    break;
                case 2:
                    type = MojoShaderUniformType.Float;
                    break;
                case 3: /* SAMPLER */ break;
                default:
                {
                    CorruptCTab();
                    return;
                }
            }

            if (setVariables && type != MojoShaderUniformType.Unknown)
            {
                var item = new MojoShaderVariable
                {
                    Type = type,
                    Index = regIdx,
                    Used = false,
                    EmitPosition = -1
                };
                Variables.Add(item);
            }

            // Add the symbol.
            var sym = new MojoShaderSymbol
            {
                Name = symbolString,
                RegisterSet = (MojoShaderSymbolRegisterSet)regSet,
                RegisterIndex = regIdx,
                RegisterCount = regCnt,
                Info = new MojoShaderSymbolTypeInfo()
            };
            cTab.Symbols.Add(sym);

            if (!ParseCTabTypeInfo(start, bytes, typeInf, sym.Info, 0))
            {
                CorruptCTab();
                return;
            }
        }
    }

    /// <summary>
    /// Preshaders only show up in compiled Effect files. The format is
    ///  undocumented, and even the instructions aren't the same opcodes as you
    ///  would find in a regular shader. These things show up because the HLSL
    ///  compiler can detect work that sets up constant registers that could
    ///  be moved out of the shader itself. Preshaders run once, then the shader
    ///  itself runs many times, using the constant registers the preshader has set
    ///  up. There are cases where the preshaders are 3+ times as many instructions
    ///  as the shader itself, so this can be a big performance win.
    /// My presumption is that Microsoft's Effects framework runs the preshaders on
    ///  the CPU, then loads the constant register file appropriately before handing
    ///  off to the GPU. As such, we do the same.
    /// [parse_preshader; mojoshader.c]
    /// </summary>
    public void ParsePreshader(
        Span<int> tokens,
        int tokCount)
    {
        const int prsiId = MojoShaderConstants.PrsiId; // 0x49535250 == 'PRSI'
        const int clitId = MojoShaderConstants.ClitId; // 0x54494C43 == 'CLIT'
        const int fxlcId = MojoShaderConstants.FxlcId; // 0x434C5846 == 'FXLC'
        const int cTabId = MojoShaderConstants.CTabId;

        Debug.Assert(!HavePreshader);
        HavePreshader = true;

        // !!! FIXME: I don't know what specific versions signify, but we need to
        // !!! FIXME:  save this to test against the CTAB version field, if
        // !!! FIXME:  nothing else.
        // !!! FIXME: 0x02 0x0? is probably the version (fx_2_?),
        // !!! FIXME:  and 0x4658 is the magic, like a real shader's version token.
        const int versionMagic = MojoShaderConstants.PreshaderVersionMagic;
        const int minVersion = 0x00000200 | versionMagic;
        const int maxVersion = 0x00000201 | versionMagic;
        var version = tokens[0];
        if (version is < minVersion or > maxVersion)
        {
            Fail("Unsupported preshader version.");
            return; // fail because the shader will malfunction w/o this.
        }
        
        tokens = tokens[1..];
        tokCount--;

        // All sections of a preshader are packed into separate comment tokens,
        //  inside the containing comment token block. Find them all before
        //  we start, so we don't care about the order they appear in the file.
        var ctab = new MojoShaderPreshaderBlockInfo();
        var prsi = new MojoShaderPreshaderBlockInfo();
        var fxlc = new MojoShaderPreshaderBlockInfo();
        var clit = new MojoShaderPreshaderBlockInfo();

        while (tokCount > 0)
        {
            var (isCommentToken, _) = IsCommentToken(tokens[0]);
            if (!isCommentToken)
            {
                // !!! FIXME: Standalone preshaders have this EOS-looking token,
                // !!! FIXME:  sometimes followed by tokens that don't appear to
                // !!! FIXME:  have anything to do with the rest of the blob.
                // !!! FIXME: So for now, treat this as a special "EOS" comment.
                if (tokens[0] == 0xFFFF)
                    break;

                Fail("Bogus preshader data.");
                return;
            }

            tokens = tokens[1..];
            tokCount--;

            if (tokens.Length > 0)
            {
                void PreshaderBlockCase(int id, MojoShaderPreshaderBlockInfo var, Span<int> tokens0)
                {
                    if (var.Seen)
                    {
                        Fail("Multiple {0:X} preshader blocks.", id);
                        return;
                    }

                    var.RawTokens = tokens0.ToArray();
                    var.Seen = true;
                }

                switch (tokens[0])
                {
                    case cTabId:
                        PreshaderBlockCase(cTabId, ctab, tokens);
                        break;
                    case prsiId:
                        PreshaderBlockCase(prsiId, prsi, tokens);
                        break;
                    case fxlcId:
                        PreshaderBlockCase(fxlcId, fxlc, tokens);
                        break;
                    case clitId:
                        PreshaderBlockCase(clitId, clit, tokens);
                        break;
                    default:
                        Fail("Bogus preshader section.");
                        return;
                }
            }
        }

        if (!ctab.Seen)
        {
            Fail("No CTAB block in preshader.");
            return;
        }

        if (!fxlc.Seen)
        {
            Fail("No FXLC block in preshader.");
            return;
        }

        if (!clit.Seen)
        {
            Fail("No CLIT block in preshader.");
            return;
        }
        // prsi.seen is optional, apparently.

        var preshader = new MojoShaderPreshader();
        Preshader = preshader;

        // Let's set up the constant literals first...
        if (clit.TokenCount == 0)
            Fail("Bogus CLIT block in preshader.");
        else
        {
            var litCount = clit.Tokens[1];
            if (litCount > ((clit.TokenCount - 2) / 2))
            {
                Fail("Bogus CLIT block in preshader.");
                return;
            }

            if (litCount > 0)
            {
                // [Saxxon] int* -> byte* -> double*
                // We have to do it like this to be endian-safe.
                preshader.Literals = new List<double>();
                var litBytes = MemoryMarshal.Cast<int, byte>(clit.Tokens[2..]);
                for (var i = 0; i < litCount; i++)
                {
                    preshader.Literals.Add(BinaryPrimitives.ReadDoubleLittleEndian(litBytes));
                    litBytes = litBytes[8..];
                }
            }
        }

        // Parse out the PRSI block. This is used to map the output registers.
        var outputMapCount = 0;
        var outputMap = Span<int>.Empty;

        if (prsi.Seen)
        {
            if (prsi.TokenCount < 8)
            {
                Fail("Bogus preshader PRSI data");
                return;
            }

            //const uint32 first_output_reg = SWAP32(prsi.tokens[1]);
            // !!! FIXME: there are a lot of fields here I don't know about.
            // !!! FIXME:  maybe [2] and [3] are for int4 and bool registers?
            //const uint32 output_reg_count = SWAP32(prsi.tokens[4]);
            // !!! FIXME:  maybe [5] and [6] are for int4 and bool registers?
            outputMapCount = prsi.Tokens[7];

            prsi.TokenOffset = 8;

            if (prsi.TokenCount < (outputMapCount + 1) * 2)
            {
                Fail("Bogus preshader PRSI data");
                return;
            }

            outputMap = prsi.Tokens;
        }

        // Now we'll figure out the CTAB...
        var cTabData = new MojoShaderCtabData();
        ParseConstantTable(ctab.RawTokens[(ctab.TokenOffset - 1)..].AsSpan(), version, false, cTabData);

        // preshader owns this now. Don't free it in this function.
        preshader.Symbols = cTabData.Symbols;

        if (!cTabData.HaveCtab)
        {
            Fail("Bogus preshader CTAB data");
            return;
        }

        // The FXLC block has the actual instructions...
        var opcodeCount = fxlc.Tokens[1];

        fxlc.TokenOffset += 2;
        if (opcodeCount > fxlc.TokenCount / 2)
        {
            Fail("Bogus preshader FXLC block.");
            return;
        } // if

        while (opcodeCount-- > 0)
        {
            var inst = new MojoShaderPreshaderInstruction();
            preshader.Instructions.Add(inst);

            var opcodeTok = fxlc.Tokens[0];
            var opcode = MojoShaderPreshaderOpcode.Nop;
            switch ((opcodeTok >> 16) & 0xFFFF)
            {
                case 0x1000:
                    opcode = MojoShaderPreshaderOpcode.Mov;
                    break;
                case 0x1010:
                    opcode = MojoShaderPreshaderOpcode.Neg;
                    break;
                case 0x1030:
                    opcode = MojoShaderPreshaderOpcode.Rcp;
                    break;
                case 0x1040:
                    opcode = MojoShaderPreshaderOpcode.Frc;
                    break;
                case 0x1050:
                    opcode = MojoShaderPreshaderOpcode.Exp;
                    break;
                case 0x1060:
                    opcode = MojoShaderPreshaderOpcode.Log;
                    break;
                case 0x1070:
                    opcode = MojoShaderPreshaderOpcode.Rsq;
                    break;
                case 0x1080:
                    opcode = MojoShaderPreshaderOpcode.Sin;
                    break;
                case 0x1090:
                    opcode = MojoShaderPreshaderOpcode.Cos;
                    break;
                case 0x10A0:
                    opcode = MojoShaderPreshaderOpcode.Asin;
                    break;
                case 0x10B0:
                    opcode = MojoShaderPreshaderOpcode.Acos;
                    break;
                case 0x10C0:
                    opcode = MojoShaderPreshaderOpcode.Atan;
                    break;
                case 0x2000:
                    opcode = MojoShaderPreshaderOpcode.Min;
                    break;
                case 0x2010:
                    opcode = MojoShaderPreshaderOpcode.Max;
                    break;
                case 0x2020:
                    opcode = MojoShaderPreshaderOpcode.Lt;
                    break;
                case 0x2030:
                    opcode = MojoShaderPreshaderOpcode.Ge;
                    break;
                case 0x2040:
                    opcode = MojoShaderPreshaderOpcode.Add;
                    break;
                case 0x2050:
                    opcode = MojoShaderPreshaderOpcode.Mul;
                    break;
                case 0x2060:
                    opcode = MojoShaderPreshaderOpcode.Atan2;
                    break;
                case 0x2080:
                    opcode = MojoShaderPreshaderOpcode.Div;
                    break;
                case 0x3000:
                    opcode = MojoShaderPreshaderOpcode.Cmp;
                    break;
                case 0x3010:
                    opcode = MojoShaderPreshaderOpcode.MovC;
                    break;
                case 0x5000:
                    opcode = MojoShaderPreshaderOpcode.Dot;
                    break;
                case 0x5020:
                    opcode = MojoShaderPreshaderOpcode.Noise;
                    break;
                case 0xA000:
                    opcode = MojoShaderPreshaderOpcode.MinScalar;
                    break;
                case 0xA010:
                    opcode = MojoShaderPreshaderOpcode.MaxScalar;
                    break;
                case 0xA020:
                    opcode = MojoShaderPreshaderOpcode.LtScalar;
                    break;
                case 0xA030:
                    opcode = MojoShaderPreshaderOpcode.GeScalar;
                    break;
                case 0xA040:
                    opcode = MojoShaderPreshaderOpcode.AddScalar;
                    break;
                case 0xA050:
                    opcode = MojoShaderPreshaderOpcode.MulScalar;
                    break;
                case 0xA060:
                    opcode = MojoShaderPreshaderOpcode.Atan2Scalar;
                    break;
                case 0xA080:
                    opcode = MojoShaderPreshaderOpcode.DivScalar;
                    break;
                case 0xD000:
                    opcode = MojoShaderPreshaderOpcode.DotScalar;
                    break;
                case 0xD020:
                    opcode = MojoShaderPreshaderOpcode.NoiseScalar;
                    break;
                default:
                    Fail("Unknown preshader opcode.");
                    break;
            }

            var operandCount = fxlc.Tokens[1] + 1; // +1 for dest.

            inst.Opcode = opcode;
            inst.ElementCount = opcodeTok & 0xFF;

            fxlc.TokenOffset += 2;
            if (operandCount * 3 > fxlc.TokenCount)
            {
                Fail("Bogus preshader FXLC block.");
                return;
            }

            while (operandCount-- > 0)
            {
                var operand = new MojoShaderPreshaderOperand();
                inst.Operands.Add(operand);
                var item = fxlc.Tokens[2];

                // !!! FIXME: Is this used anywhere other than INPUT? -flibit
                var numArrays = fxlc.Tokens[0];
                switch (fxlc.Tokens[1])
                {
                    case 1: // literal from CLIT block.
                    {
                        if (item > preshader.Literals.Count)
                        {
                            Fail("Bogus preshader literal index.");
                            break;
                        }

                        operand.Type = MojoShaderPreshaderOperandType.Literal;
                        break;
                    }

                    case 2: // item from ctabdata.
                    {
                        var sym = cTabData.Symbols.FirstOrDefault(x =>
                        {
                            var @base = x.RegisterIndex * 4;
                            var count = x.RegisterCount * 4;
                            return @base <= item && @base + count > item;
                        });

                        if (sym == null)
                        {
                            Fail("Bogus preshader input index.");
                            break;
                        }

                        operand.Type = MojoShaderPreshaderOperandType.Input;
                        if (numArrays > 0)
                        {
                            // Get each register base, indicating the arrays used.
                            // !!! FIXME: fail if fxlc.tokcount*2 > numarrays ?
                            for (var i = 0; i < numArrays; i++)
                            {
                                var jmp = fxlc.Tokens[4];
                                var bigJmp = (jmp >> 4) * 4;
                                var ltlJmp = (jmp >> 2) & 3;
                                operand.ArrayRegisters.Add(bigJmp + ltlJmp);
                                fxlc.TokenOffset += 2;
                            }
                        }

                        break;
                    }

                    case 4:
                    {
                        operand.Type = MojoShaderPreshaderOperandType.Output;

                        int i;
                        for (i = 0; i < outputMapCount; i++)
                        {
                            var @base = outputMap[(i * 2)] * 4;
                            var count = outputMap[(i * 2) + 1] * 4;
                            if ((@base <= item) && ((@base + count) > item))
                                break;
                        }

                        if (i == outputMapCount)
                        {
                            if (prsi.Seen) // No PRSI tokens, no output map.
                                Fail("Bogus preshader output index.");
                        }

                        break;
                    } // case

                    case 7:
                    {
                        operand.Type = MojoShaderPreshaderOperandType.Temp;
                        if (item >= preshader.TempCount)
                            preshader.TempCount = item + 1;
                        break;
                    } // case

                    default:
                        Debug.WriteLine("Unhandled fxlc.tokens[1] in parse_preshader!");
                        break;
                } // switch

                operand.Index = item;

                fxlc.TokenOffset += 3;
            }
        }

        // Registers need to be vec4, round up to nearest 4
        preshader.TempCount = (preshader.TempCount + 3) & ~3;
    }

    /// <summary>
    /// [parse_end_token; mojoshader.c]
    /// </summary>
    public int ParseEndToken(
    )
    {
        if (Tokens[0] != 0x0000FFFF) // end token always 0x0000FFFF.
            return 0; // not us, eat no tokens.

        if (!KnowShaderSize) // this is the end of stream!
            ShaderSize = CurrentPosition + 1;
        else if (TokensRemaining != 1) // we _must_ be last. If not: fail.
            Fail("end token before end of stream");

        if (!IsFail)
            Profile.EmitEnd(this);

        return 1;
    }

    /// <summary>
    /// [parse_phase_token; mojoshader.c]
    /// </summary>
    public int ParsePhaseToken(
    )
    {
        // !!! FIXME: needs state; allow only one phase token per shader, I think?
        if (Tokens[0] != 0x0000FFFD) // phase token always 0x0000FFFD.
            return 0; // not us, eat no tokens.

        if (!ShaderIsPixel() || !ShaderVersionExactly(1, 4))
            Fail("phase token only available in 1.4 pixel shaders");

        if (!IsFail)
            Profile.EmitPhase(this);

        return 1;
    }

    /// <summary>
    /// [parse_token; mojoshader.c]
    /// </summary>
    public int ParseToken(
    )
    {
        int rc;

        Debug.Assert(OutputStack.Count == 0);

        if (TokensRemaining < 1)
            Fail("unexpected end of shader.");

        else if ((rc = ParseCommentToken()) != 0)
            return rc;

        else if ((rc = ParseEndToken()) != 0)
            return rc;

        else if ((rc = ParsePhaseToken()) != 0)
            return rc;

        else if ((rc = ParseInstructionToken()) != 0)
            return rc;

        Fail("unknown token (0x{0:X})", Tokens[0]);
        return 1; // good luck!
    }

    // D3D stuff that's used in more than just the d3d profile...

    /// <summary>
    /// [reglist_exists; mojoshader.c]
    /// </summary>
    public MojoShaderRegister? RegListExists(
        List<MojoShaderRegister> list,
        MojoShaderRegisterType regType,
        int regNum) =>
        RegListFind(list, regType, regNum);

    /// <summary>
    /// [register_was_written; mojoshader.c]
    /// </summary>
    public bool RegisterWasWritten(
        MojoShaderRegisterType rType,
        int regNum) =>
        RegListFind(UsedRegisters, rType, regNum)?.Written ?? false;

    /// <summary>
    /// [get_defined_register; mojoshader.c]
    /// </summary>
    public bool GetDefinedRegister(
        MojoShaderRegisterType rType,
        int regNum) =>
        RegListExists(DefinedRegisters, rType, regNum) != null;

    /// <summary>
    /// [add_attribute_register; mojoshader.c]
    /// </summary>
    public void AddAttributeRegister(
        MojoShaderRegisterType rType,
        int regNum,
        MojoShaderUsage usage,
        int index,
        int writeMask,
        int flags)
    {
        var item = RegListInsert(Attributes, rType, regNum);
        item.Usage = usage;
        item.Index = index;
        item.WriteMask = writeMask;
        item.Misc = flags;

        // note that we have to check this later.
        switch (rType)
        {
            case MojoShaderRegisterType.Output when usage == MojoShaderUsage.PointSize:
                UsesPointSize = true;
                break;
            case MojoShaderRegisterType.Output when usage == MojoShaderUsage.Fog:
                UsesFog = true;
                break;
        }
    }

    /// <summary>
    /// [determine_constants_arrays; mojoshader.c]
    /// </summary>
    public void DetermineConstantsArrays(
    )
    {
        // Only process this stuff once. This is called after all DEF* opcodes
        //  could have been parsed.
        if (DeterminedConstantsArrays)
            return;

        DeterminedConstantsArrays = true;

        if (Constants.Count <= 1)
            return; // nothing to sort or group.

        // [Saxxon] the linked list gets sorted here in the original code.
        // We leverage .NET's collections here.

        var registerGroups = Constants
            .Where(x => x.Type == MojoShaderUniformType.Float) // we only care about REG_TYPE_CONST for array groups.
            .GroupBy(x => x.Index);

        foreach (var group in registerGroups)
        {
            var v = new MojoShaderVariable
            {
                Type = MojoShaderUniformType.Float,
                Index = group.Key,
                Constants = group.ToList(),
                Used = false,
                EmitPosition = -1
            };
            Variables.Add(v);
        }
    }

    /// <summary>
    /// [shader_model_1_input_usage; mojoshader.c]
    /// </summary>
    public (MojoShaderUsage Usage, int Index) ShaderModel1InputUsage(
        int regNum) =>
        regNum switch
        {
            0 => (MojoShaderUsage.Position, 0),
            1 => (MojoShaderUsage.BlendWeight, 0),
            2 => (MojoShaderUsage.BlendIndices, 0),
            3 => (MojoShaderUsage.Normal, 0),
            4 => (MojoShaderUsage.PointSize, 0),
            5 => (MojoShaderUsage.Color, 0),
            6 => (MojoShaderUsage.Color, 1),
            7 => (MojoShaderUsage.TexCoord, 0),
            8 => (MojoShaderUsage.TexCoord, 1),
            9 => (MojoShaderUsage.TexCoord, 2),
            10 => (MojoShaderUsage.TexCoord, 3),
            11 => (MojoShaderUsage.TexCoord, 4),
            12 => (MojoShaderUsage.TexCoord, 5),
            13 => (MojoShaderUsage.TexCoord, 6),
            14 => (MojoShaderUsage.TexCoord, 7),
            15 => (MojoShaderUsage.Position, 1),
            16 => (MojoShaderUsage.Normal, 1),
            _ => (MojoShaderUsage.Unknown, 0)
        };

    /// <summary>
    /// [adjust_swizzle; mojoshader.c]
    /// </summary>
    public MojoShaderSwizzleValue AdjustSwizzle(
        MojoShaderRegisterType regType,
        int regNum,
        int swizzle)
    {
        if (regType != MojoShaderRegisterType.Input)
            return swizzle;
        if (Swizzles.Count < 1)
            return swizzle;

        MojoShaderUsage usage;
        int index;

        if (!ShaderVersionAtLeast(2, 0))
        {
            (usage, index) = ShaderModel1InputUsage(regNum);
        }
        else
        {
            var reg = RegListFind(Attributes, regType, regNum);
            if (reg == null)
                return swizzle;
            usage = reg.Usage;
            index = reg.Index;
        }

        if (usage == MojoShaderUsage.Unknown)
            return swizzle;

        if (Swizzles.FirstOrDefault(s => s.Usage == usage && s.Index == index) is { } swiz)
        {
            return (swiz.Swizzles[(swizzle >> 0) & 0x3] << 0) |
                   (swiz.Swizzles[(swizzle >> 2) & 0x3] << 2) |
                   (swiz.Swizzles[(swizzle >> 4) & 0x3] << 4) |
                   (swiz.Swizzles[(swizzle >> 6) & 0x3] << 6);
        }

        return swizzle;
    }

    /// <summary>
    /// [parse_source_token; mojoshader.c]
    /// </summary>
    public (MojoShaderSourceArgInfo? Info, int Size) ParseSourceToken()
    {
        var size = 1;

        if (TokensRemaining < 1)
        {
            Fail("Out of tokens in source parameter");
            return (null, 0);
        }

        var token = Tokens[0];
        var reserved1 = (token >> 14) & 0x3;
        var reserved2 = (token >> 31) & 0x1;
        var swizzle = (token >> 16) & 0xFF;

        var info = new MojoShaderSourceArgInfo
        {
            Token = token,
            RegNum = token & 0x7FF,
            Relative = ((token >> 13) & 0x1) != 0,
            SrcMod = (MojoShaderSourceMod)((token >> 24) & 0xF),
            RegType = (MojoShaderRegisterType)(((token >> 28) & 0x7) | ((token >> 8) & 0x18))
        };

        // all the REG_TYPE_CONSTx types are the same register type, it's just
        //  split up so its regnum can be > 2047 in the bytecode. Clean it up.
        switch (info.RegType)
        {
            case MojoShaderRegisterType.Const2:
                info.RegType = MojoShaderRegisterType.Const;
                info.RegNum += 2048;
                break;
            case MojoShaderRegisterType.Const3:
                info.RegType = MojoShaderRegisterType.Const;
                info.RegNum += 4096;
                break;
            case MojoShaderRegisterType.Const4:
                info.RegType = MojoShaderRegisterType.Const;
                info.RegNum += 6144;
                break;
        }

        info.Swizzle = AdjustSwizzle(info.RegType, info.RegNum, swizzle);

        // swallow token for now, for multiple calls in a row.
        AdjustTokenPosition(1);

        if (reserved1 != 0x0)
            Fail("Reserved bits #1 in source token must be zero");

        if (reserved2 != 0x1)
            Fail("Reserved bit #2 in source token must be one");

        if (info.Relative && TokensRemaining < 1)
        {
            Fail("Out of tokens in relative source parameter");
            info.Relative = false; // don't try to process it.
        }

        if (info.Relative)
        {
            if (ShaderIsPixel() && !ShaderVersionAtLeast(3, 0))
                Fail("Relative addressing in pixel shader version < 3.0");

            // Shader Model 1 doesn't have an extra token to specify the
            //  relative register: it's always a0.x.
            if (!ShaderVersionAtLeast(2, 0))
            {
                info.RelativeRegNum = 0;
                info.RelativeRegType = MojoShaderRegisterType.Address;
                info.RelativeComponent = 0;
            }

            else // Shader Model 2 and later...
            {
                var relToken = Tokens[0];
                // swallow token for now, for multiple calls in a row.
                AdjustTokenPosition(1);

                MojoShaderSwizzleValue relSwiz = (relToken >> 16) & 0xFF;
                info.RelativeRegNum = relToken & 0x7FF;
                info.RelativeRegType = (MojoShaderRegisterType)
                    (((relToken >> 28) & 0x7) |
                     ((relToken >> 8) & 0x18));

                if (((relToken >> 31) & 0x1) == 0)
                    Fail("bit #31 in relative address must be set");

                if ((relToken & 0xF00E000) != 0) // unused bits.
                    Fail("relative address reserved bit must be zero");

                switch (info.RelativeRegType)
                {
                    case MojoShaderRegisterType.Loop:
                    case MojoShaderRegisterType.Address:
                        break;
                    default:
                        Fail("invalid register for relative address");
                        break;
                }

                if (info.RelativeRegNum != 0) // true for now.
                    Fail("invalid register for relative address");

                if ((info.RelativeRegType != MojoShaderRegisterType.Loop) && !relSwiz.IsReplicate)
                    Fail("relative address needs replicate swizzle");

                info.RelativeComponent = relSwiz & 0x3;
                size++;
            }

            if (info.RegType == MojoShaderRegisterType.Input)
            {
                if (ShaderIsPixel() || !ShaderVersionAtLeast(3, 0))
                    Fail("relative addressing of input registers not supported in this shader model");
                HaveRelativeInputRegisters = true;
            }
            else if (info.RegType == MojoShaderRegisterType.Const)
            {
                // figure out what array we're in...
                if (!IgnoresCtab)
                {
                    if (!Ctab?.HaveCtab ?? false) // hard to do efficiently without!
                        Fail("relative addressing unsupported without a CTAB");
                    else
                    {
                        DetermineConstantsArrays();

                        var relTarget = info.RegNum;

                        if (Variables.FirstOrDefault(x =>
                                relTarget >= x.Index && relTarget < (x.Index + x.Count)) is
                            { } v)
                        {
                            v.Used = true;
                            info.RelativeArray.Add(v);
                            SetUsedRegister(info.RelativeRegType, info.RelativeRegNum, false);
                        }
                        else
                        {
                            Fail("relative addressing of indeterminate array");
                        }
                    } // else
                } // if
            }
        }

        switch (info.SrcMod)
        {
            case MojoShaderSourceMod.None:
            case MojoShaderSourceMod.AbsNegate:
            case MojoShaderSourceMod.Abs:
            case MojoShaderSourceMod.Negate:
                break; // okay in any shader model.

            // apparently these are only legal in Shader Model 1.x ...
            case MojoShaderSourceMod.BiasNegate:
            case MojoShaderSourceMod.Bias:
            case MojoShaderSourceMod.SignNegate:
            case MojoShaderSourceMod.Sign:
            case MojoShaderSourceMod.Complement:
            case MojoShaderSourceMod.X2Negate:
            case MojoShaderSourceMod.X2:
            case MojoShaderSourceMod.Dz:
            case MojoShaderSourceMod.Dw:
                if (ShaderVersionAtLeast(2, 0))
                    Fail("illegal source mod for this Shader Model.");
                break;

            case MojoShaderSourceMod.Not: // !!! FIXME: I _think_ this is right...
                if (ShaderVersionAtLeast(2, 0))
                {
                    if (info.RegType != MojoShaderRegisterType.Predicate
                        && info.RegType != MojoShaderRegisterType.ConstBool)
                        Fail("NOT only allowed on bool registers.");
                }

                break;

            default:
                Fail("Unknown source modifier");
                break;
        }

        // !!! FIXME: docs say this for sm3 ... check these!
        //  "The negate modifier cannot be used on second source register of these
        //   instructions: m3x2 - ps, m3x3 - ps, m3x4 - ps, m4x3 - ps, and
        //   m4x4 - ps."
        //  "If any version 3 shader reads from one or more constant float
        //   registers (c#), one of the following must be true.
        //    All of the constant floating-point registers must use the abs modifier.
        //    None of the constant floating-point registers can use the abs modifier.

        if (!IsFail)
        {
            var reg = SetUsedRegister(info.RegType, info.RegNum, false);
            // !!! FIXME: this test passes if you write to the register
            // !!! FIXME:  in this same instruction, because we parse the
            // !!! FIXME:  destination token first.
            // !!! FIXME: Microsoft's shader validation explicitly checks temp
            // !!! FIXME:  registers for this...do they check other writable ones?
            if (info.RegType == MojoShaderRegisterType.Temp && reg is { Written: false })
                Fail("Temp register r{0} used uninitialized", info.RegNum);
        }

        return (info, size);
    }

    /// <summary>
    /// [valid_texture_type; mojoshader.c]
    /// </summary>
    public bool ValidTextureType(
        int tType) =>
        (MojoShaderTextureType)tType switch
        {
            MojoShaderTextureType.TwoD => true,
            MojoShaderTextureType.Cube => true,
            MojoShaderTextureType.Volume => true,
            _ => false
        };

    /// <summary>
    /// [srcarg_matrix_replicate; mojoshader.c]
    /// </summary>
    public void SrcArgMatrixReplicate(
        int idx, int rows)
    {
        var src = SourceArgs[idx] ?? new MojoShaderSourceArgInfo();

        for (var i = 0; i < rows - 1; i++)
        {
            var item = src.Clone();
            item.RegNum = i + 1;
            SetUsedRegister(item.RegType, item.RegNum, false);
        }
    }

    /// <summary>
    /// [check_label_register; mojoshader.c]
    /// </summary>
    public void CheckLabelRegister(
        int arg,
        string opcode)
    {
        var info = SourceArgs[arg] ?? new MojoShaderSourceArgInfo();
        var regType = info.RegType;
        var regNum = info.RegNum;

        if (regType != MojoShaderRegisterType.Label)
            Fail("{0} with a non-label register specified", opcode);
        if (!ShaderVersionAtLeast(2, 0))
            Fail("{0} not supported in Shader Model 1", opcode);
        if (ShaderVersionAtLeast(2, 255) && regNum > 2047)
            Fail("label register number must be <= 2047");
        if (regNum > 15)
            Fail("label register number must be <= 15");
    }

    /// <summary>
    /// [check_call_loop_wrappage; mojoshader.c]
    /// </summary>
    public void CheckCallLoopWrappage(
        int regNum)
    {
        // msdn says subroutines inherit aL register if you're in a loop when
        //  you call, and further more _if you ever call this function in a loop,
        //  it must always be called in a loop_. So we'll just pass our loop
        //  variable as a function parameter in those cases.

        var currentUsage = Loops > 0 ? 1 : -1;
        var reg = RegListFind(UsedRegisters, MojoShaderRegisterType.Label, regNum);

        if (reg == null)
            Fail("Invalid label for CALL");
        else if (reg.Misc == 0)
            reg.Misc = currentUsage;
        else if (reg.Misc != currentUsage)
        {
            Fail(currentUsage == 1
                ? "CALL to this label must be wrapped in LOOP/ENDLOOP"
                : "CALL to this label must not be wrapped in LOOP/ENDLOOP");
        }
    }

    /// <summary>
    /// [is_comment_token; mojoshader.c]
    /// </summary>
    public (bool Result, int Count) IsCommentToken(
        int token)
    {
        if ((token & 0xFFFF) == 0xFFFE) // actually a comment token?
        {
            if ((token & 0x80000000) != 0)
                Fail("comment token high bit must be zero."); // so says msdn.
            return (true, (token >> 16) & 0xFFFF);
        }

        return (false, 0);
    }

    /// <summary>
    /// [alloc_varname; mojoshader.c]
    /// </summary>
    public string? AllocVarName(MojoShaderRegister reg) =>
        Profile.GetVarName(this, reg.RegType, reg.RegNum);

    /// <summary>
    /// [process_definitions; mojoshader.c]
    /// </summary>
    public void ProcessDefinitions()
    {
        // !!! FIXME: apparently, pre ps_3_0, sampler registers don't need to be
        // !!! FIXME:  DCL'd before use (default to 2d?). We aren't checking
        // !!! FIXME:  this at the moment, though.

        DetermineConstantsArrays(); // in case this hasn't been called yet.

        var uniforms = Uniforms;

        foreach (var item in UsedRegisters)
        {
            var regType = item.RegType;
            var regNum = item.RegNum;

            if (!GetDefinedRegister(regType, regNum))
            {
                // haven't already dealt with this one.
                MojoShaderUsage usage;
                switch (regType)
                {
                    // !!! FIXME: I'm not entirely sure this is right...
                    case MojoShaderRegisterType.RastOut:
                    case MojoShaderRegisterType.AttrOut:
                    case MojoShaderRegisterType.TexCrdOut:
                    case MojoShaderRegisterType.ColorOut:
                    case MojoShaderRegisterType.DepthOut:
                        if (ShaderIsVertex() && ShaderVersionAtLeast(3, 0))
                        {
                            Fail("vs_3 can't use output registers without declaring them first.");
                            return;
                        }

                        usage = regType switch
                        {
                            // Apparently this is an attribute that wasn't DCL'd.
                            //  Add it to the attribute list; deal with it later.
                            MojoShaderRegisterType.RastOut => (MojoShaderRastOutType)regNum switch
                            {
                                MojoShaderRastOutType.Position => MojoShaderUsage.Position,
                                MojoShaderRastOutType.Fog => MojoShaderUsage.Fog,
                                MojoShaderRastOutType.PointSize => MojoShaderUsage.PointSize,
                                _ => MojoShaderUsage.Unknown
                            },
                            MojoShaderRegisterType.AttrOut or MojoShaderRegisterType.ColorOut => MojoShaderUsage.Color,
                            MojoShaderRegisterType.TexCrdOut => MojoShaderUsage.TexCoord,
                            MojoShaderRegisterType.DepthOut => MojoShaderUsage.Depth,
                            _ => MojoShaderUsage.Unknown
                        };

                        AddAttributeRegister(regType, regNum, usage, regNum, 0xF, 0);
                        break;

                    case MojoShaderRegisterType.Address:
                    case MojoShaderRegisterType.Predicate:
                    case MojoShaderRegisterType.Temp:
                    case MojoShaderRegisterType.Loop:
                    case MojoShaderRegisterType.Label:
                        Profile.EmitGlobal(this, regType, regNum);
                        break;

                    case MojoShaderRegisterType.Const:
                    case MojoShaderRegisterType.ConstInt:
                    case MojoShaderRegisterType.ConstBool:
                        // separate uniforms into a different list for now.
                        uniforms.Add(item);
                        break;

                    case MojoShaderRegisterType.Input:
                        // You don't have to dcl_ your inputs in Shader Model 1.
                        if (!ShaderVersionAtLeast(2, 0))
                        {
                            if (ShaderIsPixel())
                            {
                                AddAttributeRegister(regType, regNum, MojoShaderUsage.Color, regNum, 0xF, 0);
                                break;
                            }

                            if (ShaderIsVertex())
                            {
                                (usage, var index) = ShaderModel1InputUsage(regNum);
                                if (usage != MojoShaderUsage.Unknown)
                                {
                                    AddAttributeRegister(regType, regNum, usage, index, 0xF, 0);
                                    break;
                                }
                            }
                        }

                        Fail("BUG: we used a register we don't know how to define.");
                        break;
                    default:
                        Fail("BUG: we used a register we don't know how to define.");
                        break;
                }
            }
        }

        // okay, now deal with uniform/constant arrays...
        foreach (var var in Variables.Where(var => var.Used))
        {
            if (var.Constants.Count > 0)
            {
                Profile.EmitConstArray(this, var.Constants, var.Index, var.Count);
            }
            else
            {
                Profile.EmitArray(this, var);
                UniformFloat4Count += var.Count;
            }
        }

        // ...and uniforms...
        foreach (var item in uniforms)
        {
            var arraySize = -1;
            MojoShaderVariable? var1 = null;

            // check if this is a register contained in an array...
            if (item.RegType == MojoShaderRegisterType.Const)
            {
                foreach (var var in Variables)
                {
                    var1 = var;
                    if (!var.Used)
                        continue;

                    var regNum = item.RegNum;
                    var lo = var.Index;
                    if (regNum >= lo && regNum < lo + var.Count)
                    {
                        Debug.Assert(var.Constants.Count > 0);
                        item.Array.Add(var); // used when building parseData.
                        arraySize = var.Count;
                        break;
                    }
                }
            }

            Profile.EmitUniform(this, item.RegType, item.RegNum, var1!);

            if (arraySize < 0) // not part of an array?
            {
                switch (item.RegType)
                {
                    case MojoShaderRegisterType.Const:
                        UniformFloat4Count++;
                        break;
                    case MojoShaderRegisterType.ConstInt:
                        UniformInt4Count++;
                        break;
                    case MojoShaderRegisterType.ConstBool:
                        UniformBoolCount++;
                        break;
                }
            }
        }

        // ...and samplers...
        foreach (var item in Samplers)
        {
            Profile.EmitSampler(this, item.RegNum, (MojoShaderTextureType)item.Index, item.Misc != 0);
        }

        // ...and attributes...
        foreach (var item in Attributes)
        {
            Profile.EmitAttribute(this, item.RegType, item.RegNum, item.Usage, item.Index, item.WriteMask,
                item.Misc);
        }
    }

    public void State(MojoShaderOpcode op) =>
        (op switch
        {
            MojoShaderOpcode.Def => StateDef,
            MojoShaderOpcode.DefI => StateDefI,
            MojoShaderOpcode.DefB => StateDefB,
            MojoShaderOpcode.Dcl => StateDcl,
            MojoShaderOpcode.TexCrd => StateTexCrd,
            MojoShaderOpcode.Frc => StateFrc,
            MojoShaderOpcode.M4x4 => StateM4X4,
            MojoShaderOpcode.M4x3 => StateM4X3,
            MojoShaderOpcode.M3x4 => StateM3X4,
            MojoShaderOpcode.M3x3 => StateM3X3,
            MojoShaderOpcode.M3x2 => StateM3X2,
            MojoShaderOpcode.Ret => StateRet,
            MojoShaderOpcode.Label => StateLabel,
            MojoShaderOpcode.Call => StateCall,
            MojoShaderOpcode.CallNz => StateCallNz,
            MojoShaderOpcode.MovA => StateMovA,
            MojoShaderOpcode.Rcp => StateRcp,
            MojoShaderOpcode.Rsq => StateRsq,
            MojoShaderOpcode.Loop => StateLoop,
            MojoShaderOpcode.EndLoop => StateEndLoop,
            MojoShaderOpcode.BreakP => StateBreakP,
            MojoShaderOpcode.Break => StateBreak,
            MojoShaderOpcode.SetP => StateSetP,
            MojoShaderOpcode.Rep => StateRep,
            MojoShaderOpcode.EndRep => StateEndRep,
            MojoShaderOpcode.Cmp => StateCmp,
            MojoShaderOpcode.Dp4 => StateDp4,
            MojoShaderOpcode.Cnd => StateCnd,
            MojoShaderOpcode.Pow => StatePow,
            MojoShaderOpcode.Log => StateLog,
            MojoShaderOpcode.LogP => StateLogP,
            MojoShaderOpcode.SinCos => StateSinCos,
            MojoShaderOpcode.If => StateIf,
            MojoShaderOpcode.Ifc => StateIfc,
            MojoShaderOpcode.BreakC => StateBreakC,
            MojoShaderOpcode.TexKill => StateTexKill,
            MojoShaderOpcode.TexBem => StateTexBem,
            MojoShaderOpcode.TexBeml => StateTexBemL,
            MojoShaderOpcode.TexM3x2Pad => StateTexM3X2Pad,
            MojoShaderOpcode.TexM3x2Tex => StateTexM3X2Tex,
            MojoShaderOpcode.TexM3x3Pad => StateTexM3X3Pad,
            MojoShaderOpcode.TexM3x3 => StateTexM3X3,
            MojoShaderOpcode.TexM3x3Tex => StateTexM3X3Tex,
            MojoShaderOpcode.TexM3x3Spec => StateTexM3X3Spec,
            MojoShaderOpcode.TexM3x3Vspec => StateTexM3X3VSpec,
            MojoShaderOpcode.TexLd => StateTexLd,
            MojoShaderOpcode.TexLdl => StateTexLdl,
            MojoShaderOpcode.Dp2Add => StateDp2Add,
            _ => (Action)StateNone
        })();

    private void StateNone()
    {
    }

    /// <summary>
    /// [state_DEF; mojoshader.c]
    /// </summary>
    public void StateDef()
    {
        var regType = DestArg?.RegType ?? MojoShaderRegisterType.Invalid;
        var regNum = DestArg?.RegNum ?? 0;

        // !!! FIXME: fail if same register is defined twice.

        if (InstructionCount > 0)
            Fail("DEF token must come before any instructions");
        else if (regType != MojoShaderRegisterType.Const)
            Fail("DEF token using invalid register");
        else
        {
            var item = new MojoShaderConstant
            {
                Index = regNum,
                Type = MojoShaderUniformType.Float
            };

            Dwords.CopyTo(item.Value.AsSpan());
            Constants.Add(item);
            SetDefinedRegister(regType, regNum);
        }
    }

    /// <summary>
    /// [state_DEFI; mojoshader.c]
    /// </summary>
    public void StateDefI()
    {
        var regType = DestArg?.RegType ?? MojoShaderRegisterType.Invalid;
        var regNum = DestArg?.RegNum ?? 0;

        // !!! FIXME: fail if same register is defined twice.

        if (InstructionCount > 0)
            Fail("DEFI token must come before any instructions");
        else if (regType != MojoShaderRegisterType.Const)
            Fail("DEFI token using invalid register");
        else
        {
            var item = new MojoShaderConstant
            {
                Index = regNum,
                Type = MojoShaderUniformType.Int
            };

            Dwords.CopyTo(item.Value.AsSpan());
            Constants.Add(item);
            SetDefinedRegister(regType, regNum);
        }
    }

    /// <summary>
    /// [state_DEFB; mojoshader.c]
    /// </summary>
    public void StateDefB()
    {
        var regType = DestArg?.RegType ?? MojoShaderRegisterType.Invalid;
        var regNum = DestArg?.RegNum ?? 0;

        // !!! FIXME: fail if same register is defined twice.

        if (InstructionCount > 0)
            Fail("DEFB token must come before any instructions");
        else if (regType != MojoShaderRegisterType.Const)
            Fail("DEFB token using invalid register");
        else
        {
            var item = new MojoShaderConstant
            {
                Index = regNum,
                Type = MojoShaderUniformType.Bool,
                Value =
                {
                    [0] = Dwords[0] != 0 ? ~0 : 0
                }
            };

            Constants.Add(item);
            SetDefinedRegister(regType, regNum);
        }
    }

    /// <summary>
    /// [state_DCL; mojoshader.c]
    /// </summary>
    public void StateDcl()
    {
        var arg = DestArg;
        var regType = arg?.RegType ?? MojoShaderRegisterType.Invalid;
        var regNum = arg?.RegNum ?? 0;
        var wMask = arg?.WriteMask ?? 0xF;
        var mods = arg?.ResultMod ?? default;

        // parse_args_DCL() does a lot of state checking before we get here.

        // !!! FIXME: apparently vs_3_0 can use sampler registers now.
        // !!! FIXME:  (but only s0 through s3, not all 16 of them.)

        if (InstructionCount > 0)
            Fail("DCL token must come before any instructions");

        else if (ShaderIsVertex() || ShaderIsPixel())
        {
            if (regType == MojoShaderRegisterType.Sampler)
                AddSampler(regNum, (MojoShaderTextureType)Dwords[0], 0);
            else
            {
                var usage = (MojoShaderUsage)Dwords[0];
                var index = Dwords[1];
                if (usage >= MojoShaderUsage.Total)
                {
                    Fail("unknown DCL usage");
                    return;
                }

                AddAttributeRegister(regType, regNum, usage, index, wMask, (int)mods);
            }
        }

        else
        {
            Fail("unsupported shader type."); // should be caught elsewhere.
            return;
        }

        SetDefinedRegister(regType, regNum);
    }

    /// <summary>
    /// [state_TEXCRD; mojoshader.c]
    /// </summary>
    public void StateTexCrd()
    {
        if (ShaderVersionAtLeast(2, 0))
            Fail("TEXCRD in Shader Model >= 2.0"); // apparently removed.
    }

    /// <summary>
    /// [state_FRC; mojoshader.c]
    /// </summary>
    public void StateFrc()
    {
        var dst = DestArg ?? new MojoShaderDestArgInfo();

        if ((dst.ResultMod & MojoShaderMod.Saturate) != 0) // according to msdn...
            Fail("FRC destination can't use saturate modifier");

        else if (!ShaderVersionAtLeast(2, 0))
        {
            if (dst.WriteMask is { IsY: false, IsXy: false })
                Fail("FRC writemask must be .y or .xy for shader model 1.x");
        }
    }

    /// <summary>
    /// [state_M4X4; mojoshader.c]
    /// </summary>
    public void StateM4X4()
    {
        var info = DestArg ?? new MojoShaderDestArgInfo();
        if (!info.WriteMask.IsXyzw)
            Fail("M4X4 writemask must be full");

        // !!! FIXME: MSDN:
        //The xyzw (default) mask is required for the destination register. Negate and swizzle modifiers are allowed for
        //src0, but not for src1. Swizzle and negate modifiers are invalid for the src0 register. The dest and src0
        //registers cannot be the same.

        SrcArgMatrixReplicate(1, 4);
    }

    /// <summary>
    /// [state_M4X3; mojoshader.c]
    /// </summary>
    public void StateM4X3()
    {
        var info = DestArg ?? new MojoShaderDestArgInfo();
        if (!info.WriteMask.IsXyz)
            Fail("M4X3 writemask must be .xyz");

        // !!! FIXME: MSDN stuff

        SrcArgMatrixReplicate(1, 3);
    }

    /// <summary>
    /// [state_M3X4; mojoshader.c]
    /// </summary>
    public void StateM3X4()
    {
        var info = DestArg ?? new MojoShaderDestArgInfo();
        if (!info.WriteMask.IsXyzw)
            Fail("M3X4 writemask must be .xyzw");

        // !!! FIXME: MSDN stuff

        SrcArgMatrixReplicate(1, 4);
    }

    /// <summary>
    /// [state_M3X3; mojoshader.c]
    /// </summary>
    public void StateM3X3()
    {
        var info = DestArg ?? new MojoShaderDestArgInfo();
        if (!info.WriteMask.IsXyz)
            Fail("M3X3 writemask must be .xyz");

        // !!! FIXME: MSDN stuff

        SrcArgMatrixReplicate(1, 3);
    }

    /// <summary>
    /// [state_M3X3; mojoshader.c]
    /// </summary>
    public void StateM3X2()
    {
        var info = DestArg ?? new MojoShaderDestArgInfo();
        if (!info.WriteMask.IsXy)
            Fail("M3X2 writemask must be .xy");

        // !!! FIXME: MSDN stuff

        SrcArgMatrixReplicate(1, 3);
    }

    /// <summary>
    /// [state_RET; mojoshader.c]
    /// </summary>
    public void StateRet()
    {
        // MSDN all but says that assembly shaders are more or less serialized
        //  HLSL functions, and a RET means you're at the end of one, unlike how
        //  most CPUs would behave. This is actually really helpful,
        //  since we can use high-level constructs and not a mess of GOTOs,
        //  which is a godsend for GLSL...this also means we can consider things
        //  like a LOOP without a matching ENDLOOP within a label's section as
        //  an error.
        if (Loops > 0)
            Fail("LOOP without ENDLOOP");
        if (Reps > 0)
            Fail("REP without ENDREP");
    }

    /// <summary>
    /// [state_LABEL; mojoshader.c]
    /// </summary>
    public void StateLabel()
    {
        if (PreviousOpcode != MojoShaderOpcode.Ret)
            Fail("LABEL not followed by a RET");
        CheckLabelRegister(0, "LABEL");
        SetDefinedRegister(MojoShaderRegisterType.Label, SourceArgs[0]?.RegNum ?? 0);
    }

    /// <summary>
    /// [state_CALL; mojoshader.c]
    /// </summary>
    public void StateCall()
    {
        CheckLabelRegister(0, "CALL");
        CheckCallLoopWrappage(SourceArgs[0]?.RegNum ?? 0);
    }

    /// <summary>
    /// [state_CALLNZ; mojoshader.c]
    /// </summary>
    public void StateCallNz()
    {
        var regType = SourceArgs[1]?.RegType ?? MojoShaderRegisterType.Invalid;
        if (regType != MojoShaderRegisterType.ConstBool && regType != MojoShaderRegisterType.Predicate)
            Fail("CALLNZ argument isn't constbool or predicate register");
        CheckLabelRegister(0, "CALLNZ");
        CheckCallLoopWrappage(SourceArgs[0]?.RegNum ?? 0);
    }

    /// <summary>
    /// [state_MOVA; mojoshader.c]
    /// </summary>
    public void StateMovA()
    {
        if (DestArg?.RegType != MojoShaderRegisterType.Address)
            Fail("MOVA argument isn't address register");
    }

    /// <summary>
    /// [state_RCP; mojoshader.c]
    /// </summary>
    public void StateRcp()
    {
        if (!(SourceArgs[0]?.Swizzle.IsReplicate ?? false))
            Fail("RCP without replicate swizzle");
    }

    /// <summary>
    /// [state_RSQ; mojoshader.c]
    /// </summary>
    public void StateRsq()
    {
        if (!(SourceArgs[0]?.Swizzle.IsReplicate ?? false))
            Fail("RSQ without replicate swizzle");
    }

    /// <summary>
    /// [state_LOOP; mojoshader.c]
    /// </summary>
    public void StateLoop()
    {
        if (SourceArgs[0]?.RegType != MojoShaderRegisterType.Loop)
            Fail("LOOP argument isn't loop register");
        else if (SourceArgs[1]?.RegType != MojoShaderRegisterType.ConstInt)
            Fail("LOOP argument isn't constint register");
        else
            Loops++;
    }

    /// <summary>
    /// [state_ENDLOOP; mojoshader.c]
    /// </summary>
    public void StateEndLoop()
    {
        // !!! FIXME: check that we aren't straddling an IF block.
        if (Loops <= 0)
            Fail("ENDLOOP without LOOP");
        Loops--;
    }

    /// <summary>
    /// [state_BREAKP; mojoshader.c]
    /// </summary>
    public void StateBreakP()
    {
        var regType = SourceArgs[0]?.RegType ?? MojoShaderRegisterType.Invalid;
        if (regType != MojoShaderRegisterType.Predicate)
            Fail("BREAKP argument isn't predicate register");
        else if (!(SourceArgs[0]?.Swizzle.IsReplicate ?? false))
            Fail("BREAKP without replicate swizzle");
        else if (Loops == 0 && Reps == 0)
            Fail("BREAKP outside LOOP/ENDLOOP or REP/ENDREP");
    }

    /// <summary>
    /// [state_BREAK; mojoshader.c]
    /// </summary>
    public void StateBreak()
    {
        if (Loops == 0 && Reps == 0)
            Fail("BREAK outside LOOP/ENDLOOP or REP/ENDREP");
    }

    /// <summary>
    /// [state_SETP; mojoshader.c]
    /// </summary>
    public void StateSetP()
    {
        var regType = DestArg?.RegType ?? MojoShaderRegisterType.Invalid;
        if (regType != MojoShaderRegisterType.Predicate)
            Fail("SETP argument isn't predicate register");
    }

    /// <summary>
    /// [state_REP; mojoshader.c]
    /// </summary>
    public void StateRep()
    {
        var regType = SourceArgs[0]?.RegType ?? MojoShaderRegisterType.Invalid;
        if (regType != MojoShaderRegisterType.ConstInt)
            Fail("REP argument isn't constint register");

        Reps++;
        if (Reps > MaxReps)
            MaxReps = Reps;
    }

    /// <summary>
    /// [state_ENDREP; mojoshader.c]
    /// </summary>
    public void StateEndRep()
    {
        // !!! FIXME: check that we aren't straddling an IF block.
        if (Reps <= 0)
            Fail("ENDREP without REP");
        Reps--;
    }

    /// <summary>
    /// [state_CMP; mojoshader.c]
    /// </summary>
    public void StateCmp()
    {
        Cmps++;

        // extra limitations for ps < 1.4 ...
        if (ShaderVersionAtLeast(1, 4))
            return;

        int i;
        var dst = DestArg ?? new MojoShaderDestArgInfo();
        var dRegType = dst.RegType;
        var dRegNum = dst.RegNum;

        if (Cmps > 3)
            Fail("only 3 CMP instructions allowed in this shader model");

        for (i = 0; i < 3; i++)
        {
            var src = SourceArgs[i] ?? new MojoShaderSourceArgInfo();
            var sRegType = src.RegType;
            var sRegNum = src.RegNum;
            if (dRegType == sRegType && dRegNum == sRegNum)
                Fail("CMP dest can't match sources in this shader model");
        }

        InstructionCount++; // takes an extra slot in ps_1_2 and _3.
    }

    /// <summary>
    /// [state_DP4; mojoshader.c]
    /// </summary>
    public void StateDp4()
    {
        // extra limitations for ps < 1.4 ...
        if (!ShaderVersionAtLeast(1, 4))
            InstructionCount++; // takes an extra slot in ps_1_2 and _3.
    }

    /// <summary>
    /// [state_CND; mojoshader.c]
    /// </summary>
    public void StateCnd()
    {
        // apparently it was removed...it's not in the docs past ps_1_4 ...
        if (ShaderVersionAtLeast(2, 0))
            Fail("CND not allowed in this shader model");

        // extra limitations for ps <= 1.4 ...
        else if (!ShaderVersionAtLeast(1, 4))
        {
            var src = SourceArgs[0];
            if (src?.RegType != MojoShaderRegisterType.Temp ||
                src.RegNum != 0 ||
                src.Swizzle != 0xFF)
                Fail("CND src must be r0.a in this shader model");
        }
    }

    /// <summary>
    /// [state_POW; mojoshader.c]
    /// </summary>
    public void StatePow()
    {
        if (!(SourceArgs[0]?.Swizzle.IsReplicate ?? false))
            Fail("POW src0 must have replicate swizzle");
        else if (!(SourceArgs[1]?.Swizzle.IsReplicate ?? false))
            Fail("POW src1 must have replicate swizzle");
    }

    /// <summary>
    /// [state_LOG; mojoshader.c]
    /// </summary>
    public void StateLog()
    {
        if (!(SourceArgs[0]?.Swizzle.IsReplicate ?? false))
            Fail("LOG src0 must have replicate swizzle");
    }

    /// <summary>
    /// [state_LOGP; mojoshader.c]
    /// </summary>
    public void StateLogP()
    {
        if (!(SourceArgs[0]?.Swizzle.IsReplicate ?? false))
            Fail("LOGP src0 must have replicate swizzle");
    }

    /// <summary>
    /// [state_SINCOS; mojoshader.c]
    /// </summary>
    public void StateSinCos()
    {
        var dst = DestArg ?? new MojoShaderDestArgInfo();
        var mask = dst.WriteMask;
        if (mask is { IsX: false, IsY: false, IsXy: false })
            Fail("SINCOS write mask must be .x or .y or .xy");

        else if (!(SourceArgs[0]?.Swizzle.IsReplicate ?? false))
            Fail("SINCOS src0 must have replicate swizzle");

        else if ((dst.ResultMod & MojoShaderMod.Saturate) != 0) // according to msdn...
            Fail("SINCOS destination can't use saturate modifier");

        // this opcode needs extra registers, with extra limitations, for <= sm2.
        else if (!ShaderVersionAtLeast(3, 0))
        {
            int i;
            for (i = 1; i < 3; i++)
            {
                if (SourceArgs[i]?.RegType != MojoShaderRegisterType.Const)
                {
                    Fail("SINCOS src{0} must be constfloat", i);
                    return;
                }
            }

            if (SourceArgs[1]?.RegNum == SourceArgs[2]?.RegNum)
                Fail("SINCOS src1 and src2 must be different registers");
        }
    }

    /// <summary>
    /// [state_IF; mojoshader.c]
    /// </summary>
    public void StateIf()
    {
        var regType = SourceArgs[0]?.RegType;
        if (regType != MojoShaderRegisterType.Predicate && regType != MojoShaderRegisterType.ConstBool)
            Fail("IF src0 must be CONSTBOOL or PREDICATE");
        // !!! FIXME: track if nesting depth.
    }

    /// <summary>
    /// [state_IFC; mojoshader.c]
    /// </summary>
    public void StateIfc()
    {
        if (!(SourceArgs[0]?.Swizzle.IsReplicate ?? false))
            Fail("IFC src0 must have replicate swizzle");
        else if (!(SourceArgs[1]?.Swizzle.IsReplicate ?? false))
            Fail("IFC src1 must have replicate swizzle");
        // !!! FIXME: track if nesting depth.
    }

    /// <summary>
    /// [state_BREAKC; mojoshader.c]
    /// </summary>
    public void StateBreakC()
    {
        if (!(SourceArgs[0]?.Swizzle.IsReplicate ?? false))
            Fail("BREAKC src1 must have replicate swizzle");
        else if (!(SourceArgs[1]?.Swizzle.IsReplicate ?? false))
            Fail("BREAKC src2 must have replicate swizzle");
        else if (Loops == 0 && Reps == 0)
            Fail("BREAKC outside LOOP/ENDLOOP or REP/ENDREP");
    }

    /// <summary>
    /// [state_TEXKILL; mojoshader.c]
    /// </summary>
    public void StateTexKill()
    {
        // The MSDN docs say this should be a source arg, but the driver docs
        //  say it's a dest arg. That's annoying.
        var info = DestArg ?? new MojoShaderDestArgInfo();
        var regType = info.RegType;
        if (!info.WriteMask.IsXyzw)
            Fail("TEXKILL writemask must be .xyzw");
        else if (regType != MojoShaderRegisterType.Temp && regType != MojoShaderRegisterType.Texture)
            Fail("TEXKILL must use a temp or texture register");

        // !!! FIXME: "If a temporary register is used, all components must have been previously written."
        // !!! FIXME: "If a texture register is used, all components that are read must have been declared."
        // !!! FIXME: there are further limitations in ps_1_3 and earlier.
    }

    /// <summary>
    /// [state_texops; mojoshader.c]
    /// </summary>
    public void StateTexOps(
        string opcode,
        int dims,
        int texBem)
    {
        var dst = DestArg ?? new MojoShaderDestArgInfo();
        var src = SourceArgs[0] ?? new MojoShaderSourceArgInfo();
        if (dst.RegType != MojoShaderRegisterType.Texture)
            Fail("{0} destination must be a texture register", opcode);
        if (src.RegType != MojoShaderRegisterType.Texture)
            Fail("{0} source must be a texture register", opcode);
        if (src.RegNum >= dst.RegNum) // so says MSDN.
            Fail("{0} dest must be a higher register than source", opcode);

        if (dims != 0)
        {
            var tType = dims == 2
                ? MojoShaderTextureType.TwoD
                : MojoShaderTextureType.Cube;

            AddSampler(dst.RegNum, tType, texBem);
        }

        AddAttributeRegister(MojoShaderRegisterType.Texture, dst.RegNum,
            MojoShaderUsage.TexCoord, dst.RegNum, 0xF, 0);

        // Strictly speaking, there should be a TEX opcode prior to this call that
        //  should fill in this metadata, but I'm not sure that's required for the
        //  shader to assemble in D3D, so we'll do this so we don't fail with a
        //  cryptic error message even if the developer didn't do the TEX.
        AddAttributeRegister(MojoShaderRegisterType.Texture, src.RegNum,
            MojoShaderUsage.TexCoord, src.RegNum, 0xF, 0);
    }

    /// <summary>
    /// [state_texbem; mojoshader.c]
    /// </summary>
    public void StateTexBemFamily(
        string opcode)
    {
        // The TEXBEM equasion, according to MSDN:
        //u' = TextureCoordinates(stage m)u + D3DTSS_BUMPENVMAT00(stage m)*t(n)R
        //         + D3DTSS_BUMPENVMAT10(stage m)*t(n)G
        //v' = TextureCoordinates(stage m)v + D3DTSS_BUMPENVMAT01(stage m)*t(n)R
        //         + D3DTSS_BUMPENVMAT11(stage m)*t(n)G
        //t(m)RGBA = TextureSample(stage m)
        //
        // ...TEXBEML adds this at the end:
        //t(m)RGBA = t(m)RGBA * [(t(n)B * D3DTSS_BUMPENVLSCALE(stage m)) +
        //           D3DTSS_BUMPENVLOFFSET(stage m)]

        if (ShaderVersionAtLeast(1, 4))
            Fail("{0} opcode not available after Shader Model 1.3", opcode);

        if (!ShaderVersionAtLeast(1, 2))
        {
            if (SourceArgs[0]?.SrcMod == MojoShaderSourceMod.Sign)
                Fail("{0} forbids _bx2 on source reg before ps_1_2", opcode);
        }

        // !!! FIXME: MSDN:
        // !!! FIXME: Register data that has been read by a texbem
        // !!! FIXME:  or texbeml instruction cannot be read later,
        // !!! FIXME:  except by another texbem or texbeml.

        StateTexOps(opcode, 2, 1);
    }

    /// <summary>
    /// [state_TEXBEM; mojoshader.c]
    /// </summary>
    public void StateTexBem() =>
        StateTexBemFamily("TEXBEM");

    /// <summary>
    /// [state_TEXBEML; mojoshader.c]
    /// </summary>
    public void StateTexBemL() =>
        StateTexBemFamily("TEXBEML");

    /// <summary>
    /// [state_TEXM3X2PAD; mojoshader.c]
    /// </summary>
    public void StateTexM3X2Pad()
    {
        if (ShaderVersionAtLeast(1, 4))
            Fail("TEXM3X2PAD opcode not available after Shader Model 1.3");
        StateTexOps("TEXM3X2PAD", 0, 0);
        // !!! FIXME: check for correct opcode existence and order more rigorously?
        TexM3X2PadSrc0 = SourceArgs[0]?.RegNum ?? 0;
        TexM3X2PadDst0 = DestArg?.RegNum ?? 0;
    }

    /// <summary>
    /// [state_TEXM3X2TEX; mojoshader.c]
    /// </summary>
    public void StateTexM3X2Tex()
    {
        if (ShaderVersionAtLeast(1, 4))
            Fail("TEXM3X2TEX opcode not available after Shader Model 1.3");
        if (TexM3X2PadDst0 == -1)
            Fail("TEXM3X2TEX opcode without matching TEXM3X2PAD");
        // !!! FIXME: check for correct opcode existence and order more rigorously?
        StateTexOps("TEXM3X2TEX", 2, 0);
        ResetTexMpad = true;

        var sreg = RegListFind(Samplers, MojoShaderRegisterType.Sampler, DestArg?.RegNum ?? 0);
        var tType = (MojoShaderTextureType)(sreg?.Index ?? 0);

        // A samplermap might change this to something nonsensical.
        if (tType != MojoShaderTextureType.TwoD)
            Fail("TEXM3X2TEX needs a 2D sampler");
    }

    /// <summary>
    /// [state_TEXM3X3PAD; mojoshader.c]
    /// </summary>
    public void StateTexM3X3Pad()
    {
        if (ShaderVersionAtLeast(1, 4))
            Fail("TEXM3X2TEX opcode not available after Shader Model 1.3");
        StateTexOps("TEXM3X3PAD", 0, 0);

        // !!! FIXME: check for correct opcode existence and order more rigorously?
        if (TexM3X3PadDst0 == -1)
        {
            TexM3X3PadSrc0 = SourceArgs[0]?.RegNum ?? 0;
            TexM3X3PadDst0 = DestArg?.RegNum ?? 0;
        }
        else if (TexM3X3PadDst1 == -1)
        {
            TexM3X3PadSrc1 = SourceArgs[0]?.RegNum ?? 0;
            TexM3X3PadDst1 = DestArg?.RegNum ?? 0;
        }
    }

    /// <summary>
    /// [state_texm3x3; mojoshader.c]
    /// </summary>
    public void StateTexM3X3Family(string opcode, int dims)
    {
        // !!! FIXME: check for correct opcode existence and order more rigorously?
        if (ShaderVersionAtLeast(1, 4))
            Fail("{0} opcode not available after Shader Model 1.3", opcode);
        if (TexM3X3PadDst1 == -1)
            Fail("{0} opcode without matching TEXM3X3PADs", opcode);
        StateTexOps(opcode, dims, 0);
        ResetTexMpad = true;

        var sreg = RegListFind(Samplers, MojoShaderRegisterType.Sampler,
            DestArg?.RegNum ?? 0);
        var tType = (MojoShaderTextureType)(sreg?.Index ?? 0);

        // A sampler map might change this to something nonsensical.
        if (tType != MojoShaderTextureType.Volume && tType != MojoShaderTextureType.Cube)
            Fail("{0} needs a 3D or Cubemap sampler", opcode);
    }

    /// <summary>
    /// [state_TEXM3X3; mojoshader.c]
    /// </summary>
    public void StateTexM3X3()
    {
        if (!ShaderVersionAtLeast(1, 2))
            Fail("TEXM3X3 opcode not available in Shader Model 1.1");
        StateTexM3X3Family("TEXM3X3", 0);
    }

    /// <summary>
    /// [state_TEXM3X3TEX; mojoshader.c]
    /// </summary>
    public void StateTexM3X3Tex()
    {
        StateTexM3X3Family("TEXM3X3TEX", 3);
    }

    /// <summary>
    /// [state_TEXM3X3SPEC; mojoshader.c]
    /// </summary>
    public void StateTexM3X3Spec()
    {
        StateTexM3X3Family("TEXM3X3SPEC", 3);
        if (SourceArgs[1]?.RegType != MojoShaderRegisterType.Const)
            Fail("TEXM3X3SPEC final arg must be a constant register");
    }

    /// <summary>
    /// [state_TEXM3X3VSPEC; mojoshader.c]
    /// </summary>
    public void StateTexM3X3VSpec()
    {
        StateTexM3X3Family("TEXM3X3VSPEC", 3);
    }

    public void StateTexLd()
    {
        if (ShaderVersionAtLeast(2, 0))
        {
            var src0 = SourceArgs[0] ?? new MojoShaderSourceArgInfo();
            var src1 = SourceArgs[1] ?? new MojoShaderSourceArgInfo();

            // !!! FIXME: verify texldp restrictions:
            //http://msdn.microsoft.com/en-us/library/bb206221(VS.85).aspx
            // !!! FIXME: ...and texldb, too.
            //http://msdn.microsoft.com/en-us/library/bb206217(VS.85).aspx

            //const RegisterType rt0 = src0->regtype;

            // !!! FIXME: msdn says it has to be temp, but Microsoft's HLSL
            // !!! FIXME:  compiler is generating code that uses oC0 for a dest.
            //if (ctx->dest_arg.regtype != REG_TYPE_TEMP)
            //    Fail("TEXLD dest must be a temp register");

            // !!! FIXME: this can be an REG_TYPE_INPUT, DCL'd to TEXCOORD.
            //else if ((rt0 != REG_TYPE_TEXTURE) && (rt0 != REG_TYPE_TEMP))
            //    Fail("TEXLD src0 must be texture or temp register");
            //else

            if (src0.SrcMod != MojoShaderSourceMod.None)
                Fail("TEXLD src0 must have no modifiers");
            else if (src1.RegType != MojoShaderRegisterType.Sampler)
                Fail("TEXLD src1 must be sampler register");
            else if (src1.SrcMod != MojoShaderSourceMod.None)
                Fail("TEXLD src1 must have no modifiers");
            else if (InstructionControls != (int)MojoShaderTexLdControl.TexLd &&
                     InstructionControls != (int)MojoShaderTexLdControl.TexLdP &&
                     InstructionControls != (int)MojoShaderTexLdControl.TexLdB)
            {
                Fail("TEXLD has unknown control bits");
            }

            // Shader Model 3 added swizzle support to this opcode.
            if (!ShaderVersionAtLeast(3, 0))
            {
                if (!src0.Swizzle.IsNone)
                    Fail("TEXLD src0 must not swizzle");
                else if (!src1.Swizzle.IsNone)
                    Fail("TEXLD src1 must not swizzle");
            }

            if ((MojoShaderTextureType)(SourceArgs[1]?.RegNum ?? 0) == MojoShaderTextureType.Cube)
                InstructionCount += 3;
        }

        else if (ShaderVersionAtLeast(1, 4))
        {
            // !!! FIXME: checks for ps_1_4 version here...
        }

        else
        {
            // !!! FIXME: add (other?) checks for ps_1_1 version here...
            var info = DestArg ?? new MojoShaderDestArgInfo();
            var sampler = info.RegNum;
            if (info.RegType != MojoShaderRegisterType.Texture)
                Fail("TEX param must be a texture register");
            AddSampler(sampler, MojoShaderTextureType.TwoD, 0);
        }
    }

    /// <summary>
    /// [state_TEXLDL; mojoshader.c]
    /// </summary>
    public void StateTexLdl()
    {
        if (!ShaderVersionAtLeast(3, 0))
            Fail("TEXLDL in version < Shader Model 3.0");
        else if (SourceArgs[1]?.RegType != MojoShaderRegisterType.Sampler)
            Fail("TEXLDL src1 must be sampler register");
        else
        {
            if ((MojoShaderTextureType)(SourceArgs[1]?.RegNum ?? 0) == MojoShaderTextureType.Cube)
                InstructionCount += 3;
        }
    }

    /// <summary>
    /// [state_DP2ADD; mojoshader.c]
    /// </summary>
    public void StateDp2Add()
    {
        if (!(SourceArgs[2]?.Swizzle.IsReplicate ?? false))
            Fail("DP2ADD src2 must have replicate swizzle");
    }
}