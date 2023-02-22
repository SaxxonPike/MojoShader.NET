using System.Text.Json.Serialization;

namespace MojoShaderDotNet.Types;

/// <summary>
/// [PreshaderBlockInfo; mojoshader.c]
/// </summary>
public class MojoShaderPreshaderBlockInfo
{
    public int TokenOffset { get; set; }
    public int[] RawTokens { get; set; }
    [JsonIgnore]
    public Span<int> Tokens => RawTokens.AsSpan(TokenOffset..);
    public bool Seen { get; set; }
    public int TokenCount => Tokens.Length;
}