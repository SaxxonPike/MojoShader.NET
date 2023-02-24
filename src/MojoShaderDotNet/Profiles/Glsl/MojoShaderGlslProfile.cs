using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using MojoShaderDotNet.Profiles.Glsl.V400;
using MojoShaderDotNet.Types;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CommentTypo

namespace MojoShaderDotNet.Profiles.Glsl;

/// <summary>
/// [mojoshader_profile_glsl.c]
/// </summary>
public class MojoShaderGlslProfile : MojoShaderProfile
{
    public override IMojoShaderContext CreateContext() =>
        new MojoShaderGlslContext();

    public override string Name => MojoShaderProfiles.Glsl;

    public virtual string OutputName => "varying";

    public virtual string InputName => "attribute";

    /// <summary>
    /// [macro EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitUnimplemented(IMojoShaderContext ctx, string name) =>
        ctx.Fail("{0} unimplemented in glsl profile", name);

    /// <summary>
    /// [get_GLSL_register_string; mojoshader_profile_glsl.c]
    /// </summary>
    public (string name, string number)? GetRegisterString(IMojoShaderContext ctx, MojoShaderRegisterType regType,
        int regNum) =>
        // turns out these are identical at the moment.
        GetD3DRegisterString(ctx, regType, regNum);

    /// <summary>
    /// [get_GLSL_uniform_type; mojoshader_profile_glsl.c]
    /// </summary>
    public string? GetUniformType(IMojoShaderContext ctx, MojoShaderRegisterType rType)
    {
        switch (rType)
        {
            case MojoShaderRegisterType.Const:
                return "vec4";
            case MojoShaderRegisterType.ConstInt:
                return "ivec4";
            case MojoShaderRegisterType.ConstBool:
                return "bool";
            default:
                ctx.Fail("BUG: used a uniform we don't know how to define.");
                break;
        }

        return null;
    }

    /// <summary>
    /// [get_GLSL_varname; mojoshader_profile_glsl.c]
    /// </summary>
    public override string? GetVarName(IMojoShaderContext ctx, MojoShaderRegisterType regType, int regNum) =>
        GetRegisterString(ctx, regType, regNum) is { } type
            ? $"{ctx.ShaderTypeStr}_{type.name}{type.number}"
            : null;

    /// <summary>
    /// [get_GLSL_const_array_varname; mojoshader_profile_glsl.c]
    /// </summary>
    public override string GetConstArrayVarName(IMojoShaderContext ctx, int @base, int size) =>
        $"{ctx.ShaderTypeStr}_const_array_{@base}_{size}";

    /// <summary>
    /// [get_GLSL_input_array_varname; mojoshader_profile_glsl.c]
    /// </summary>
    public string GetInputArrayVarName(IMojoShaderContext ctx) =>
        "vertex_input_array";

    /// <summary>
    /// [get_GLSL_uniform_array_varname; mojoshader_profile_glsl.c]
    /// </summary>
    public string GetUniformArrayVarName(IMojoShaderContext ctx, MojoShaderRegisterType regType) =>
        $"{ctx.ShaderTypeStr}_uniforms_{GetUniformType(ctx, regType)}";

    /// <summary>
    /// [get_GLSL_destarg_varname; mojoshader_profile_glsl.c]
    /// </summary>
    public string? GetDestArgVarName(IMojoShaderContext ctx) =>
        ctx.DestArg is { } arg
            ? $"{GetVarName(ctx, arg.RegType, arg.RegNum)}"
            : null;

    /// <summary>
    /// [get_GLSL_srcarg_varname; mojoshader_profile_glsl.c]
    /// </summary>
    public string? GetSrcArgVarName(IMojoShaderContext ctx, int idx)
    {
        if (ctx.SourceArgs is not { } args)
            return null;

        if (idx >= args.Length)
        {
            ctx.Fail("Too many source args");
            return string.Empty;
        }

        return args[idx] is { } arg
            ? GetVarName(ctx, arg.RegType, arg.RegNum)
            : null;
    }

    /// <summary>
    /// [get_GLSL_destarg_assign; mojoshader_profile_glsl.c]
    /// </summary>
    public string? MakeDestArgAssign(
        IMojoShaderContext ctx,
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
        string? fmt,
        params object?[] ap)
    {
        var arg = ctx.DestArg;

        if (arg == null)
            return null;

        if (arg.WriteMask == 0)
            return string.Empty; // no writemask? It's a no-op.

        var clampLeft = string.Empty;
        var clampRight = string.Empty;
        if ((arg.ResultMod & MojoShaderMod.Saturate) != 0)
        {
            var vecSize = arg.WriteMask.VecSize;
            clampLeft = "clamp(";
            clampRight = vecSize == 1
                ? ", 0.0, 1.0)"
                : $", vec{vecSize}(0.0), vec{vecSize}(1.0))";
        }

        // MSDN says MOD_PP is a hint and many implementations ignore it. So do we.

        // CENTROID only allowed in DCL opcodes, which shouldn't come through here.
        Debug.Assert((arg.ResultMod & MojoShaderMod.Centroid) == 0);

        if (ctx.Predicated)
        {
            ctx.Fail("predicated destinations unsupported"); // !!! FIXME
            return string.Empty;
        }

        var operation = ap.Length > 0
            ? string.Format(fmt ?? string.Empty, ap)
            : fmt ?? string.Empty;

        var (needParens, resultShiftStr) = arg.ResultShift switch
        {
            0x1 => (true, " * 2.0"),
            0x2 => (true, " * 4.0"),
            0x3 => (true, " * 8.0"),
            0xD => (true, " / 8.0"),
            0xE => (true, " / 4.0"),
            0xF => (true, " / 2.0"),
            _ => (false, string.Empty)
        };

        var regTypeStr = string.Empty;
        var regNumStr = string.Empty;
        if (GetRegisterString(ctx, arg.RegType, arg.RegNum) is { } regString)
            (regTypeStr, regNumStr) = regString;

        var writeMaskStr = new StringBuilder();
        var scalar = ctx.IsScalar(ctx.ShaderType, arg.RegType, arg.RegNum);
        if (!scalar && !arg.WriteMask.IsXyzw)
        {
            writeMaskStr.Append('.');
            if (arg.WriteMask[0]) writeMaskStr.Append('x');
            if (arg.WriteMask[1]) writeMaskStr.Append('y');
            if (arg.WriteMask[2]) writeMaskStr.Append('z');
            if (arg.WriteMask[3]) writeMaskStr.Append('w');
        }

        var leftParen = needParens ? "(" : "";
        var rightParen = needParens ? ")" : "";

        return $"{ctx.ShaderTypeStr}_{regTypeStr}{regNumStr}{writeMaskStr} = " +
               $"{clampLeft}{leftParen}{operation}{rightParen}{resultShiftStr}{clampRight};";
    }

    /// <summary>
    /// [make_GLSL_swizzle_string; mojoshader_profile_glsl.c]
    /// </summary>
    public string MakeSwizzleString(
        IMojoShaderContext ctx,
        MojoShaderSwizzleValue swizzle,
        MojoShaderWriteMaskValue writemask)
    {
        const string swizzleChannels = "xyzw";

        if (swizzle.IsNone && writemask.IsXyzw)
            return string.Empty;

        var swizStr = new StringBuilder();
        swizStr.Append('.');
        if (writemask[0])
            swizStr.Append(swizzleChannels[swizzle[0]]);
        if (writemask[1])
            swizStr.Append(swizzleChannels[swizzle[1]]);
        if (writemask[2])
            swizStr.Append(swizzleChannels[swizzle[2]]);
        if (writemask[3])
            swizStr.Append(swizzleChannels[swizzle[3]]);
        return swizStr.ToString();
    }

    /// <summary>
    /// [make_GLSL_srcarg_string; mojoshader_profile_glsl.c]
    /// </summary>
    public string? MakeSrcArgString(
        IMojoShaderContext ctx,
        int idx,
        MojoShaderWriteMaskValue writemask)
    {
        const string swizzleChannels = "xyzw";

        if (idx >= ctx.SourceArgs.Length)
        {
            ctx.Fail("Too many source args");
            return string.Empty;
        }

        var arg = ctx.SourceArgs[idx];
        if (arg == null)
            return null;

        var preModStr = "";
        var postModStr = "";
        switch (arg.SrcMod)
        {
            case MojoShaderSourceMod.Negate:
                preModStr = "-";
                break;

            case MojoShaderSourceMod.BiasNegate:
                preModStr = "-(";
                postModStr = " - 0.5)";
                break;

            case MojoShaderSourceMod.Bias:
                preModStr = "(";
                postModStr = " - 0.5)";
                break;

            case MojoShaderSourceMod.SignNegate:
                preModStr = "-((";
                postModStr = " - 0.5) * 2.0)";
                break;

            case MojoShaderSourceMod.Sign:
                preModStr = "((";
                postModStr = " - 0.5) * 2.0)";
                break;

            case MojoShaderSourceMod.Complement:
                preModStr = "(1.0 - ";
                postModStr = ")";
                break;

            case MojoShaderSourceMod.X2Negate:
                preModStr = "-(";
                postModStr = " * 2.0)";
                break;

            case MojoShaderSourceMod.X2:
                preModStr = "(";
                postModStr = " * 2.0)";
                break;

            case MojoShaderSourceMod.Dz:
                ctx.Fail("SRCMOD_DZ unsupported");
                return string.Empty; // !!! FIXME

            case MojoShaderSourceMod.Dw:
                ctx.Fail("SRCMOD_DW unsupported");
                return string.Empty; // !!! FIXME

            case MojoShaderSourceMod.AbsNegate:
                preModStr = "-abs(";
                postModStr = ")";
                break;

            case MojoShaderSourceMod.Abs:
                preModStr = "abs(";
                postModStr = ")";
                break;

            case MojoShaderSourceMod.Not:
                preModStr = "!";
                break;
        }

        string? regTypeStr = null;

        if (!arg.Relative)
            regTypeStr = GetVarName(ctx, arg.RegType, arg.RegNum);

        var relLBracket = string.Empty;
        var relOffset = string.Empty;
        var relRBracket = string.Empty;
        var relSwizzle = string.Empty;
        var relRegTypeStr = string.Empty;

        if (arg.Relative)
        {
            if (arg.RegType == MojoShaderRegisterType.Input)
                regTypeStr = GetInputArrayVarName(ctx);
            else
            {
                Debug.Assert(arg.RegType == MojoShaderRegisterType.Const);
                var ra = arg.RelativeArray[0];
                var arrayIdx = ra.Index;
                var offset = arg.RegNum - arrayIdx;
                Debug.Assert(offset >= 0);
                if (ra.Constants.Count > 0)
                {
                    var arraySize = ra.Count;
                    regTypeStr = GetConstArrayVarName(ctx, arrayIdx, arraySize);
                    if (offset != 0)
                        relOffset = $"{offset} + ";
                }
                else
                {
                    regTypeStr = GetUniformArrayVarName(ctx, arg.RegType);
                    relOffset = offset == 0
                        ? $"ARRAYBASE_{arrayIdx} + "
                        : $"(ARRAYBASE_{arrayIdx} + {offset}) + ";
                }
            }

            relLBracket = "[";

            if (arg.RelativeRegType == MojoShaderRegisterType.Loop)
            {
                relRegTypeStr = "aL";
                relSwizzle = string.Empty;
            }
            else
            {
                relRegTypeStr = GetVarName(ctx, arg.RelativeRegType, arg.RelativeRegNum);
                relSwizzle = $".{swizzleChannels[arg.RelativeComponent]}";
            }

            relRBracket = "]";
        }

        var swizStr = string.Empty;
        if (!ctx.IsScalar(ctx.ShaderType, arg.RegType, arg.RegNum))
        {
            swizStr = MakeSwizzleString(ctx, arg.Swizzle, writemask);
        }

        if (regTypeStr == null)
        {
            ctx.Fail("Unknown source register type.");
            return string.Empty;
        }

        return string.Concat(
            preModStr, regTypeStr, relLBracket, relOffset,
            relRegTypeStr, relSwizzle, relRBracket, swizStr,
            postModStr);
    }

