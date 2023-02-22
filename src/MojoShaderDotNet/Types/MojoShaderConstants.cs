namespace MojoShaderDotNet.Types;

public static class MojoShaderConstants
{
    /// <summary>
    /// MAX_SHADER_MAJOR.
    /// </summary>
    public const int MaxShaderMajor = 3;

    /// <summary>
    /// MAX_SHADER_MINOR.
    /// </summary>
    public const int MaxShaderMinor = 255;

    /// <summary>
    /// This is the ID for a D3DXSHADER_CONSTANTTABLE in the bytecode comments.
    /// </summary>
    public const int CTabId = 0x42415443;

    /// <summary>
    /// sizeof (D3DXSHADER_CONSTANTTABLE).
    /// </summary>
    public const int CTabSize = 28;

    /// <summary>
    /// sizeof (D3DXSHADER_CONSTANTINFO).
    /// </summary>
    public const int CInfoSize = 20;

    /// <summary>
    /// sizeof (D3DXSHADER_TYPEINFO).
    /// </summary>
    public const int CTypeInfoSize = 16;

    /// <summary>
    /// sizeof (D3DXSHADER_STRUCTMEMBERINFO)
    /// </summary>
    public const int CMemberInfoSize = 8;

    /// <summary>
    /// Preshader magic value PRES_ID.
    /// </summary>
    public const int PresId = 0x53455250;

    /// <summary>
    /// Preshader magic value PRSI_ID.
    /// </summary>
    public const int PrsiId = 0x49535250;

    /// <summary>
    /// Preshader magic value CLID_ID.
    /// </summary>
    public const int ClitId = 0x54494C43;

    /// <summary>
    /// Preshader magic value FXLC_ID.
    /// </summary>
    public const int FxlcId = 0x434C5846;
}