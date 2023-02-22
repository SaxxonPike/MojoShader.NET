namespace MojoShaderDotNet.Types;

public class MojoShaderSpirVPatchTable
{
    private readonly Memory<byte> _data;

    public MojoShaderSpirVPatchTable()
    {
    }
    
    public MojoShaderSpirVPatchTable(Memory<byte> data)
    {
        _data = data;
    }
    
    // Patches for uniforms
    public MojoShaderSpirVPatchEntry? VpFlip { get; set; }
    public MojoShaderSpirVPatchEntry? ArrayVec4 { get; set; }
    public MojoShaderSpirVPatchEntry? ArrayIvec4 { get; set; }
    public MojoShaderSpirVPatchEntry? ArrayBool { get; set; }
    public IReadOnlyList<MojoShaderSpirVPatchEntry> Samplers { get; set; } = new MojoShaderSpirVPatchEntry[16];
    public int LocationCount { get; set; }

    /// <summary>
    /// TEXCOORD0 is patched to PointCoord if VS outputs PointSize.
    /// In `helpers`: [OpDecorate|id|Location|0xDEADBEEF] -> [OpDecorate|id|BuiltIn|PointCoord]
    /// Offset derived from attrib_offsets[TEXCOORD][0].
    /// in `mainline_intro`, [OpVariable|tid|id|StorageClass], patch tid to pvec2i
    /// </summary>
    public int PointCoordVarOffset { get; set; }

    /// <summary>
    /// in `mainline_top`, [OpLoad|tid|id|src_id], patch tid to vec2
    /// </summary>
    public int PointCoordLoadOffset { get; set; }

    public int TidPVec2I { get; set; }
    public int TidVec2 { get; set; }
    public int TidPVec4I { get; set; }
    public int TidVec4 { get; set; }

    // Patches for linking vertex output/pixel input
    public int[,] AttribOffsets { get; set; } = new int[(int)MojoShaderUsage.Total, 16];
    public int[] OutputOffsets { get; set; } = new int[16];
}