    /// <summary>
    /// macro [MAKE_GLSL_SRCARG_STRING_x; mojoshader_profile_glsl.c]
    /// </summary>
    public string? MakeSrcArgStringX(IMojoShaderContext ctx, int idx) =>
        MakeSrcArgString(ctx, idx, 0b0001);

    /// <summary>
    /// macro [MAKE_GLSL_SRCARG_STRING_y; mojoshader_profile_glsl.c]
    /// </summary>
    public string? MakeSrcArgStringY(IMojoShaderContext ctx, int idx) =>
        MakeSrcArgString(ctx, idx, 0b0010);

    /// <summary>
    /// macro [MAKE_GLSL_SRCARG_STRING_z; mojoshader_profile_glsl.c]
    /// </summary>
    public string? MakeSrcArgStringZ(IMojoShaderContext ctx, int idx) =>
        MakeSrcArgString(ctx, idx, 0b0100);

    /// <summary>
    /// macro [MAKE_GLSL_SRCARG_STRING_w; mojoshader_profile_glsl.c]
    /// </summary>
    public string? MakeSrcArgStringW(IMojoShaderContext ctx, int idx) =>
        MakeSrcArgString(ctx, idx, 0b1000);

    /// <summary>
    /// macro [MAKE_GLSL_SRCARG_STRING_scalar; mojoshader_profile_glsl.c]
    /// </summary>
    public string? MakeSrcArgStringScalar(IMojoShaderContext ctx, int idx) =>
        MakeSrcArgString(ctx, idx, 0b0001);

    /// <summary>
    /// macro [MAKE_GLSL_SRCARG_STRING_full; mojoshader_profile_glsl.c]
    /// </summary>
    public string? MakeSrcArgStringFull(IMojoShaderContext ctx, int idx) =>
        MakeSrcArgString(ctx, idx, 0b1111);

    /// <summary>
    /// macro [MAKE_GLSL_SRCARG_STRING_masked; mojoshader_profile_glsl.c]
    /// </summary>
    public string? MakeSrcArgStringMasked(IMojoShaderContext ctx, int idx) =>
        MakeSrcArgString(ctx, idx, ctx.DestArg?.WriteMask ?? 0);

    /// <summary>
    /// macro [MAKE_GLSL_SRCARG_STRING_vec3; mojoshader_profile_glsl.c]
    /// </summary>
    public string? MakeSrcArgStringVec3(IMojoShaderContext ctx, int idx) =>
        MakeSrcArgString(ctx, idx, 0b0111);

    /// <summary>
    /// macro [MAKE_GLSL_SRCARG_STRING_vec2; mojoshader_profile_glsl.c]
    /// </summary>
    public string? MakeSrcArgStringVec2(IMojoShaderContext ctx, int idx) =>
        MakeSrcArgString(ctx, idx, 0b0011);

    /// <summary>
    /// get_GLSL_comparison_string_scalar; mojoshader_profile_glsl.c]
    /// </summary>
    public string GetComparisonStringScalar(IMojoShaderContext ctx)
    {
        var result = (MojoShaderComparisonControl)ctx.InstructionControls switch
        {
            MojoShaderComparisonControl.None => "",
            MojoShaderComparisonControl.Greater => ">",
            MojoShaderComparisonControl.Equal => "==",
            MojoShaderComparisonControl.GreaterOrEqual => ">=",
            MojoShaderComparisonControl.Less => "<",
            MojoShaderComparisonControl.Not => "!=",
            MojoShaderComparisonControl.LessOrEqual => "<=",
            _ => null
        };

        if (result != null)
            return result;

        ctx.Fail("unknown comparison control");
        return string.Empty;
    }

    /// <summary>
    /// get_GLSL_comparison_string_vector; mojoshader_profile_glsl.c]
    /// </summary>
    public string GetComparisonStringVector(IMojoShaderContext ctx)
    {
        var result = (MojoShaderComparisonControl)ctx.InstructionControls switch
        {
            MojoShaderComparisonControl.None => "",
            MojoShaderComparisonControl.Greater => "greaterThan",
            MojoShaderComparisonControl.Equal => "equal",
            MojoShaderComparisonControl.GreaterOrEqual => "greaterThanEqual",
            MojoShaderComparisonControl.Less => "lessThan",
            MojoShaderComparisonControl.Not => "notEqual",
            MojoShaderComparisonControl.LessOrEqual => "lessThanEqual",
            _ => null
        };

        if (result != null)
            return result;

        ctx.Fail("unknown comparison control");
        return string.Empty;
    }

    /// <summary>
    /// !!! FIXME:
    /// GLSL 1.30 introduced textureGrad() for this, but it looks like the
    ///  functions are overloaded instead of texture2DGrad() (etc).
    ///
    /// The spec says we can't use GLSL's texture*Lod() built-ins from fragment
    ///  shaders for some inexplicable reason.
    /// For now, you'll just have to suffer with the potentially wrong mipmap
    ///  until I can figure something out.
    /// 
    /// ARB_shader_texture_lod and EXT_gpu_shader4 added texture2DLod/Grad*(),
    ///  so we'll use them if available. Failing that, we'll just fallback
    ///  to a regular texture2D call and hope the mipmap it chooses is close
    ///  enough.
    ///
    /// [prepend_glsl_texlod_extensions; mojoshader_profile_glsl.c]
    /// </summary>
    public void PrependTexLodExtensions(IMojoShaderContext ctx)
    {
        if (ctx is not MojoShaderGlslContext { GlslGeneratedTexLodSetup: true } glCtx)
            return;

        glCtx.GlslGeneratedTexLodSetup = true;

        glCtx.PushOutput(MojoShaderProfileOutput.Preflight);
        if (glCtx.ProfileSupportsGlsl400)
        {
            // These functions exist in GLSL 4.0+ for both Vertex and Fragment shaders.
            glCtx.OutputLine("#define texture2DGrad(a,b,c,d) textureGrad(a,b,c,d)");
            glCtx.OutputLine("#define texture2DProjGrad(a,b,c,d) textureProjGrad(a,b,c,d)");
            glCtx.OutputLine("#define texture2DLod(a,b,c) textureLod(a,b,c)");
        }
        else
        {
            // Use reduced functionality in older GLSL contexts. 
            glCtx.OutputLine("#if GL_ARB_shader_texture_lod");
            glCtx.OutputLine("#extension GL_ARB_shader_texture_lod : enable");
            glCtx.OutputLine("#define texture2DGrad texture2DGradARB");
            glCtx.OutputLine("#define texture2DProjGrad texture2DProjARB");
            glCtx.OutputLine("#elif GL_EXT_gpu_shader4");
            glCtx.OutputLine("#extension GL_EXT_gpu_shader4 : enable");
            glCtx.OutputLine("#else");
            glCtx.OutputLine("#define texture2DGrad(a,b,c,d) texture2D(a,b)");
            glCtx.OutputLine("#define texture2DProjGrad(a,b,c,d) texture2DProj(a,b)");
            if (glCtx.ShaderIsPixel())
                glCtx.OutputLine("#define texture2DLod(a,b,c) texture2D(a,b)");
            glCtx.OutputLine("#endif");
        }

        glCtx.OutputBlankLine();
        glCtx.PopOutput();
    }

    protected virtual bool EmitStartInternal(IMojoShaderContext ctx)
    {
        if (ctx is not MojoShaderGlslContext validCtx)
            return false;

        // No gl_FragData[] before GLSL 1.10, so we have to force the version.
        validCtx.PushOutput(MojoShaderProfileOutput.Preflight);
        validCtx.OutputLine("#version 110");
        validCtx.PopOutput();
        return true;
    }

    /// <summary>
    /// [emit_GLSL_start; mojoshader_profile_glsl.c]
    /// </summary>
    public override void EmitStart(IMojoShaderContext ctx, string profileStr)
    {
        if (!ctx.ShaderIsVertex() && !ctx.ShaderIsPixel())
        {
            ctx.Fail("Shader type {0} unsupported in this profile.", ctx.ShaderType);
            return;
        }

        if (!EmitStartInternal(ctx))
        {
            ctx.Fail("Profile '{0}' unsupported or unknown.", ctx.ProfileId);
            return;
        }

        ctx.PushOutput(MojoShaderProfileOutput.MainLineIntro);
        ctx.OutputLine("void main()");
        ctx.OutputLine("{");
        ctx.PopOutput();

        ctx.SetOutput(MojoShaderProfileOutput.MainLine);
        ctx.Indent++;
    }

    /// <summary>
    /// [emit_GLSL_end; mojoshader_profile_glsl.c]
    /// </summary>
    public override void EmitEnd(IMojoShaderContext ctx)
    {
        // ps_1_* writes color to r0 instead oC0. We move it to the right place.
        // We don't have to worry about a RET opcode messing this up, since
        //  RET isn't available before ps_2_0.
        if (ctx.ShaderIsPixel() && !ctx.ShaderVersionAtLeast(2, 0))
        {
            var shStr = ctx.ShaderTypeStr;
            ctx.SetUsedRegister(MojoShaderRegisterType.ColorOut, 0, true);
            ctx.OutputLine("{0}_oC0 = {1}_r0;", shStr, shStr);
        }
        else if (ctx.ShaderIsVertex())
        {
            if (ctx.FlipRenderTargetOption)
            {
                ctx.OutputLine("gl_Position.y = gl_Position.y * vpFlip;");
            }

            if (ctx.DepthClippingOption)
            {
                ctx.OutputLine("gl_Position.z = gl_Position.z * 2.0 - gl_Position.w;");
            }
        }

        // force a RET opcode if we're at the end of the stream without one.
        if (ctx.PreviousOpcode != MojoShaderOpcode.Ret)
            EmitRet(ctx);
    }

    /// <summary>
    /// [emit_GLSL_phase; mojoshader_profile_glsl.c]
    /// </summary>
    public override void EmitPhase(IMojoShaderContext ctx)
    {
        // no-op in GLSL.
    }

