namespace MojoShaderDotNet.Types;

public enum MojoShaderRegisterType
{
    Temp = 0,
    Input = 1,
    Const = 2,
    Address = 3,
    Texture = 3, // ALSO 3!
    RastOut = 4,
    AttrOut = 5,
    TexCrdOut = 6,
    Output = 6, // ALSO 6!
    ConstInt = 7,
    ColorOut = 8,
    DepthOut = 9,
    Sampler = 10,
    Const2 = 11,
    Const3 = 12,
    Const4 = 13,
    ConstBool = 14,
    Loop = 15,
    TempFloat16 = 16,
    MiscType = 17,
    Label = 18,
    Predicate = 19,
    Max = Predicate,
    Invalid = Max + 1
}