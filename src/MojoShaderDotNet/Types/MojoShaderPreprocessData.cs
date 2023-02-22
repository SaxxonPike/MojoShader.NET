namespace MojoShaderDotNet.Types;

/// <summary>
/// Structure used to return data from preprocessing of a shader...
/// [MOJOSHADER_preprocessData; mojoshader.h]
/// </summary>
public class MojoShaderPreprocessData
{
    /// <summary>
    /// Elements of data that specify errors that were generated
    ///  by parsing this shader.
    /// This can be NULL if there were no errors or if (error_count) is zero.
    /// </summary>
    public List<MojoShaderError> Errors { get; set; } =
        new();

    /// <summary>
    /// Bytes of output from preprocessing. This is a UTF-8 string.
    /// Will be NULL on error.
    /// </summary>
    public string? Output { get; set; }
}