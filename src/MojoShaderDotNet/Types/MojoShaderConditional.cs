namespace MojoShaderDotNet.Types;

public class MojoShaderConditional
{
    public MojoShaderToken Type { get; set; }
    public int LineNum { get; set; }
    public int Skipping { get; set; }
    public int Chosen { get; set; }
    // public MojoShaderConditional? Next { get; set; }
}