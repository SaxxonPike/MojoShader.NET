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
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", fileName);
        var data = File.ReadAllBytes(path);
        using var stream = new MemoryStream(data);

        var ms = new MojoShader
        {
            Log = TestContext.Out
        };

        var output = ms.Parse(MojoShaderProfiles.Glsl, "", stream, null);

        output.Should().NotBeNull();
        output!.Errors.Should().BeEmpty();

        var code = output.ToString();

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

        var shader = 0;
        try
        {
            GL.
            GL.LoadBindings(new Mock<IBindingsContext>().Object);
            var maj = GL.GetInteger(GetPName.MajorVersion);
            var min = GL.GetInteger(GetPName.MinorVersion);
            shader = GL.CreateShader(shaderType);
            shader.Should().NotBe(0);
            GL.ShaderSource(shader, code);
            GL.GetError().Should().Be(ErrorCode.NoError);
            GL.CompileShader(shader);
            GL.GetError().Should().Be(ErrorCode.NoError);
        }
        finally
        {
            if (shader != 0)
                GL.DeleteShader(shader);
        }
    }
}