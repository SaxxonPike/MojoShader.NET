namespace MojoShaderDotNet.Types;

public class MojoShaderDestArgInfo
{
    /// <summary>
    /// this is the unmolested token in the stream.
    /// </summary>
    public int Token { get; set; }

    public int RegNum { get; set; }

    public bool Relative { get; set; }

    /// <summary>
    /// xyzw or rgba (all four, not split out).
    /// </summary>
    public MojoShaderWriteMaskValue WriteMask { get; set; }

    /// <summary>
    /// writemask before mojoshader tweaks it.
    /// </summary>
    public MojoShaderWriteMaskValue OrigWriteMask { get; set; }

    public MojoShaderMod ResultMod { get; set; }
    public int ResultShift { get; set; }
    public MojoShaderRegisterType RegType { get; set; }
}