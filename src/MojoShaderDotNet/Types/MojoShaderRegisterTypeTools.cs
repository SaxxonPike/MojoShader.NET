namespace MojoShaderDotNet.Types;

public static class MojoShaderRegisterTypeTools
{
    public static string Str(this MojoShaderRegisterType type) =>
        $"{type}".ToLower();
}