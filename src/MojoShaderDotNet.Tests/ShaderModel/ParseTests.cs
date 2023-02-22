using FluentAssertions;
using MojoShaderDotNet.Types;
using Moq;
using NUnit.Framework;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

namespace MojoShaderDotNet.Tests.ShaderModel;

[SingleThreaded]
public class ParseTests
{
    [Test]
    [TestCase("ps_1_1_simple.bin")]
    [TestCase("ps_1_2_simple.bin")]
    [TestCase("ps_1_3_simple.bin")]
    [TestCase("ps_1_4_simple.bin")]
    [TestCase("ps_2_0_simple.bin")]
    [TestCase("vs_1_1_simple.bin")]
    [TestCase("vs_2_0_simple.bin")]
    public void TestParse(string fileName)
    {
        // Arrange.
        
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", fileName);
        var data = File.ReadAllBytes(path);
        using var stream = new MemoryStream(data);

        // Act.
        
        var subject = new MojoShader();
        var output = subject.Parse(MojoShaderProfiles.Glsl, "", stream, null);
        
        // Assert.
        
        output.Should().NotBeNull();
        output!.Errors.Should().BeEmpty();

        var code = output.ToString();
        TestContext.Out.WriteLine(code);

        var shaderType = output.ShaderType switch
        {
            MojoShaderShaderType.Pixel => ShaderType.FragmentShader,
            MojoShaderShaderType.Vertex => ShaderType.VertexShader,
            MojoShaderShaderType.Geometry => ShaderType.GeometryShader,
            _ => default
        };

        if (shaderType == default)
        {
            Assert.Fail("Invalid shader type");
            return;
        }
    }
}