    /// <summary>
    /// [output_GLSL_uniform_array; mojoshader_profile_glsl.c]
    /// </summary>
    public void OutputUniformArray(IMojoShaderContext ctx, MojoShaderRegisterType regType, int size)
    {
        if (size < 1)
            return;

        var buf = GetUniformArrayVarName(ctx, regType);
        string type;
        switch (regType)
        {
            case MojoShaderRegisterType.Const:
                type = "vec4";
                break;
            case MojoShaderRegisterType.ConstInt:
                type = "ivec4";
                break;
            case MojoShaderRegisterType.ConstBool:
                type = "bool";
                break;
            default:
                ctx.Fail("BUG: used a uniform we don't know how to define.");
                return;
        }

        ctx.OutputLine("uniform {0} {1}[{2}];", type, buf, size);
    }

    /// <summary>
    /// [emit_GLSL_finalize; mojoshader_profile_glsl.c]
    /// </summary>
    public override void EmitFinalize(IMojoShaderContext ctx)
    {
        // throw some blank lines around to make source more readable.
        ctx.PushOutput(MojoShaderProfileOutput.Globals);
        ctx.OutputBlankLine();
        ctx.PopOutput();

        // If we had a relative addressing of REG_TYPE_INPUT, we need to build
        //  an array for it at the start of main(). GLSL doesn't let you specify
        //  arrays of attributes.
        //vec4 blah_array[BIGGEST_ARRAY];
        if (ctx.HaveRelativeInputRegisters) // !!! FIXME
            ctx.Fail("Relative addressing of input registers not supported.");

        ctx.PushOutput(MojoShaderProfileOutput.Preflight);
        OutputUniformArray(ctx, MojoShaderRegisterType.Const, ctx.UniformFloat4Count);
        OutputUniformArray(ctx, MojoShaderRegisterType.ConstInt, ctx.UniformInt4Count);
        OutputUniformArray(ctx, MojoShaderRegisterType.ConstBool, ctx.UniformBoolCount);

        if (ctx.FlipRenderTargetOption && ctx.ShaderIsVertex())
            ctx.OutputLine("uniform float vpFlip;");

        if (ctx.NeedsMaxFloat)
            ctx.OutputLine("const float FLT_MAX = 1e38;");

        ctx.PopOutput();
    }

    /// <summary>
    /// [emit_GLSL_global; mojoshader_profile_glsl.c]
    /// </summary>
    public override void EmitGlobal(IMojoShaderContext ctx, MojoShaderRegisterType regType, int regNum)
    {
        var varName = GetVarName(ctx, regType, regNum);

        ctx.PushOutput(MojoShaderProfileOutput.Globals);
        switch (regType)
        {
            case MojoShaderRegisterType.Address:
                if (ctx.ShaderIsVertex())
                    ctx.OutputLine("ivec4 {0};", varName);
                else if (ctx.ShaderIsPixel()) // actually REG_TYPE_TEXTURE.
                {
                    // We have to map texture registers to temps for ps_1_1, since
                    //  they work like temps, initialize with tex coords, and the
                    //  ps_1_1 TEX opcode expects to overwrite it.
                    if (!ctx.ShaderVersionAtLeast(1, 4))
                    {
                        // GLSL ES does not have gl_TexCoord!
                        // Also, gl_TexCoord[4+] is unreliable!
                        var skipGlTexCoord = ctx is MojoShaderGlslContext { ProfileSupportsGlslEs: true } ||
                                             regNum >= 4;
                        if (skipGlTexCoord)
                            ctx.OutputLine("vec4 {0} = io_{1}_{2};",
                                varName, (int)MojoShaderUsage.TexCoord, regNum);
                        else
                            ctx.OutputLine("vec4 {0} = gl_TexCoord[{1}];",
                                varName, regNum);
                    }
                }

                break;
            case MojoShaderRegisterType.Predicate:
                ctx.OutputLine("bvec4 {0};", varName);
                break;
            case MojoShaderRegisterType.Temp:
                ctx.OutputLine("vec4 {0};", varName);
                break;
            case MojoShaderRegisterType.Loop:
                break; // no-op. We declare these in for loops at the moment.
            case MojoShaderRegisterType.Label:
                break; // no-op. If we see it here, it means we optimized it out.
            default:
                ctx.Fail("BUG: we used a register we don't know how to define.");
                break;
        }

        ctx.PopOutput();
    }

    /// <summary>
    /// [emit_GLSL_array; mojoshader_profile_glsl.c]
    /// </summary>
    public override void EmitArray(IMojoShaderContext ctx, MojoShaderVariable var)
    {
        // All uniforms (except constant arrays, which only get pushed once at
        //  compile time) are now packed into a single array, so we can batch
        //  the uniform transfers. So this doesn't actually define an array
        //  here; the one, big array is emitted during finalization instead.
        // However, we need to #define the offset into the one, big array here,
        //  and let dereferences use that #define.
        var @base = var.Index;
        var glslBase = ctx.UniformFloat4Count;
        ctx.PushOutput(MojoShaderProfileOutput.Globals);
        ctx.OutputLine("#define ARRAYBASE_{0} {1}", @base, glslBase);
        ctx.PopOutput();
        var.EmitPosition = glslBase;
    }

    /// <summary>
    /// [emit_GLSL_const_array; mojoshader_profile_glsl.c]
    /// </summary>
    public override void EmitConstArray(IMojoShaderContext ctx, IList<MojoShaderConstant> constList, int @base,
        int size)
    {
        // stock GLSL 1.0 can't do constant arrays, so make a uniform array
        //  and have the OpenGL glue assign it at link time. Lame!
        var varName = GetConstArrayVarName(ctx, @base, size);
        ctx.PushOutput(MojoShaderProfileOutput.Globals);
        ctx.OutputLine("uniform vec4 {0}[{1}];", varName, size);
        ctx.PopOutput();
    }

    /// <summary>
    /// [emit_GLSL_uniform; mojoshader_profile_glsl.c]
    /// </summary>
    public override void EmitUniform(IMojoShaderContext ctx, MojoShaderRegisterType regType, int regNum,
        MojoShaderVariable? var)
    {
        // Now that we're pushing all the uniforms as one big array, pack these
        //  down, so if we only use register c439, it'll actually map to
        //  glsl_uniforms_vec4[0]. As we push one big array, this will prevent
        //  uploading unused data.

        string? name;
        var index = 0;

        var varName = GetVarName(ctx, regType, regNum);
        ctx.PushOutput(MojoShaderProfileOutput.Globals);

        if (var == null)
        {
            name = GetUniformArrayVarName(ctx, regType);

            if (regType == MojoShaderRegisterType.Const)
                index = ctx.UniformFloat4Count;
            else if (regType == MojoShaderRegisterType.ConstInt)
                index = ctx.UniformInt4Count;
            else if (regType == MojoShaderRegisterType.ConstBool)
                index = ctx.UniformBoolCount;
            else // get_GLSL_uniform_array_varname() would have called fail().
                Debug.Assert(!ctx.IsFail);

            ctx.OutputLine("#define {0} {1}[{2}]", varName, name, index);
        }

        else
        {
            var arraybase = var.Index;
            if (var.Constants.Any())
            {
                name = GetConstArrayVarName(ctx, arraybase, var.Count);
                index = regNum - arraybase;
            }
            else
            {
                Debug.Assert(var.EmitPosition != -1);
                name = GetUniformArrayVarName(ctx, regType);
                index = regNum - arraybase + var.EmitPosition;
            }

            ctx.OutputLine("#define {0} {1}[{2}]", varName, name, index);
        }

        ctx.PopOutput();
    }

    public override void EmitSampler(IMojoShaderContext ctx, int stage, MojoShaderTextureType ttype, bool tb)
    {
        var type = string.Empty;
        switch (ttype)
        {
            case MojoShaderTextureType.TwoD:
                type = "sampler2D";
                break;
            case MojoShaderTextureType.Cube:
                type = "samplerCube";
                break;
            case MojoShaderTextureType.Volume:
                type = "sampler3D";
                break;
            default:
                ctx.Fail("BUG: used a sampler we don't know how to define.");
                break;
        }

        var var = GetVarName(ctx, MojoShaderRegisterType.Sampler, stage);

        ctx.PushOutput(MojoShaderProfileOutput.Globals);
        ctx.OutputLine("uniform {0} {1};", type, var);
        if (tb) // This sampler used a ps_1_1 TEXBEM opcode?
        {
            var index = ctx.UniformFloat4Count;
            ctx.UniformFloat4Count += 2;
            var name = GetUniformArrayVarName(ctx, MojoShaderRegisterType.Const);
            ctx.OutputLine("#define {0}_texbem {1}[{2}]", var, name, index);
            ctx.OutputLine("#define {0}_texbeml {1}[{2}]", var, name, index + 1);
        }

        ctx.PopOutput();
    }

