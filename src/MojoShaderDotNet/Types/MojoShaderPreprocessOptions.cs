namespace MojoShaderDotNet.Types;

/// <summary>
/// Options for use with <see cref="MojoShaderParseOptions.Preprocess"/>.
/// </summary>
public class MojoShaderPreprocessOptions
{
    /// <summary>
    /// Points to (define_count) preprocessor definitions, and can be
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