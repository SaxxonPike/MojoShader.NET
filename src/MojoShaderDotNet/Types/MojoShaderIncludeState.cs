namespace MojoShaderDotNet.Types;

public class MojoShaderIncludeState
{
    public string FileName { get; set; }
    public string SourceBase { get; set; }
    public string Source { get; set; }
    public string Token { get; set; }
    public MojoShaderToken TokenVal { get; set; }
    public int PushedBack { get; set; }
    public string LexerMarker { get; set; }
    public int ReportWhitespace { get; set; }
    public int ReportComments { get; set; }
    public int AsmComments { get; set; }
    public int OrigLength { get; set; }
    public int BytesLeft { get; set; }
    public int Line { get; set; }
    public List<MojoShaderConditional> ConditionalStack { get; set; } = new();
    // public MojoShaderIncludeState? Next { get; set; }
}