namespace MojoShaderDotNet.Types;

public class MojoShaderDefine
{
    public string Identifier { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string Original { get; set; } = string.Empty;
    public List<string> Parameters { get; set; } = new();
    public int ParamCount { get; set; }
}