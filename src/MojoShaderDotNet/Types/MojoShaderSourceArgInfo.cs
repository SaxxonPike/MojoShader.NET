namespace MojoShaderDotNet.Types;

/// <summary>
/// [SourceArgInfo; mojoshader_profile.h]
/// </summary>
public class MojoShaderSourceArgInfo
{
    public int Token { get; set; }
    public int RegNum { get; set; }
    public MojoShaderSwizzleValue Swizzle { get; set; }
    public MojoShaderSourceMod SrcMod { get; set; }
    public MojoShaderRegisterType RegType { get; set; }
    public bool Relative { get; set; }
    public MojoShaderRegisterType RelativeRegType { get; set; }
    public int RelativeRegNum { get; set; }
    public int RelativeComponent { get; set; }
    public List<MojoShaderVariable> RelativeArray { get; set; } = new();

    public MojoShaderSourceArgInfo Clone() =>
        new()
        {
            Token = Token,
            RegNum = RegNum,
            Swizzle = Swizzle,
            SrcMod = SrcMod,
            RegType = RegType,
            Relative = Relative,
            RelativeRegType = RelativeRegType,
            RelativeRegNum = RelativeRegNum,
            RelativeComponent = RelativeComponent,
            RelativeArray = RelativeArray.Select(x => x.Clone()).ToList()
        };
}