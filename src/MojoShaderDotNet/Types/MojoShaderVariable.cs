namespace MojoShaderDotNet.Types;

/// <summary>
/// [VariableList; mojoshader_profile.h]
/// </summary>
public class MojoShaderVariable
{
    public MojoShaderUniformType Type { get; set; }
    public int Index { get; set; }
    public int Count => Constants.Count;
    public List<MojoShaderConstant> Constants { get; set; } = new();
    public bool Used { get; set; }
    public int EmitPosition { get; set; }

    public MojoShaderVariable Clone() =>
        new()
        {
            Type = Type,
            Index = Index,
            Constants = Constants.Select(x => x.Clone()).ToList(),
            Used = Used,
            EmitPosition = EmitPosition
        };
}