using System.Buffers.Binary;

namespace MojoShaderDotNet.Types;

public class MojoShaderSpirVPatchEntry
{
    public const int Size = 8;
    
    private readonly Memory<byte> _data;

    public MojoShaderSpirVPatchEntry() =>
        _data = new byte[Size];
    
    public MojoShaderSpirVPatchEntry(Memory<byte> data) => 
        _data = data;

    public int Offset
    {
        get => BinaryPrimitives.ReadInt32LittleEndian(_data.Span);
        set => BinaryPrimitives.WriteInt32LittleEndian(_data.Span, value);
    }

    public int Location
    {
        get => BinaryPrimitives.ReadInt32LittleEndian(_data.Span[4..]);
        set => BinaryPrimitives.WriteInt32LittleEndian(_data.Span[4..], value);
    }
}