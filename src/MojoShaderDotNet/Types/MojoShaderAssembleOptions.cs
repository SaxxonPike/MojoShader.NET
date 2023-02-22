namespace MojoShaderDotNet.Types;

/// <summary>
/// Options for use with <see cref="IMojoShader.Assemble"/>
/// </summary>
public class MojoShaderAssembleOptions
{
    /// <summary>
    /// Contains UTF-8 strings, and
    ///  can be NULL. These strings are inserted as comments in the bytecode.
    /// </summary>
    public List<string> Comments { get; set; } =
        new();

    /// <summary>
    /// Contains symbol structs, and can be NULL. These
    ///  become a CTAB field in the bytecode. This is optional, but
    ///  MOJOSHADER_parse() needs CTAB data for all arrays used in a program, or
    ///  relative addressing will not be permitted, so you'll want to at least
    ///  provide symbol information for those. The symbol data is 100% trusted
    ///  at this time; it will not be checked to see if it matches what was
    ///  assembled in any way whatsoever.
    /// </summary>
    public List<MojoShaderSymbol> Symbols { get; set; } =
        new();

    /// <summary>
    /// Contains preprocessor definitions, and can be
    ///  NULL. These are treated by the preprocessor as if the source code started
    ///  with one #define for each entry you pass in here.
    /// </summary>
    public List<MojoShaderPreprocessorDefine> Defines { get; set; } =
        new();

    /// <summary>
    /// Lets the app control the preprocessor's
    ///  behaviour for #include statements. Optional and can be NULL.
    /// </summary>
    public MojoShaderIncludeFunction? Include { get; set; }
}