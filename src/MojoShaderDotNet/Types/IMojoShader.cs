namespace MojoShaderDotNet.Types;

/// <summary>
/// [mojoshader.h]
/// </summary>
public interface IMojoShader
{
    /// <summary>
    /// Parse a compiled Direct3D shader's bytecode.
    /// 
    /// This is your primary entry point into MojoShader. You need to pass it
    ///  a compiled D3D shader and tell it which "profile" you want to use to
    ///  convert it into useful data.
    /// 
    /// This function will never return NULL.
    /// 
    /// [MOJOSHADER_parse; mojoshader.h]
    /// </summary>
    /// <param name="profile">
    /// The available profiles are the set of MOJOSHADER_PROFILE_* defines.
    /// </param>
    /// <param name="mainFn">
    /// You should pass a name for your shader's main function in here, via the
    ///  (mainfn) param. Some profiles need this name to be unique. Passing a NULL
    ///  here will pick a reasonable default, and most profiles will ignore it
    ///  anyhow. As the name of the shader's main function, etc, so make it a
    ///  simple name that would match C's identifier rules. Keep it simple!
    /// </param>
    /// <param name="stream"></param>
    /// <param name="options">
    /// Options to customize the parse operation.
    /// </param>
    /// <returns></returns>
    MojoShaderParseData? Parse(
        string profile,
        string mainFn,
        Stream stream,
        MojoShaderParseOptions? options);

    /// <summary>
    /// This function is optional. Use this to convert Direct3D shader assembly
    ///  language into bytecode, which can be handled by MOJOSHADER_parse().
    ///
    /// This will return a MOJOSHADER_parseData, like MOJOSHADER_parse() would,
    ///  except the profile will be MOJOSHADER_PROFILE_BYTECODE and the output
    ///  will be the assembled bytecode instead of some other language. This output
    ///  can be pushed back through MOJOSHADER_parseData() with a different profile.
    /// </summary>
    /// <param name="fileName">
    /// A UTF-8 filename. It can be NULL. We do not
    ///  actually access this file, as we obtain our data from (source). This
    ///  string is copied when we need to report errors while processing (source),
    ///  as opposed to errors in a file referenced via the #include directive in
    ///  (source). If this is NULL, then errors will report the filename as NULL,
    ///  too.
    /// </param>
    /// <param name="source">
    /// A UTF-8 string of valid Direct3D shader assembly source code.
    /// </param>
    /// <param name="options">
    /// </param>
    /// <returns></returns>
    MojoShaderParseData Assemble(
        string fileName,
        string source,
        MojoShaderAssembleOptions options);

    /// <summary>
    /// This function is optional. Use this to compile high-level shader programs.
    /// 
    /// This is intended to turn HLSL source code into D3D assembly code, which
    ///  can then be passed to MOJOSHADER_assemble() to convert it to D3D bytecode
    ///  (which can then be used with MOJOSHADER_parseData() to support other
    ///  shading targets).
    /// </summary>
    /// <param name="sourceProfile">
    /// (srcprofile) specifies the source language of the shader. You can specify
    ///  a shader model with this, too. See MOJOSHADER_SRC_PROFILE_* constants.
    /// </param>
    /// <param name="fileName">
    /// (filename) is a NULL-terminated UTF-8 filename. It can be NULL. We do not
    ///  actually access this file, as we obtain our data from (source). This
    ///  string is copied when we need to report errors while processing (source),
    ///  as opposed to errors in a file referenced via the #include directive in
    ///  (source). If this is NULL, then errors will report the filename as NULL,
    ///  too.
    /// </param>
    /// <param name="source">
    /// (source) is an UTF-8 string of valid high-level shader source code.
    ///  It does not need to be NULL-terminated.
    /// </param>
    /// <param name="options">
    /// Options to further customize the compile operation.
    /// </param>
    public MojoShaderCompileData Compile(
        string sourceProfile,
        string fileName,
        string source,
        MojoShaderCompileOptions options);

    /// <summary>
    /// Patching SPIR-V binaries before linking is needed to ensure locations do not
    /// overlap between shader stages. Unfortunately, OpDecorate takes Literal, so we
    /// can't use Result &lt;id&gt; from OpSpecConstant and leave this up to specialization
    /// mechanism.
    /// Patch table must be propagated from parsing to program linking, but since
    /// MOJOSHADER_parseData is public and I'd like to avoid changing ABI and exposing
    /// this, it is appended to MOJOSHADER_parseData::output using postflight buffer.
    /// </summary>
    public void SpirvLinkAttributes(MojoShaderParseData vertex, MojoShaderParseData pixel);
    
    public TextWriter? Log { get; set; }
}