namespace MojoShaderDotNet.Types;

public class MojoShaderCompileOptions
{
    /// <summary>
    /// Contains preprocessor definitions. These are treated by the preprocessor as if the source code started
    ///  with one #define for each entry you pass in here.
    /// </summary>
    public List<MojoShaderPreprocessorDefine> Defs { get; set; } = new();

    /// <summary>
    /// Lets the app control the preprocessor's
    ///  behaviour for #include statements. Optional and can be NULL.
    /// </summary>
    public MojoShaderIncludeFunction? Include { get; set; }
}