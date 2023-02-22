namespace MojoShaderDotNet.Profiles.Metal;

public class MojoShaderMetalContext : MojoShaderContext
{
    public int MetalNeedHeaderCommon { get; set; }
    public int MetalNeedHeaderMath { get; set; }
    public int MetalNeedHeaderRelational { get; set; }
    public int MetalNeedHeaderGeometric { get; set; }
    public int MetalNeedHeaderGraphics { get; set; }
    public int MetalNeedHeaderTexture { get; set; }
}