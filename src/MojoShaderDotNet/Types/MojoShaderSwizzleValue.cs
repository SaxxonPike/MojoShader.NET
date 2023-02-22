namespace MojoShaderDotNet.Types;

/// <summary>
/// [Saxxon] Made to simplify working with and testing swizzle values.
/// </summary>
public struct MojoShaderSwizzleValue
{
    public static implicit operator int(MojoShaderSwizzleValue swiz) =>
        swiz.RawValue;

    public static implicit operator MojoShaderSwizzleValue(int swiz) =>
        new(swiz);

    private const int NoSwizzle = 0b_11_10_01_00; // .xyzw

    public MojoShaderSwizzleValue()
        : this(NoSwizzle)
    {
    }

    public MojoShaderSwizzleValue(int value)
    {
        RawValue = value & 0b11111111;
    }

    public byte this[int index]
    {
        get => unchecked((byte)((RawValue >> (index << 1)) & 0b11));
        set
        {
            var mask = 0b11 << (index << 1);
            var val = (value & 0b11) << (index << 1);
            RawValue = (RawValue & ~mask) | val;
        }
    }

    /// <summary>
    /// 0xE4 == 11100100 ... 0 1 2 3. No swizzle.
    /// [no_swizzle; mojoshader_profile_common.c]
    /// </summary>
    public bool IsNone =>
        RawValue == NoSwizzle;

    /// <summary>
    /// elements 1|2 match 3|4 and element 1 matches element 2.
    /// [replicate_swizzle; mojoshader_profile_common.c]
    /// </summary>
    public bool IsReplicate =>
        RawValue is 0b00_00_00_00 
            or 0b01_01_01_01 
            or 0b10_10_10_10 
            or 0b11_11_11_11;

    public int RawValue { get; set; }

    public override string ToString()
    {
        const string letters = "xyzw";
        return new string(new[]
        {
            letters[RawValue & 0b11],
            letters[(RawValue >> 2) & 0b11],
            letters[(RawValue >> 4) & 0b11],
            letters[(RawValue >> 6) & 0b11]
        });
    }
}