    public override void EmitAttribute(IMojoShaderContext ctx, MojoShaderRegisterType regType, int regNum,
        MojoShaderUsage usage, int index,
        int wMask, int flags)
    {
        var supportGlslEs = ctx is MojoShaderGlslContext { ProfileSupportsGlslEs: true };
        var supportGlsl400 = ctx is MojoShaderGlslContext { ProfileSupportsGlsl400: true };

        // !!! FIXME: this function doesn't deal with write masks at all yet!
        string? usageStr = null;
        var arrayLeft = string.Empty;
        var arrayRight = string.Empty;
        var indexStr = string.Empty;

        var var = GetVarName(ctx, regType, regNum);

        //assert((flags & MOD_PP) == 0);  // !!! FIXME: is PP allowed?
        if (index != 0) // !!! FIXME: a lot of these MUST be zero.
            indexStr = $"{index}";

        if (ctx.ShaderIsVertex())
        {
            // pre-vs3 output registers.
            // these don't ever happen in DCL opcodes, I think. Map to vs_3_*
            //  output registers.
            if (!ctx.ShaderVersionAtLeast(3, 0))
            {
                if (regType == MojoShaderRegisterType.RastOut)
                {
                    regType = MojoShaderRegisterType.Output;
                    index = regNum;
                    switch ((MojoShaderRastOutType)regNum)
                    {
                        case MojoShaderRastOutType.Position:
                            usage = MojoShaderUsage.Position;
                            break;
                        case MojoShaderRastOutType.Fog:
                            usage = MojoShaderUsage.Fog;
                            break;
                        case MojoShaderRastOutType.PointSize:
                            usage = MojoShaderUsage.PointSize;
                            break;
                    }
                }

                else if (regType == MojoShaderRegisterType.AttrOut)
                {
                    regType = MojoShaderRegisterType.Output;
                    usage = MojoShaderUsage.Color;
                    index = regNum;
                }

                else if (regType == MojoShaderRegisterType.TexCrdOut)
                {
                    regType = MojoShaderRegisterType.Output;
                    usage = MojoShaderUsage.TexCoord;
                    index = regNum;
                }
            }
            
            // Some templates for writing input and output definitions..

            void WriteVarying(string? v, MojoShaderUsage u, int i)
            {
                if (supportGlsl400)
                {
                    // "same as _out_ when in a vertex shader and same as _in_ when in a fragment shader"
                    var direction = ctx.ShaderType switch
                    {
                        MojoShaderShaderType.Vertex => "out",
                        _ => "in"
                    };
                    ctx.OutputLine("{2} io_{0}_{1};", u, i, direction);
                }
                else if (supportGlslEs)
                    ctx.OutputLine("varying highp float io_{0}_{1};", (int) u, i);
                else
                    ctx.OutputLine("varying float io_{0}_{1};", (int) u, i);
                ctx.OutputLine("#define {0} io_{1}_{2}", v, u, i);
            }

            void WriteAttribute(string? v)
            {
                if (supportGlsl400)
                    ctx.OutputLine("in vec4 {0};", v);
                else
                    ctx.OutputLine("attribute vec4 {0};", v);
            }
            
            // to avoid limitations of various GL entry points for input
            // attributes (glSecondaryColorPointer() can only take 3 component
            // items, glVertexPointer() can't do GL_UNSIGNED_BYTE, many other
            // issues), we set up all inputs as generic vertex attributes, so we
            // can pass data in just about any form, and ignore the built-in GLSL
            // attributes like gl_SecondaryColor. Output needs to use the the
            // built-ins, though, but we don't have to worry about the GL entry
            // point limitations there.

            if (regType == MojoShaderRegisterType.Input)
            {
                ctx.PushOutput(MojoShaderProfileOutput.Globals);
                WriteAttribute(var);
                ctx.PopOutput();
            }

            else if (regType == MojoShaderRegisterType.Output)
            {
                switch (usage)
                {
                    case MojoShaderUsage.Position:
                        if (index == 0)
                            usageStr = "gl_Position";
                        break;
                    case MojoShaderUsage.PointSize:
                        if (index == 0)
                            usageStr = "gl_PointSize";
                        else
                        {
                            ctx.PushOutput(MojoShaderProfileOutput.Globals);
                            WriteVarying(var, usage, index);
                            ctx.PopOutput();
                            return;
                        }

                        break;
                    case MojoShaderUsage.Color:
                        // GLSL ES does not have gl_FrontColor
                        // GLSL 4.0+ does not have gl_FrontColor outside the compatibility profile
                        if (supportGlslEs || supportGlsl400)
                            break;
                        indexStr = string.Empty; // no explicit number.
                        usageStr = index switch
                        {
                            0 => "gl_FrontColor",
                            1 => "gl_FrontSecondaryColor",
                            _ => null
                        };
                        break;
                    case MojoShaderUsage.Fog:
                        // GLSL ES does not have gl_FogFragCoord
                        // GLSL 4.0+ does not have gl_FogFragCoord outside the compatibility profile
                        if (supportGlslEs || supportGlsl400)
                            break;
                        if (index == 0)
                            usageStr = "gl_FogFragCoord";
                        else
                        {
                            ctx.PushOutput(MojoShaderProfileOutput.Globals);
                            WriteVarying(var, usage, index);
                            ctx.PopOutput();
                            return;
                        }

                        break;
                    case MojoShaderUsage.TexCoord:
                        // GLSL ES does not have gl_TexCoord
                        // GLSL 4.0+ does not have gl_TexCoord outside the compatibility profile
                        if (supportGlslEs || supportGlsl400)
                            break; 
                        if (index >= 4)
                            break; // gl_TexCoord[4+] is unreliable!
                        indexStr = $"{index}";
                        usageStr = "gl_TexCoord";
                        arrayLeft = "[";
                        arrayRight = "]";
                        break;

                    // !!! FIXME: we need to deal with some more built-in varyings here.
                }

                // !!! FIXME: the #define is a little hacky, but it means we don't
                // !!! FIXME:  have to track these separately if this works.
                ctx.PushOutput(MojoShaderProfileOutput.Globals);
                // no mapping to built-in var? Just make it a regular global, pray.
                if (usageStr == null)
                {
                    WriteVarying(var, usage, index);
                }
                else
                {
                    ctx.OutputLine("#define {0} {1}{2}{3}{4}", var, usageStr,
                        arrayLeft, indexStr, arrayRight);
                }

                ctx.PopOutput();
            }

            else
            {
                ctx.Fail("unknown vertex shader attribute register");
            }
        }

        else if (ctx.ShaderIsPixel())
        {
            // samplers DCLs get handled in emit_GLSL_sampler().

            if ((flags & (int)MojoShaderMod.Centroid) != 0) // !!! FIXME
            {
                ctx.Fail("centroid unsupported in {0} profile", ctx.Profile?.Name);
                return;
            }

            if (regType == MojoShaderRegisterType.ColorOut)
            {
                if (!ctx.HaveMultiColorOutputs)
                    usageStr = "gl_FragColor"; // maybe faster?
                else
                {
                    indexStr = $"{regNum}";
                    usageStr = "gl_FragData";
                    arrayLeft = "[";
                    arrayRight = "]";
                }
            }

            else if (regType == MojoShaderRegisterType.DepthOut)
                usageStr = "gl_FragDepth";

            // !!! FIXME: can you actualy have a texture register with COLOR usage?
            else if (regType is MojoShaderRegisterType.Texture or MojoShaderRegisterType.Input)
            {
                if (!supportGlslEs)
                {
                    if (usage == MojoShaderUsage.TexCoord)
                    {
                        // ps_1_1 does a different hack for this attribute.
                        //  Refer to emit_GLSL_global()'s REG_TYPE_ADDRESS code.
                        if (!ctx.ShaderVersionAtLeast(1, 4))
                            return;
                        if (index < 4) // gl_TexCoord[4+] is unreliable!
                        {
                            indexStr = $"{index}";
                            usageStr = "gl_TexCoord";
                            arrayLeft = "[";
                            arrayRight = "]";
                        }
                    }

                    else if (usage == MojoShaderUsage.Color)
                    {
                        indexStr = string.Empty; // no explicit number.
                        if (index == 0)
                            usageStr = "gl_Color";
                        else if (index == 1)
                            usageStr = "gl_SecondaryColor";
                        // FIXME: Does this even matter when we have varyings? -flibit
                        // else
                        //    fail(ctx, "unsupported color index");
                    }
                }
            }

            else if (regType == MojoShaderRegisterType.MiscType)
            {
                var mt = (MojoShaderMiscTypeType)regNum;
                if (mt == MojoShaderMiscTypeType.Face)
                {
                    ctx.PushOutput(MojoShaderProfileOutput.MainLineIntro);
                    ctx.Indent++;
                    ctx.OutputLine("float {0} = gl_FrontFacing ? 1.0 : -1.0;", var);
                    ctx.PopOutput();
                    return;
                }

                if (mt == MojoShaderMiscTypeType.Position)
                {
                    ctx.PushOutput(MojoShaderProfileOutput.Globals);
                    ctx.OutputLine("uniform vec2 vposFlip;");
                    ctx.PopOutput();

                    // TODO: For half-pixel offset compensation, floor() this value!
                    ctx.PushOutput(MojoShaderProfileOutput.MainLineIntro);
                    ctx.Indent++;
                    ctx.OutputLine("vec4 {0} = vec4(gl_FragCoord.x, (gl_FragCoord.y * vposFlip.x) + vposFlip.y, " +
                                   "gl_FragCoord.z, gl_FragCoord.w);", var);
                    ctx.PopOutput();
                    return;
                }

                ctx.Fail("BUG: unhandled misc register");
            }

            else
            {
                ctx.Fail("unknown pixel shader attribute register");
            }

            ctx.PushOutput(MojoShaderProfileOutput.Globals);
            // no mapping to built-in var? Just make it a regular global, pray.
            if (usageStr == null)
            {
                if (supportGlslEs)
                    ctx.OutputLine("varying highp vec4 io_{0}_{1};", usage, index);
                else
                    ctx.OutputLine("varying vec4 io_{0}_{1};", usage, index);
                ctx.OutputLine("#define {0} io_{1}_{2}", var, usage, index);
            }
            else
            {
                ctx.OutputLine("#define {0} {1}{2}{3}{4}", var, usageStr,
                    arrayLeft, indexStr, arrayRight);
            }

            ctx.PopOutput();
        }

        else
        {
            ctx.Fail("Unknown shader type"); // state machine should catch this.
        }
    }

    public void EmitNop(IMojoShaderContext ctx)
    {
        // no-op is a no-op.  :)
    }

    public void EmitMov(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var code = MakeDestArgAssign(ctx, src0);
        ctx.OutputLine(code);
    }

