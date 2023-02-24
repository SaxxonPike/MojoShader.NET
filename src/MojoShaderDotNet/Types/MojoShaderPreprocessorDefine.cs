namespace MojoShaderDotNet.Types;

/// <summary>
/// Structure used to pass predefined macros. Maps to D3DXMACRO.
///  You can have macro arguments: set identifier to "a(b, c)" or whatever.
/// [MOJOSHADER_preprocessorDefine; mojoshader.h]
/// </summary>
public class MojoShaderPreprocessorDefine
{
    public string Identifier { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
}