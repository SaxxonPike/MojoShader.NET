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
    /// x or red
    /// </summary>
    public bool WriteMask0 => WriteMask[0];

    /// <summary>
    /// y or green
    /// </summary>
    public bool WriteMask1 => WriteMask[1];

    /// <summary>
    /// z or blue
    /// </summary>
    public bool WriteMask2 => WriteMask[2];

    /// <summary>
    /// w or alpha
    /// </summary>
    public bool WriteMask3 => WriteMask[3];

    /// <summary>
    /// writemask before mojoshader tweaks it.
    /// </summary>
    public MojoShaderWriteMaskValue OrigWriteMask { get; set; }

    public MojoShaderMod ResultMod { get; set; }
    public int ResultShift { get; set; }
    public MojoShaderRegisterType RegType { get; set; }
}