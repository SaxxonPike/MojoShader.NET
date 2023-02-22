using System.Text;

namespace MojoShaderDotNet.Types;

/// <summary>
/// [Saxxon] Made to simplify working with and testing write mask values.
/// </summary>
public struct MojoShaderWriteMaskValue
{
    public static implicit operator int(MojoShaderWriteMaskValue wm) =>
        wm.RawValue;

    public static implicit operator MojoShaderWriteMaskValue(int wm) =>
        new(wm);

    public static MojoShaderWriteMaskValue X => 
        new(0x1);

    private const int FullWriteMask = 0b1111;

    public MojoShaderWriteMaskValue()
        : this(FullWriteMask)
    {
    }

    public MojoShaderWriteMaskValue(int value) =>
        RawValue = value;

    public bool this[int index]
    {
        get => ((RawValue >> index) & 1) != 0;
        set => RawValue = value
            ? RawValue | (1 << index)
            : RawValue & ~(1 << index);
    }

    /// <summary>
    /// Returns true if the write mask is at implied defaults (.xyzw)
    /// [writemask_xyzw; mojoshader_profile_common.c]
    /// </summary>
    public bool IsXyzw =>
        RawValue == 0b1111;

    /// <summary>
    /// Returns true if the write mask is .xyz
    /// [writemask_xyz; mojoshader_profile_common.c]
    /// </summary>
    public bool IsXyz =>
        RawValue == 0b0111;

    /// <summary>
    /// Returns true if the write mask is .xy
    /// [writemask_xy; mojoshader_profile_common.c]
    /// </summary>
    public bool IsXy =>
        RawValue == 0b0011;

    /// <summary>
    /// Returns true if the write mask is .x
    /// [writemask_x; mojoshader_profile_common.c]
    /// </summary>
    public bool IsX =>
        RawValue == 0b0001;

    /// <summary>
    /// Returns true if the write mask is .y
    /// [writemask_y; mojoshader_profile_common.c]
    /// </summary>
    public bool IsY =>
        RawValue == 0b0010;

    /// <summary>
    /// Returns true if the write mask is all disabled.
    /// </summary>
    public bool IsNone =>
        RawValue == 0b0000;

    public int RawValue { get; set; }

    public int VecSize =>
        (RawValue & 1) +
        ((RawValue >> 1) & 1) +
        ((RawValue >> 2) & 1) +
        ((RawValue >> 3) & 1);

    public override string ToString()
    {
        const string letters = "xyzw";

        var output = new StringBuilder();
        for (var i = 0; i < 4; i++)
            if (((RawValue >> i) & 1) != 0)
                output.Append(letters[i]);

        return output.ToString();
    }
}