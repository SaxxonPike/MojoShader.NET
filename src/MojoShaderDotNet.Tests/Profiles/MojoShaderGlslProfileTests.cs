using FluentAssertions;
using MojoShaderDotNet.Types;
using NUnit.Framework;

namespace MojoShaderDotNet.Profiles;

public class MojoShaderGlslProfileTests
{
    [Test]
    [TestCase("ps_1_1_simple.bin", MojoShaderShaderType.Pixel)]
    [TestCase("ps_1_2_simple.bin", MojoShaderShaderType.Pixel)]
    [TestCase("ps_1_3_simple.bin", MojoShaderShaderType.Pixel)]
    [TestCase("ps_1_4_simple.bin", MojoShaderShaderType.Pixel)]
    [TestCase("ps_2_0_simple.bin", MojoShaderShaderType.Pixel)]
    [TestCase("vs_1_1_simple.bin", MojoShaderShaderType.Vertex)]
    [TestCase("vs_2_0_simple.bin", MojoShaderShaderType.Vertex)]
    public void TestParse(string fileName, MojoShaderShaderType expectedType)
    {
        // Arrange.
        
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", fileName);
        var data = File.ReadAllBytes(path);
        using var stream = new MemoryStream(data);

        // Act.
        
        var subject = new MojoShader();
        var result = subject.Parse(MojoShaderProfiles.Glsl, stream)!;
        
        // Assert.
        
        result.Should().NotBeNull();
        
        var code = result.ToString();
        TestContext.Out.WriteLine(code);

        result.Errors.Should().BeEmpty();
        result.ShaderType.Should().Be(expectedType);
    }
}