using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MojoShaderDotNet.Types;

namespace MojoShaderDotNet.Profiles;

public interface IMojoShaderContext
{
    bool IsFail { get; set; }
    MojoShaderPosition ErrorPosition { get; set; }
    int CurrentPosition { get; set; }

    [JsonIgnore] Span<int> Tokens { get; }
    int[] OrigTokens { get; set; }

    /// <summary>
    /// Tokens remaining; represents "tokencount" field.
    /// </summary>
    int TokensRemaining { get; }

    bool KnowShaderSize { get; set; }
    List<MojoShaderSwizzle> Swizzles { get; set; }
    List<MojoShaderSamplerMap> SamplerMap { get; set; }
    StreamWriter Output { get; set; }
    StreamWriter Preflight { get; set; }
    StreamWriter Globals { get; set; }
    StreamWriter Inputs { get; set; }
    StreamWriter Outputs { get; set; }
    StreamWriter Helpers { get; set; }
    StreamWriter Subroutines { get; set; }
    StreamWriter MainLineIntro { get; set; }
    StreamWriter MainLineArguments { get; set; }
    StreamWriter MainLineTop { get; set; }
    StreamWriter MainLine { get; set; }
    StreamWriter Postflight { get; set; }
    StreamWriter Ignore { get; set; }
    Stack<StreamWriter> OutputStack { get; set; }
    Stack<int> IndentStack { get; set; }
    int Indent { get; set; }
    string ShaderTypeStr { get; set; }
    string EndLine { get; set; }
    string MainFn { get; set; }
    string ProfileId { get; set; }
    IMojoShaderProfile? Profile { get; set; }
    MojoShaderShaderType ShaderType { get; set; }
    int MajorVer { get; set; }
    int MinorVer { get; set; }
    MojoShaderDestArgInfo? DestArg { get; set; }
    MojoShaderSourceArgInfo?[] SourceArgs { get; set; }
    MojoShaderSourceArgInfo? PredicateArg { get; set; }
    int[] Dwords { get; set; }
    int VersionToken { get; set; }
    int InstructionCount { get; set; }
    int InstructionControls { get; set; }
    MojoShaderOpcode CurrentOpcode { get; set; }
    MojoShaderOpcode PreviousOpcode { get; set; }
    bool CoIssue { get; set; }
    int Loops { get; set; }
    int Reps { get; set; }
    int MaxReps { get; set; }
    int Cmps { get; set; }
    int ScratchRegisters { get; set; }
    int MaxScratchRegisters { get; set; }
    Stack<int> BranchLabels { get; set; }
    int AssignedBranchLabels { get; set; }
    int AssignedVertexAttributes { get; set; }
    int LastAddressRegComponent { get; set; }
    List<MojoShaderRegister> UsedRegisters { get; set; }
    List<MojoShaderRegister> DefinedRegisters { get; set; }
    List<MojoShaderError> Errors { get; set; }
    List<MojoShaderConstant> Constants { get; set; }
    int UniformFloat4Count { get; set; }
    int UniformInt4Count { get; set; }
    int UniformBoolCount { get; set; }
    List<MojoShaderRegister> Uniforms { get; set; }
    List<MojoShaderRegister> Attributes { get; set; }
    List<MojoShaderRegister> Samplers { get; set; }
    List<MojoShaderVariable> Variables { get; set; }
    bool CentroidAllowed { get; set; }
    MojoShaderCtabData? Ctab { get; set; }
    bool HaveRelativeInputRegisters { get; set; }
    bool HaveMultiColorOutputs { get; set; }
    bool DeterminedConstantsArrays { get; set; }
    bool Predicated { get; set; }
    bool UsesPointSize { get; set; }
    bool UsesFog { get; set; }
    bool NeedsMaxFloat { get; set; }
    bool HavePreshader { get; set; }
    bool IgnoresCtab { get; set; }
    bool ResetTexMpad { get; set; }
    int TexM3X2PadDst0 { get; set; }
    int TexM3X2PadSrc0 { get; set; }
    int TexM3X3PadDst0 { get; set; }
    int TexM3X3PadSrc0 { get; set; }
    int TexM3X3PadDst1 { get; set; }
    int TexM3X3PadSrc1 { get; set; }
    MojoShaderPreshader? Preshader { get; set; }
    List<MojoShaderInstruction> Instructions { get; set; }
    int ShaderSize { get; set; }
    TextWriter? Log { get; set; }

    /// <summary>
    /// [MOJOSHADER_FLIP_RENDERTARGET]
    /// </summary>
    bool FlipRenderTargetOption { get; set; }

    /// <summary>
    /// [MOJOSHADER_DEPTH_CLIPPING]
    /// </summary>
    bool DepthClippingOption { get; set; }

    List<MojoShaderUniform> BuildUniforms();
    List<MojoShaderConstant> BuildConstants();
    List<MojoShaderSampler> BuildSamplers();
    List<MojoShaderAttribute> BuildAttributes();
    List<MojoShaderAttribute> BuildOutputs();
    MojoShaderParseData BuildParseData();
    string BuildOutput();

    int GetVerUi32(int major, int minor);
    int RegToUi32(MojoShaderRegisterType regType, int regNum);

    bool ShaderVersionSupported(int maj, int min);
    bool ShaderVersionAtLeast(int maj, int min);
    bool ShaderVersionExactly(int maj, int min);
    bool ShaderIsPixel();
    bool ShaderIsVertex();
    void Fail([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] items);
    void Fail(string reason);

    void AddSampler(int regNum, MojoShaderTextureType tType, int texBem);
    (MojoShaderDestArgInfo? Info, int Size) ParseDestinationToken();
    void SetDstArgWriteMask(MojoShaderDestArgInfo dst, int mask);
    MojoShaderRegister SetUsedRegister(MojoShaderRegisterType regType, int regNum, bool written);
    MojoShaderRegister RegListInsert(List<MojoShaderRegister> registers, MojoShaderRegisterType regType, int regNum);
    void SetDefinedRegister(MojoShaderRegisterType regType, int regNum);
    MojoShaderRegister? RegListFind(List<MojoShaderRegister> registers, MojoShaderRegisterType regType, int regNum);
    MojoShaderRegister CreateRegister(MojoShaderRegisterType regType, int regNum);

    bool IsScalar(MojoShaderShaderType shaderType, MojoShaderRegisterType rType, int rNum);

    void SetOutput(MojoShaderProfileOutput output);
    void PushOutput(MojoShaderProfileOutput output);
    void PopOutput();
    void OutputLine(string? line);
    void OutputLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? format, params object?[] items);
    void OutputBlankLine();
    void AdjustTokenPosition(int incr);

    void SrcArgMatrixReplicate(int idx, int rows);
    void CheckLabelRegister(int arg, string opcode);
    void CheckCallLoopWrappage(int regNum);

    bool RegisterWasWritten(MojoShaderRegisterType rType, int regNum);
    bool GetDefinedRegister(MojoShaderRegisterType rType, int regNum);

    void AddAttributeRegister(MojoShaderRegisterType rType, int regNum, MojoShaderUsage usage,
        int index, int writeMask, int flags);

    int ParseToken();
    int ParseVersionToken(string profileStr);

    void ProcessDefinitions();
    int ParseArgs(MojoShaderInstructionArgs args);
    void State(MojoShaderOpcode op);
}