    public void EmitAdd(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);
        var code = MakeDestArgAssign(ctx, "{0} + {1}", src0!, src1!);
        ctx.OutputLine(code);
    }

    public void EmitSub(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);
        var code = MakeDestArgAssign(ctx, "{0} - {1}", src0!, src1!);
        ctx.OutputLine(code);
    }

    public void EmitMad(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);
        var src2 = MakeSrcArgStringMasked(ctx, 2);
        var code = MakeDestArgAssign(ctx, "({0} * {1}) + {2}", src0!, src1!, src2!);
        ctx.OutputLine(code);
    }

    public void EmitMul(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);
        var code = MakeDestArgAssign(ctx, "{0} * {1}", src0!, src1!);
        ctx.OutputLine(code);
    }

    public void EmitRcp(IMojoShaderContext ctx)
    {
        var vecSize = ctx.DestArg!.WriteMask.VecSize;
        var cast = vecSize != 1
            ? $"vec{vecSize}"
            : string.Empty;
        var src0 = MakeSrcArgStringScalar(ctx, 0);
        ctx.NeedsMaxFloat = true;
        var code = MakeDestArgAssign(ctx, "{0}(({1} == 0.0) ? FLT_MAX : 1.0 / {2})", cast, src0!, src0!);
        ctx.OutputLine(code);
    }

    public void EmitRsq(IMojoShaderContext ctx)
    {
        var vecSize = ctx.DestArg!.WriteMask.VecSize;
        var cast = vecSize != 1
            ? $"vec{vecSize}"
            : string.Empty;
        var src0 = MakeSrcArgStringScalar(ctx, 0);
        ctx.NeedsMaxFloat = true;
        var code = MakeDestArgAssign(ctx, "{0}(({1} == 0.0) ? FLT_MAX : inversesqrt(abs({2})))", cast, src0!, src0!);
        ctx.OutputLine(code);
    }

    public void EmitDotProd(IMojoShaderContext ctx, string? src0, string? src1, string extra)
    {
        var vecSize = ctx.DestArg!.WriteMask.VecSize;
        var castLeft = string.Empty;
        var castRight = string.Empty;
        if (vecSize != 1)
        {
            castLeft = $"vec{vecSize}(";
            castRight = ")";
        }

        var code = MakeDestArgAssign(ctx, "{0}dot({1}, {2}){3}{4}", castLeft, src0, src1, extra, castRight);
        ctx.OutputLine(code);
    }

    public void EmitDp3(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringVec3(ctx, 0);
        var src1 = MakeSrcArgStringVec3(ctx, 1);
        EmitDotProd(ctx, src0, src1, string.Empty);
    }

    public void EmitDp4(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringFull(ctx, 0);
        var src1 = MakeSrcArgStringFull(ctx, 1);
        EmitDotProd(ctx, src0, src1, string.Empty);
    }

    public void EmitMin(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);
        var code = MakeDestArgAssign(ctx, "min({0}, {1})", src0, src1);
        ctx.OutputLine(code);
    }

    public void EmitMax(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);
        var code = MakeDestArgAssign(ctx, "max({0}, {1})", src0, src1);
        ctx.OutputLine(code);
    }

    public void EmitSlt(IMojoShaderContext ctx)
    {
        var vecSize = ctx.DestArg!.WriteMask.VecSize;
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);

        // float(bool) or vec(bvec) results in 0.0 or 1.0, like SLT wants.
        var code = vecSize == 1
            ? MakeDestArgAssign(ctx, "float({0} < {1})", src0, src1)
            : MakeDestArgAssign(ctx, "vec{0}(lessThan({1}, {2}))", vecSize, src0, src1);
        ctx.OutputLine(code);
    }

    public void EmitSge(IMojoShaderContext ctx)
    {
        var vecSize = ctx.DestArg!.WriteMask.VecSize;
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);

        // float(bool) or vec(bvec) results in 0.0 or 1.0, like SGE wants.
        var code = vecSize == 1
            ? MakeDestArgAssign(ctx, "float({0} >= {1})", src0, src1)
            : MakeDestArgAssign(ctx, "vec{0}(greaterThanEqual({1}, {2}))", vecSize, src0, src1);
        ctx.OutputLine(code);
    }

    public void EmitExp(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var code = MakeDestArgAssign(ctx, "exp2({0})", src0);
        ctx.OutputLine(code);
    }

    public void EmitLog(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var code = MakeDestArgAssign(ctx, "log2({0})", src0);
        ctx.OutputLine(code);
    }

    public void EmitLitHelper(IMojoShaderContext ctx)
    {
        if (ctx is not MojoShaderGlslContext glCtx ||
            glCtx.GlslGeneratedLitHelper)
            return;
        glCtx.GlslGeneratedLitHelper = true;

        const string maxP = "127.9961"; // value from the dx9 reference.

        ctx.PushOutput(MojoShaderProfileOutput.Helpers);
        ctx.OutputLine("vec4 LIT(const vec4 src)");
        ctx.OutputLine("{");
        ctx.Indent++;
        ctx.OutputLine("float power = clamp(src.w, -{0}, {1});", maxP, maxP);
        ctx.OutputLine("vec4 retval = vec4(1.0, 0.0, 0.0, 1.0);");
        ctx.OutputLine("if (src.x > 0.0) {");
        ctx.Indent++;
        ctx.OutputLine("retval.y = src.x;");
        ctx.OutputLine("if (src.y > 0.0) {");
        ctx.Indent++;
        ctx.OutputLine("retval.z = pow(src.y, power);");
        ctx.Indent--;
        ctx.OutputLine("}");
        ctx.Indent--;
        ctx.OutputLine("}");
        ctx.OutputLine("return retval;");
        ctx.Indent--;
        ctx.OutputLine("}");
        ctx.OutputBlankLine();
        ctx.PopOutput();
    }

    public void EmitLit(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringFull(ctx, 0);
        EmitLitHelper(ctx);
        var code = MakeDestArgAssign(ctx, "LIT({0})", src0);
        ctx.OutputLine(code);
    }

    public void EmitDst(IMojoShaderContext ctx)
    {
        // !!! FIXME: needs to take ctx->dst_arg.writemask into account.
        var src0Y = MakeSrcArgStringY(ctx, 0);
        var src1Y = MakeSrcArgStringY(ctx, 1);
        var src0Z = MakeSrcArgStringZ(ctx, 0);
        var src1W = MakeSrcArgStringW(ctx, 1);
        var code = MakeDestArgAssign(ctx, "vec4(1.0, {0} * {1}, {2}, {3})",
            src0Y, src1Y, src0Z, src1W);
        ctx.OutputLine(code);
    }

    public void EmitLrp(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);
        var src2 = MakeSrcArgStringMasked(ctx, 2);
        var code = MakeDestArgAssign(ctx, "mix({0}, {1}, {2})", src2, src1, src0);
        ctx.OutputLine(code);
    }

    public void EmitFrc(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var code = MakeDestArgAssign(ctx, "fract({0})", src0);
        ctx.OutputLine(code);
    }

    public void EmitM4X4(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringFull(ctx, 0);
        var row0 = MakeSrcArgStringFull(ctx, 1);
        var row1 = MakeSrcArgStringFull(ctx, 2);
        var row2 = MakeSrcArgStringFull(ctx, 3);
        var row3 = MakeSrcArgStringFull(ctx, 4);
        var code = MakeDestArgAssign(ctx, "vec4(dot({0}, {1}), dot({2}, {3}), dot({4}, {5}), dot({6}, {7}))",
            src0, row0, src0, row1, src0, row2, src0, row3);
        ctx.OutputLine(code);
    }

    public void EmitM4X3(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringFull(ctx, 0);
        var row0 = MakeSrcArgStringFull(ctx, 1);
        var row1 = MakeSrcArgStringFull(ctx, 2);
        var row2 = MakeSrcArgStringFull(ctx, 3);
        var code = MakeDestArgAssign(ctx, "vec3(dot({0}, {1}), dot({2}, {3}), dot({4}, {5}))",
            src0, row0, src0, row1, src0, row2);
        ctx.OutputLine(code);
    }

    public void EmitM3X4(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringVec3(ctx, 0);
        var row0 = MakeSrcArgStringVec3(ctx, 1);
        var row1 = MakeSrcArgStringVec3(ctx, 2);
        var row2 = MakeSrcArgStringVec3(ctx, 3);
        var row3 = MakeSrcArgStringVec3(ctx, 4);
        var code = MakeDestArgAssign(ctx, "vec4(dot({0}, {1}), dot({2}, {3}), dot({4}, {5}), dot({6}, {7}))",
            src0, row0, src0, row1, src0, row2, src0, row3);
        ctx.OutputLine(code);
    }

    public void EmitM3X3(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringVec3(ctx, 0);
        var row0 = MakeSrcArgStringVec3(ctx, 1);
        var row1 = MakeSrcArgStringVec3(ctx, 2);
        var row2 = MakeSrcArgStringVec3(ctx, 3);
        var code = MakeDestArgAssign(ctx, "vec3(dot({0}, {1}), dot({2}, {3}), dot({4}, {5}))",
            src0, row0, src0, row1, src0, row2);
        ctx.OutputLine(code);
    }

    public void EmitM3X2(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringVec3(ctx, 0);
        var row0 = MakeSrcArgStringVec3(ctx, 1);
        var row1 = MakeSrcArgStringVec3(ctx, 2);
        var code = MakeDestArgAssign(ctx, "vec2(dot({0}, {1}), dot({2}, {3}))",
            src0, row0, src0, row1);
        ctx.OutputLine(code);
    }

    public void EmitCall(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        if (ctx.Loops > 0)
            ctx.OutputLine("{0}(aL);", src0);
        else
            ctx.OutputLine("{0}();", src0);
    }

    public void EmitCallNz(IMojoShaderContext ctx)
    {
        // !!! FIXME: if src1 is a constbool that's true, we can remove the
        // !!! FIXME:  if. If it's false, we can make this a no-op.
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);

        if (ctx.Loops > 0)
            ctx.OutputLine("if ({0}) {{ {1}(aL); }}", src1, src0);
        else
            ctx.OutputLine("if ({0}) {{ {1}(); }}", src1, src0);
    }

    public void EmitLoop(IMojoShaderContext ctx)
    {
        // !!! FIXME: swizzle?
        var var = GetSrcArgVarName(ctx, 1);
        Debug.Assert(ctx.SourceArgs[0]?.RegNum == 0); // in case they add aL1 someday.
        ctx.OutputLine("{");
        ctx.Indent++;
        ctx.OutputLine("const int aLend = {0}.x + {1}.y;", var, var);
        ctx.OutputLine("for (int aL = {0}.y; aL < aLend; aL += {1}.z) {{", var, var);
        ctx.Indent++;
    }

    /// <summary>
    /// [emit_GLSL_RET; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitRet(IMojoShaderContext ctx)
    {
        // thankfully, the MSDN specs say a RET _has_ to end a function...no
        //  early returns. So if you hit one, you know you can safely close
        //  a high-level function.
        ctx.Indent--;
        ctx.OutputLine("}");
        ctx.OutputBlankLine();
        // !!! FIXME: is this for LABEL? Maybe set it there so we don't allocate unnecessarily.
        ctx.SetOutput(MojoShaderProfileOutput.Subroutines);
    }

    /// <summary>
    /// [emit_GLSL_ENDLOOP; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitEndLoop(IMojoShaderContext ctx)
    {
        ctx.Indent--;
        ctx.OutputLine("}");
        ctx.Indent--;
        ctx.OutputLine("}");
    }

    /// <summary>
    /// [emit_GLSL_LABEL; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitLabel(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var label = ctx.SourceArgs[0]!.RegNum;
        var reg = ctx.RegListFind(ctx.UsedRegisters, MojoShaderRegisterType.Label, label);
        Debug.Assert(ctx.Output == ctx.Subroutines); // not mainline, etc.
        Debug.Assert(ctx.Indent == 0); // we shouldn't be in the middle of a function.

        // MSDN specs say CALL* has to come before the LABEL, so we know if we
        //  can ditch the entire function here as unused.
        if (reg == null)
            ctx.SetOutput(MojoShaderProfileOutput.Ignore); // Func not used. Parse, but don't output.

        // !!! FIXME: it would be nice if we could determine if a function is
        // !!! FIXME:  only called once and, if so, forcibly inline it.

        var usesLoopReg = reg is { Misc: 1 } ? "int aL" : "";
        ctx.OutputLine("void {0}({1})", src0, usesLoopReg);
        ctx.OutputLine("{");
        ctx.Indent++;
    }

    /// <summary>
    /// [emit_GLSL_DCL; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitDcl(IMojoShaderContext ctx)
    {
        // no-op. We do this in our emit_attribute() and emit_uniform().
    }

    /// <summary>
    /// [emit_GLSL_POW; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitPow(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);
        var code = MakeDestArgAssign(ctx, "pow(abs({0}), {1})", src0, src1);
        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_CRS; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitCrs(IMojoShaderContext ctx)
    {
        // !!! FIXME: needs to take ctx->dst_arg.writemask into account.
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);
        var code = MakeDestArgAssign(ctx, "cross({0}, {1})", src0, src1);
        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_SGN; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitSgn(IMojoShaderContext ctx)
    {
        // (we don't need the temporary registers specified for the D3D opcode.)
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var code = MakeDestArgAssign(ctx, "sign({0})", src0);
        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_ABS; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitAbs(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var code = MakeDestArgAssign(ctx, "abs({0})", src0);
        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_NRM; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitNrm(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var code = MakeDestArgAssign(ctx, "normalize({0})", src0);
        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_SINCOS; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitSinCos(IMojoShaderContext ctx)
    {
        // we don't care about the temp registers that <= sm2 demands; ignore them.
        //  sm2 also talks about what components are left untouched vs. undefined,
        //  but we just leave those all untouched with GLSL write masks (which
        //  would fulfill the "undefined" requirement, too).
        var mask = ctx.DestArg!.WriteMask;
        var src0 = MakeSrcArgStringScalar(ctx, 0);
        var code = string.Empty;

        if (mask.IsX)
            MakeDestArgAssign(ctx, "cos({0})", src0);
        else if (mask.IsY)
            MakeDestArgAssign(ctx, "sin({0})", src0);
        else if (mask.IsXy)
            MakeDestArgAssign(ctx, "vec2(cos({0}), sin({1}))", src0, src0);

        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_REP; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitRep(IMojoShaderContext ctx)
    {
        // !!! FIXME:
        // msdn docs say legal loop values are 0 to 255. We can check DEFI values
        //  at parse time, but if they are pulling a value from a uniform, do
        //  we clamp here?
        // !!! FIXME: swizzle is legal here, right?
        var src0 = MakeSrcArgStringX(ctx, 0);
        var rep = ctx.Reps;
        ctx.OutputLine("for (int rep{0} = 0; rep{1} < {2}; rep{3}++) {{",
            rep, rep, src0, rep);
        ctx.Indent++;
    }

    /// <summary>
    /// [emit_GLSL_ENDREP; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitEndRep(IMojoShaderContext ctx)
    {
        ctx.Indent--;
        ctx.OutputLine("}");
    }

    /// <summary>
    /// [emit_GLSL_IF; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitIf(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringScalar(ctx, 0);
        ctx.OutputLine("if ({0}) {{", src0);
        ctx.Indent++;
    }

    /// <summary>
    /// [emit_GLSL_IFC; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitIfC(IMojoShaderContext ctx)
    {
        var comp = GetComparisonStringScalar(ctx);
        var src0 = MakeSrcArgStringScalar(ctx, 0);
        var src1 = MakeSrcArgStringScalar(ctx, 1);
        ctx.OutputLine("if ({0} {1} {2}) {{", src0, comp, src1);
        ctx.Indent++;
    }

    /// <summary>
    /// [emit_GLSL_ELSE; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitElse(IMojoShaderContext ctx)
    {
        ctx.Indent--;
        ctx.OutputLine("} else {");
        ctx.Indent++;
    }

    /// <summary>
    /// [emit_GLSL_ENDIF; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitEndIf(IMojoShaderContext ctx)
    {
        ctx.Indent--;
        ctx.OutputLine("}");
    }

    /// <summary>
    /// [emit_GLSL_BREAK; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitBreak(IMojoShaderContext ctx)
    {
        ctx.OutputLine("break;");
    }

    /// <summary>
    /// [emit_GLSL_BREAKC; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitBreakC(IMojoShaderContext ctx)
    {
        var comp = GetComparisonStringScalar(ctx);
        var src0 = MakeSrcArgStringScalar(ctx, 0);
        var src1 = MakeSrcArgStringScalar(ctx, 1);
        ctx.OutputLine("if ({0} {1} {2}) {{ break; }}", src0, comp, src1);
    }

    /// <summary>
    /// [emit_GLSL_MOVA; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitMovA(IMojoShaderContext ctx)
    {
        var vecSize = ctx.DestArg!.WriteMask.VecSize;
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        string? code;

        if (vecSize == 1)
            code = MakeDestArgAssign(ctx, "int(floor(abs({0}) + 0.5) * sign({1}))",
                src0, src0);
        else
            code = MakeDestArgAssign(ctx, "ivec{0}(floor(abs({1}) + vec{2}(0.5)) * sign({3}))",
                vecSize, src0, vecSize, src0);

        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_DEFB; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitDefB(IMojoShaderContext ctx)
    {
        var varName = GetDestArgVarName(ctx);
        ctx.PushOutput(MojoShaderProfileOutput.Globals);
        ctx.OutputLine("const bool {0} = {1};",
            varName, ctx.Dwords[0] != 0 ? "true" : "false");
        ctx.PopOutput();
    }

    /// <summary>
    /// [emit_GLSL_DEFI; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitDefI(IMojoShaderContext ctx)
    {
        var varName = GetDestArgVarName(ctx);
        var x = ctx.Dwords;
        ctx.PushOutput(MojoShaderProfileOutput.Globals);
        ctx.OutputLine("const ivec4 {0} = ivec4({1}, {2}, {3}, {4});",
            varName, x[0], x[1], x[2], x[3]);
        ctx.PopOutput();
    }

    /// <summary>
    /// [macro EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXCRD); mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexCrd(IMojoShaderContext ctx) =>
        EmitUnimplemented(ctx, "TEXCRD");

    /// <summary>
    /// [emit_GLSL_TEXKILL; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexKill(IMojoShaderContext ctx)
    {
        var dst = GetDestArgVarName(ctx);
        ctx.OutputLine("if (any(lessThan({0}.xyz, vec3(0.0)))) discard;", dst);
    }

    public void EmitTexLd(IMojoShaderContext ctx)
    {
        if (!ctx.ShaderVersionAtLeast(1, 4))
        {
            var info = ctx.DestArg!;
            string? code = null;

            var sreg = ctx.RegListFind(ctx.Samplers, MojoShaderRegisterType.Sampler, info.RegNum);
            var tType = sreg != null
                ? (MojoShaderTextureType)sreg.Index
                : 0;

            // !!! FIXME: this code counts on the register not having swizzles, etc.
            var dst = GetDestArgVarName(ctx);
            var sampler = GetVarName(ctx, MojoShaderRegisterType.Sampler, info.RegNum);

            switch (tType)
            {
                case MojoShaderTextureType.TwoD:
                    code = MakeDestArgAssign(ctx, "texture2D({0}, {1}.xy)", sampler, dst);
                    break;
                case MojoShaderTextureType.Cube:
                    code = MakeDestArgAssign(ctx, "textureCube({0}, {1}.xyz)", sampler, dst);
                    break;
                case MojoShaderTextureType.Volume:
                    code = MakeDestArgAssign(ctx, "texture3D({0}, {1}.xyz)", sampler, dst);
                    break;
                default:
                    ctx.Fail("unexpected texture type");
                    break;
            }

            ctx.OutputLine(code);
        }

        else
        {
            if (!ctx.ShaderVersionAtLeast(2, 0))
            {
                // ps_1_4 is different, too!
                ctx.Fail("TEXLD == Shader Model 1.4 unimplemented."); // !!! FIXME
                return;
            }

            var sampArg = ctx.SourceArgs[1]!;
            var sreg = ctx.RegListFind(ctx.Samplers, MojoShaderRegisterType.Sampler, sampArg.RegNum);
            string? src0;
            var src1 = GetSrcArgVarName(ctx, 1); // !!! FIXME: SRC_MOD?

            if (sreg == null)
            {
                ctx.Fail("TEXLD using undeclared sampler");
                return;
            }

            // !!! FIXME: does the d3d bias value map directly to GLSL?
            var biasSep = string.Empty;
            var bias = string.Empty;

            if (ctx.InstructionControls == (int)MojoShaderTexLdControl.TexLdB)
            {
                biasSep = ", ";
                bias = MakeSrcArgStringW(ctx, 0);
            }

            string? funcName;
            switch ((MojoShaderTextureType)sreg.Index)
            {
                case MojoShaderTextureType.TwoD:
                    if (ctx.InstructionControls == (int)MojoShaderTexLdControl.TexLdP)
                    {
                        funcName = "texture2DProj";
                        src0 = MakeSrcArgStringFull(ctx, 0);
                    }
                    else // texld/texldb
                    {
                        funcName = "texture2D";
                        src0 = MakeSrcArgStringVec2(ctx, 0);
                    }

                    break;
                case MojoShaderTextureType.Cube:
                    if (ctx.InstructionControls == (int)MojoShaderTexLdControl.TexLdP)
                        ctx.Fail("TEXLDP on a cubemap"); // !!! FIXME: is this legal?
                    funcName = "textureCube";
                    src0 = MakeSrcArgStringVec3(ctx, 0);
                    break;
                case MojoShaderTextureType.Volume:
                    if (ctx.InstructionControls == (int)MojoShaderTexLdControl.TexLdP)
                    {
                        funcName = "texture3DProj";
                        src0 = MakeSrcArgStringFull(ctx, 0);
                    }
                    else // texld/texldb
                    {
                        funcName = "texture3D";
                        src0 = MakeSrcArgStringVec3(ctx, 0);
                    }

                    break;
                default:
                    ctx.Fail("unknown texture type");
                    return;
            }

            Debug.Assert(!ctx.IsScalar(ctx.ShaderType, sampArg.RegType, sampArg.RegNum));
            var swizStr = MakeSwizzleString(ctx, sampArg.Swizzle, ctx.DestArg!.WriteMask);
            var code = MakeDestArgAssign(ctx, "{0}({1}, {2}{3}{4}){5}",
                funcName, src1, src0, biasSep, bias, swizStr);

            ctx.OutputLine(code);
        }
    }

    /// <summary>
    /// [emit_GLSL_TEXBEM; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexBem(IMojoShaderContext ctx)
    {
        var info = ctx.DestArg!;
        var dst = GetDestArgVarName(ctx);
        var src = GetSrcArgVarName(ctx, 0);

        // !!! FIXME: this code counts on the register not having swizzles, etc.
        var sampler = GetVarName(ctx, MojoShaderRegisterType.Sampler, info.RegNum);
        var code = MakeDestArgAssign(ctx,
            "texture2D({0}, vec2({1}.x + ({2}_texbem.x * {3}.x) + ({4}_texbem.z * {5}.y)," +
            " {6}.y + ({7}_texbem.y * {8}.x) + ({9}_texbem.w * {10}.y)))",
            sampler,
            dst, sampler, src, sampler, src,
            dst, sampler, src, sampler, src);

        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_TEXBEML; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexBemL(IMojoShaderContext ctx)
    {
        // !!! FIXME: this code counts on the register not having swizzles, etc.
        var info = ctx.DestArg!;
        var dst = GetDestArgVarName(ctx);
        var src = GetSrcArgVarName(ctx, 0);
        var sampler = GetVarName(ctx, MojoShaderRegisterType.Sampler, info.RegNum);
        var code = MakeDestArgAssign(ctx,
            "(texture2D({0}, vec2({1}.x + ({2}_texbem.x * {3}.x) + ({4}_texbem.z * {5}.y)," +
            " {6}.y + ({7}_texbem.y * {8}.x) + ({9}_texbem.w * {10}.y)))) *" +
            " (({11}.z * {12}_texbeml.x) + {13}_texbem.y)",
            sampler,
            dst, sampler, src, sampler, src,
            dst, sampler, src, sampler, src,
            src, sampler, sampler);

        ctx.OutputLine(code);
    }

    public void EmitTexReg2Ar(IMojoShaderContext ctx) =>
        EmitUnimplemented(ctx, "TEXREG2AR"); // !!! FIXME

    public void EmitTexReg2Gb(IMojoShaderContext ctx) =>
        EmitUnimplemented(ctx, "TEXREG2GB"); // !!! FIXME

    /// <summary>
    /// [emit_GLSL_TEXM3X2PAD; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexM3X2Pad(IMojoShaderContext ctx)
    {
        // no-op ... work happens in emit_GLSL_TEXM3X2TEX().
    }

    /// <summary>
    /// [emit_GLSL_TEXM3X2TEX; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexM3X2Tex(IMojoShaderContext ctx)
    {
        if (ctx.TexM3X2PadSrc0 == -1)
            return;

        var info = ctx.DestArg!;

        // !!! FIXME: this code counts on the register not having swizzles, etc.
        var sampler = GetVarName(ctx, MojoShaderRegisterType.Sampler, info.RegNum);
        var src0 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X2PadSrc0);
        var src1 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X2PadDst0);
        var src2 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.SourceArgs[0]!.RegNum);
        var dst = GetDestArgVarName(ctx);
        var code = MakeDestArgAssign(ctx,
            "texture2D({0}, vec2(dot({1}.xyz, {2}.xyz), dot({3}.xyz, {4}.xyz)))",
            sampler, src0, src1, src2, dst);
        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_TEXM3X3PAD; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexM3X3Pad(IMojoShaderContext ctx)
    {
        // no-op ... work happens in emit_GLSL_TEXM3X3*().
    }

    /// <summary>
    /// [emit_GLSL_TEXM3X3TEX; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexM3X3Tex(IMojoShaderContext ctx)
    {
        if (ctx.TexM3X3PadSrc1 < 0)
            return;

        var info = ctx.DestArg!;

        // !!! FIXME: this code counts on the register not having swizzles, etc.
        var sampler = GetVarName(ctx, MojoShaderRegisterType.Sampler, info.RegNum);
        var src0 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadDst0);
        var src1 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadSrc0);
        var src2 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadDst1);
        var src3 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadSrc1);
        var src4 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.SourceArgs[0]!.RegNum);
        var dst = GetDestArgVarName(ctx);

        var sreg = ctx.RegListFind(ctx.Samplers, MojoShaderRegisterType.Sampler, info.RegNum);
        var tType = (MojoShaderTextureType)(sreg?.Index ?? 0);
        var tTypeStr = tType == MojoShaderTextureType.Cube ? "Cube" : "3D";

        var code = MakeDestArgAssign(ctx,
            "texture{0}({1}," +
            " vec3(dot({2}.xyz, {3}.xyz)," +
            " dot({4}.xyz, {5}.xyz)," +
            " dot({6}.xyz, {7}.xyz)))",
            tTypeStr, sampler,
            src0, src1,
            src2, src3,
            dst, src4);

        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_TEXM3X3SPEC_helper; mojoshacer_profile_glsl.c]
    /// </summary>
    public void EmitTexM3X3SpecHelper(IMojoShaderContext ctx)
    {
        if (ctx is not MojoShaderGlslContext glCtx ||
            glCtx.GlslGeneratedTexM3x3SpecHelper)
            return;

        glCtx.GlslGeneratedTexM3x3SpecHelper = true;

        ctx.PushOutput(MojoShaderProfileOutput.Helpers);
        ctx.OutputLine("vec3 TEXM3X3SPEC_reflection(const vec3 normal, const vec3 eyeray)");
        ctx.OutputLine("{");
        ctx.Indent++;
        ctx.OutputLine("return (2.0 * ((normal * eyeray) / (normal * normal)) * normal) - eyeray;");
        ctx.Indent--;
        ctx.OutputLine("}");
        ctx.OutputBlankLine();
        ctx.PopOutput();
    }

    /// <summary>
    /// [emit_GLSL_TEXM3X3SPEC; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexM3X3Spec(IMojoShaderContext ctx)
    {
        if (ctx.TexM3X3PadSrc1 < 0)
            return;

        var info = ctx.DestArg!;
        EmitTexM3X3SpecHelper(ctx);

        // !!! FIXME: this code counts on the register not having swizzles, etc.

        var sampler = GetVarName(ctx, MojoShaderRegisterType.Sampler, info.RegNum);
        var src0 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadDst0);
        var src1 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadSrc0);
        var src2 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadDst1);
        var src3 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadSrc1);
        var src4 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.SourceArgs[0]!.RegNum);
        var src5 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.SourceArgs[1]!.RegNum);
        var dst = GetDestArgVarName(ctx);
        var sreg = ctx.RegListFind(ctx.Samplers, MojoShaderRegisterType.Sampler, info.RegNum);
        var tType = (MojoShaderTextureType)(sreg?.Index ?? 0);
        var tTypeStr = tType == MojoShaderTextureType.Cube ? "Cube" : "3D";

        var code = MakeDestArgAssign(ctx,
            "texture{0}({1}, " +
            /**/ "TEXM3X3SPEC_reflection(" +
            /*    */ "vec3(" +
            /*        */ "dot({2}.xyz, {3}.xyz), " +
            /*        */ "dot({4}.xyz, {5}.xyz), " +
            /*        */ "dot({6}.xyz, {7}.xyz)" +
            /*    */ ")," +
            /*    */ "{8}.xyz," +
            /**/ ")" +
            ")",
            tTypeStr, sampler, src0, src1, src2, src3, dst, src4, src5);

        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_TEXM3X3VSPEC; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexM3X3VSpec(IMojoShaderContext ctx)
    {
        if (ctx.TexM3X3PadSrc1 < 0)
            return;

        var info = ctx.DestArg!;
        EmitTexM3X3SpecHelper(ctx);

        // !!! FIXME: this code counts on the register not having swizzles, etc.

        var sampler = GetVarName(ctx, MojoShaderRegisterType.Sampler, info.RegNum);
        var src0 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadDst0);
        var src1 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadSrc0);
        var src2 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadDst1);
        var src3 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadSrc1);
        var src4 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.SourceArgs[0]!.RegNum);
        var dst = GetDestArgVarName(ctx);
        var sreg = ctx.RegListFind(ctx.Samplers, MojoShaderRegisterType.Sampler, info.RegNum);
        var tType = (MojoShaderTextureType)(sreg?.Index ?? 0);
        var tTypeStr = tType == MojoShaderTextureType.Cube ? "Cube" : "3D";

        var code = MakeDestArgAssign(ctx,
            "texture{0}({1}, " +
            /**/ "TEXM3X3SPEC_reflection(" +
            /*    */ "vec3(" +
            /*        */ "dot({2}.xyz, {3}.xyz), " +
            /*        */ "dot({4}.xyz, {5}.xyz), " +
            /*        */ "dot({6}.xyz, {7}.xyz)" +
            /*    */ "), " +
            /*    */ "vec3({8}.w, {9}.w, {10}.w)" +
            /**/ ")" +
            ")",
            tTypeStr, sampler, src0, src1, src2, src3, dst, src4, src0, src2, dst);

        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_EXPP; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitExpP(IMojoShaderContext ctx)
    {
        // !!! FIXME: msdn's asm docs don't list this opcode, I'll have to check the driver documentation.
        EmitExp(ctx); // I guess this is just partial precision EXP?
    }

    /// <summary>
    /// [emit_GLSL_LOGP; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitLogP(IMojoShaderContext ctx)
    {
        // LOGP is just low-precision LOG, but we'll take the higher precision.
        EmitLog(ctx);
    }

    /// <summary>
    /// [emit_GLSL_comparison_operations; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitComparisonOperations(IMojoShaderContext ctx, string cmp)
    {
        int i;
        var dst = ctx.DestArg!;
        var srcArg0 = ctx.SourceArgs[0]!;
        var origMask = dst.WriteMask;
        var usedSwiz = new bool[4];
        var writeMask = dst.WriteMask;
        var src0Swiz = srcArg0.Swizzle;

        for (i = 0; i < 4; i++)
        {
            var mask = 1 << i;

            if (!writeMask[i])
                continue;
            if (usedSwiz[i])
                continue;

            // This is a swizzle we haven't checked yet.
            usedSwiz[i] = true;

            // see if there are any other elements swizzled to match (.yyyy)
            int j;
            for (j = i + 1; j < 4; j++)
            {
                if (!writeMask[j])
                    continue;
                if (src0Swiz[i] != src0Swiz[j])
                    continue;
                mask |= 1 << j;
                usedSwiz[j] = true;
            }

            // okay, (mask) should be the writemask of swizzles we like.

            var src0 = MakeSrcArgString(ctx, 0, 1 << i);
            var src1 = MakeSrcArgString(ctx, 1, mask);
            var src2 = MakeSrcArgString(ctx, 2, mask);

            ctx.SetDstArgWriteMask(dst, mask);

            var code = MakeDestArgAssign(ctx,
                "(({0} {1}) ? {2} : {3})",
                src0, cmp, src1, src2);
            ctx.OutputLine(code);
        }

        ctx.SetDstArgWriteMask(dst, origMask);
    }

    /// <summary>
    /// [emit_GLSL_CND; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitCnd(IMojoShaderContext ctx) =>
        EmitComparisonOperations(ctx, "> 0.5");

    /// <summary>
    /// [emit_GLSL_DEF; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitDef(IMojoShaderContext ctx)
    {
        var val = MemoryMarshal.Cast<int, float>(ctx.Dwords.AsSpan()); // !!! FIXME: could be int?
        var varName = GetDestArgVarName(ctx);
        var val0 = val[0].FloatStr(true);
        var val1 = val[1].FloatStr(true);
        var val2 = val[2].FloatStr(true);
        var val3 = val[3].FloatStr(true);

        ctx.PushOutput(MojoShaderProfileOutput.Globals);
        ctx.OutputLine("const vec4 {0} = vec4({1}, {2}, {3}, {4});",
            varName, val0, val1, val2, val3);
        ctx.PopOutput();
    }

    /// <summary>
    /// [macro EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXREG2RGB); mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexReg2Rgb(IMojoShaderContext ctx) =>
        EmitUnimplemented(ctx, "TEXREG2RGB"); // !!! FIXME

    /// <summary>
    /// [macro EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXDP3TEX); mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexDp3Tex(IMojoShaderContext ctx) =>
        EmitUnimplemented(ctx, "TEXDP3TEX"); // !!! FIXME

    /// <summary>
    /// [macro EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXM3X2DEPTH); mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexM3X2Depth(IMojoShaderContext ctx) =>
        EmitUnimplemented(ctx, "TEXM3X2DEPTH"); // !!! FIXME

    /// <summary>
    /// [macro EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXDP3); mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexDp3(IMojoShaderContext ctx) =>
        EmitUnimplemented(ctx, "TEXDP3"); // !!! FIXME

    /// <summary>
    /// [emit_GLSL_TEXM3X3; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexM3X3(IMojoShaderContext ctx)
    {
        if (ctx.TexM3X3PadSrc1 < 0)
            return;

        // !!! FIXME: this code counts on the register not having swizzles, etc.
        var src0 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadDst0);
        var src1 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadSrc0);
        var src2 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadDst1);
        var src3 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.TexM3X3PadSrc1);
        var src4 = GetVarName(ctx, MojoShaderRegisterType.Texture, ctx.SourceArgs[0]!.RegNum);
        var dst = GetDestArgVarName(ctx);
        var code = MakeDestArgAssign(ctx,
            "vec4(dot({0}.xyz, {1}.xyz), dot({2}.xyz, {3}.xyz), dot({4}.xyz, {5}.xyz), 1.0)",
            src0, src1, src2, src3, dst, src4);

        ctx.OutputLine(code);
    }

    /// <summary>
    /// [macro EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXDEPTH); mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexDepth(IMojoShaderContext ctx) =>
        EmitUnimplemented(ctx, "TEXDEPTH"); // !!! FIXME

    /// <summary>
    /// [emit_GLSL_CMP; mojoshader_profile_glsl.c]
    /// </summary>
    /// <param name="ctx"></param>
    public void EmitCmp(IMojoShaderContext ctx) =>
        EmitComparisonOperations(ctx, ">= 0.0");

    /// <summary>
    /// [macro EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(BEM); mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitBem(IMojoShaderContext ctx) =>
        EmitUnimplemented(ctx, "BEM"); // !!! FIXME

    /// <summary>
    /// [emit_GLSL_DP2ADD; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitDp2Add(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringVec2(ctx, 0);
        var src1 = MakeSrcArgStringVec2(ctx, 1);
        var src2 = MakeSrcArgStringScalar(ctx, 2);
        var extra = $" + {src2}";
        EmitDotProd(ctx, src0, src1, extra);
    }

    /// <summary>
    /// [emit_GLSL_DSX; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitDsX(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var code = MakeDestArgAssign(ctx, "dFdx({0})", src0);
        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_DSY; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitDsY(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var code = MakeDestArgAssign(ctx, "dFdy({0})", src0);
        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_TEXLDD; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitTexLdd(IMojoShaderContext ctx)
    {
        var sampArg = ctx.SourceArgs[1]!;
        var sreg = ctx.RegListFind(ctx.Samplers, MojoShaderRegisterType.Sampler, sampArg.RegNum);
        string? funcName;
        var src0 = string.Empty;
        var src1 = GetSrcArgVarName(ctx, 1);
        var src2 = string.Empty;
        var src3 = string.Empty;

        if (sreg == null)
        {
            ctx.Fail("TEXLDD using undeclared sampler");
            return;
        }

        switch ((MojoShaderTextureType)sreg.Index)
        {
            case MojoShaderTextureType.TwoD:
                funcName = "texture2D";
                MakeSrcArgStringVec2(ctx, 0);
                MakeSrcArgStringVec2(ctx, 2);
                MakeSrcArgStringVec2(ctx, 3);
                break;
            case MojoShaderTextureType.Cube:
                funcName = "textureCube";
                MakeSrcArgStringVec3(ctx, 0);
                MakeSrcArgStringVec3(ctx, 2);
                MakeSrcArgStringVec3(ctx, 3);
                break;
            case MojoShaderTextureType.Volume:
                funcName = "texture3D";
                MakeSrcArgStringVec3(ctx, 0);
                MakeSrcArgStringVec3(ctx, 2);
                MakeSrcArgStringVec3(ctx, 3);
                break;
            default:
                ctx.Fail("unknown texture type");
                return;
        }

        Debug.Assert(!ctx.IsScalar(ctx.ShaderType, sampArg.RegType, sampArg.RegNum));
        var swizStr = MakeSwizzleString(ctx, sampArg.Swizzle, ctx.DestArg!.WriteMask);

        var code = MakeDestArgAssign(ctx,
            "{0}Grad({1}, {2}, {3}, {4}){5}",
            funcName, src1, src0, src2, src3, swizStr);

        PrependTexLodExtensions(ctx);
        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_SETP; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitSetP(IMojoShaderContext ctx)
    {
        var vecSize = ctx.DestArg!.WriteMask.VecSize;
        var src0 = MakeSrcArgStringMasked(ctx, 0);
        var src1 = MakeSrcArgStringMasked(ctx, 1);
        string? code;

        // destination is always predicate register (which is type bvec4).
        if (vecSize == 1)
        {
            var comp = GetComparisonStringScalar(ctx);
            code = MakeDestArgAssign(ctx, "({0} {1} {2})",
                src0, comp, src1);
        }
        else
        {
            var comp = GetComparisonStringVector(ctx);
            code = MakeDestArgAssign(ctx, "{0}({1}, {2})",
                comp, src0, src1);
        }

        ctx.OutputLine(code);
    }

    public void EmitTexLdl(IMojoShaderContext ctx)
    {
        var sampArg = ctx.SourceArgs[1]!;
        var sreg = ctx.RegListFind(ctx.Samplers, MojoShaderRegisterType.Sampler, sampArg.RegNum);
        string? pattern;
        var src0 = MakeSrcArgStringFull(ctx, 0);
        var src1 = GetSrcArgVarName(ctx, 1);

        if (sreg == null)
        {
            ctx.Fail("TEXLDL using undeclared sampler");
            return;
        }

        // HLSL tex2dlod accepts (sampler, uv.xyz, uv.w) where uv.w is the LOD
        // GLSL seems to want the dimensionality to match the sampler (.xy vs .xyz)
        //  so we vary the swizzle accordingly
        switch ((MojoShaderTextureType)sreg.Index)
        {
            case MojoShaderTextureType.TwoD:
                pattern = "texture2DLod({0}, {1}.xy, {2}.w){3}";
                break;
            case MojoShaderTextureType.Cube:
                pattern = "textureCubeLod({0}, {1}.xyz, {2}.w){3}";
                break;
            case MojoShaderTextureType.Volume:
                pattern = "texture3DLod({0}, {1}.xyz, {2}.w){3}";
                break;
            default:
                ctx.Fail("unknown texture type");
                return;
        }

        Debug.Assert(!ctx.IsScalar(ctx.ShaderType, sampArg.RegType, sampArg.RegNum));
        var swizStr = MakeSwizzleString(ctx, sampArg.Swizzle, ctx.DestArg!.WriteMask);
        var code = MakeDestArgAssign(ctx, pattern, src1, src0, src0, swizStr);
        PrependTexLodExtensions(ctx);
        ctx.OutputLine(code);
    }

    /// <summary>
    /// [emit_GLSL_BREAKP; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitBreakP(IMojoShaderContext ctx)
    {
        var src0 = MakeSrcArgStringScalar(ctx, 0);
        ctx.OutputLine("if ({0}) {{ break; }}", src0);
    }

    /// <summary>
    /// [emit_GLSL_RESERVED; mojoshader_profile_glsl.c]
    /// </summary>
    public void EmitReserved(IMojoShaderContext ctx)
    {
        // do nothing; fails in the state machine.
    }

    public override void EmitFunction(IMojoShaderContext ctx, MojoShaderOpcode op) =>
        (op switch
        {
            MojoShaderOpcode.Reserved => EmitReserved,
            MojoShaderOpcode.Nop => EmitNop,
            MojoShaderOpcode.Mov => EmitMov,
            MojoShaderOpcode.Add => EmitAdd,
            MojoShaderOpcode.Sub => EmitSub,
            MojoShaderOpcode.Mad => EmitMad,
            MojoShaderOpcode.Mul => EmitMul,
            MojoShaderOpcode.Rcp => EmitRcp,
            MojoShaderOpcode.Rsq => EmitRsq,
            MojoShaderOpcode.Dp3 => EmitDp3,
            MojoShaderOpcode.Dp4 => EmitDp4,
            MojoShaderOpcode.Min => EmitMin,
            MojoShaderOpcode.Max => EmitMax,
            MojoShaderOpcode.Slt => EmitSlt,
            MojoShaderOpcode.Sge => EmitSge,
            MojoShaderOpcode.Exp => EmitExp,
            MojoShaderOpcode.Log => EmitLog,
            MojoShaderOpcode.Lit => EmitLit,
            MojoShaderOpcode.Dst => EmitDst,
            MojoShaderOpcode.Lrp => EmitLrp,
            MojoShaderOpcode.Frc => EmitFrc,
            MojoShaderOpcode.M4x4 => EmitM4X4,
            MojoShaderOpcode.M4x3 => EmitM4X3,
            MojoShaderOpcode.M3x4 => EmitM3X4,
            MojoShaderOpcode.M3x3 => EmitM3X3,
            MojoShaderOpcode.M3x2 => EmitM3X2,
            MojoShaderOpcode.Call => EmitCall,
            MojoShaderOpcode.CallNz => EmitCallNz,
            MojoShaderOpcode.Loop => EmitLoop,
            MojoShaderOpcode.Ret => EmitRet,
            MojoShaderOpcode.EndLoop => EmitEndLoop,
            MojoShaderOpcode.Label => EmitLabel,
            MojoShaderOpcode.Dcl => EmitDcl,
            MojoShaderOpcode.Pow => EmitPow,
            MojoShaderOpcode.Crs => EmitCrs,
            MojoShaderOpcode.Sgn => EmitSgn,
            MojoShaderOpcode.Abs => EmitAbs,
            MojoShaderOpcode.Nrm => EmitNrm,
            MojoShaderOpcode.SinCos => EmitSinCos,
            MojoShaderOpcode.Rep => EmitRep,
            MojoShaderOpcode.EndRep => EmitEndRep,
            MojoShaderOpcode.If => EmitIf,
            MojoShaderOpcode.Ifc => EmitIfC,
            MojoShaderOpcode.Else => EmitElse,
            MojoShaderOpcode.EndIf => EmitEndIf,
            MojoShaderOpcode.Break => EmitBreak,
            MojoShaderOpcode.BreakC => EmitBreakC,
            MojoShaderOpcode.MovA => EmitMovA,
            MojoShaderOpcode.DefB => EmitDefB,
            MojoShaderOpcode.DefI => EmitDefI,
            MojoShaderOpcode.TexCrd => EmitTexCrd,
            MojoShaderOpcode.TexKill => EmitTexKill,
            MojoShaderOpcode.TexLd => EmitTexLd,
            MojoShaderOpcode.TexBem => EmitTexBem,
            MojoShaderOpcode.TexBeml => EmitTexBemL,
            MojoShaderOpcode.TexReg2Ar => EmitTexReg2Ar,
            MojoShaderOpcode.TexReg2Gb => EmitTexReg2Gb,
            MojoShaderOpcode.TexM3x2Pad => EmitTexM3X2Pad,
            MojoShaderOpcode.TexM3x2Tex => EmitTexM3X2Tex,
            MojoShaderOpcode.TexM3x3Pad => EmitTexM3X3Pad,
            MojoShaderOpcode.TexM3x3Tex => EmitTexM3X3Tex,
            MojoShaderOpcode.TexM3x3Spec => EmitTexM3X3Spec,
            MojoShaderOpcode.TexM3x3Vspec => EmitTexM3X3VSpec,
            MojoShaderOpcode.ExpP => EmitExpP,
            MojoShaderOpcode.LogP => EmitLogP,
            MojoShaderOpcode.Cnd => EmitCnd,
            MojoShaderOpcode.Def => EmitDef,
            MojoShaderOpcode.TexReg2Rgb => EmitTexReg2Rgb,
            MojoShaderOpcode.TexDp3Tex => EmitTexDp3Tex,
            MojoShaderOpcode.TexM3x2Depth => EmitTexM3X2Depth,
            MojoShaderOpcode.TexDp3 => EmitTexDp3,
            MojoShaderOpcode.TexM3x3 => EmitTexM3X3,
            MojoShaderOpcode.TexDepth => EmitTexDepth,
            MojoShaderOpcode.Cmp => EmitCmp,
            MojoShaderOpcode.Bem => EmitBem,
            MojoShaderOpcode.Dp2Add => EmitDp2Add,
            MojoShaderOpcode.Dsx => EmitDsX,
            MojoShaderOpcode.Dsy => EmitDsY,
            MojoShaderOpcode.TexLdd => EmitTexLdd,
            MojoShaderOpcode.SetP => EmitSetP,
            MojoShaderOpcode.TexLdl => EmitTexLdl,
            MojoShaderOpcode.BreakP => EmitBreakP,
            _ => (Action<IMojoShaderContext>)EmitReserved
        })(ctx);
}