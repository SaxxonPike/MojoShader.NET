namespace MojoShaderDotNet.Types;

public class MojoShaderIncludeState
{
    public string FileName { get; set; } = string.Empty;
    public string SourceBase { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public MojoShaderToken TokenVal { get; set; } = MojoShaderToken.Unknown;
    public bool PushedBack { get; set; }
    public string LexerMarker { get; set; } = string.Empty;
    public bool ReportWhitespace { get; set; }
    public bool ReportComments { get; set; }
    public bool AsmComments { get; set; }
    public int OrigLength { get; set; }
    public int BytesLeft { get; set; }
    public int Line { get; set; }
    public List<MojoShaderConditional> ConditionalStack { get; set; } = new();
}