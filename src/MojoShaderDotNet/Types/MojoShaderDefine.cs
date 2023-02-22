namespace MojoShaderDotNet.Types;

public class MojoShaderDefine
{
    public string Identifier { get; set; }
    public string Definition { get; set; }
    public string Original { get; set; }
    public List<string> Parameters { get; set; } = new();
    public int ParamCount { get; set; }
    // public MojoShaderDefine? Next { get; set; }
}