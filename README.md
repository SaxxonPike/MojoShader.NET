# MojoShader.NET

[MojoShader](https://github.com/icculus/mojoshader) can convert
shaders from compiled Shader Model format shaders (Direct3D) to
other shader formats, such as GLSL.

This is a C# port of MojoShader. The license for this port is
identical to the original source. This is a work in progress;
not all profiles are ported. It is not yet intended for production
use, but some ported profiles may well be suitable; naturally, do
this at your own risk.

Many thanks to [Icculus](https://icculus.org/mojoshader/) for sharing
the original code with the world. Portability of shader code continues to
be an important part of game portability as a whole.

### Purpose

There are other packages out there that come with pre-built binaries
and wrappers that already work with .NET

### Requirements

- An up-to-date .NET Core SDK.
  - The MojoShaderDotNet project may target a specific version of
    the .NET Core SDK, but it may be possible to have a lower
    version targeted without issue.
  - There are no plans to target .NET Framework 4 (or lower.)

### Usage

- Import MojoShaderDotNet into your project. This can be done by
  downloading the source and adding it to your solution, or, in the
  future, via NuGet. (Packages are pending at this time.)
- Create a `MojoShader` instance and consume it:
  ```csharp
  using var stream = File.Open("MyCompiledShader.bin");
  var mojoShader = new MojoShader();
  var result = mojoShader.Parse(MojoShaderProfiles.Glsl, stream);
  ```
- There are many ways to interpret the output data. But if all you
  want is the converted shader, `.ToString()` is overloaded to
  return it as a UTF-8 formatted string:
  ```csharp
  var code = result.ToString();
  ```

### Implemented profiles

- `Bytecode`
- `Glsl`
- `Glsl120`
- `GlslEs`

For a list of all possible profiles, look at `MojoShader.Types.MojoShaderProfiles`.
String constants in that class can be passed in as the profile to `.Parse()`.

### Further information

Please check out the original MojoShader project linked above. However,
if you experience bugs or other issues with this shader, please do not
post the issue on that repo; use the 
[issue tracker](https://github.com/SaxxonPike/MojoShader.NET/issues) on
this repository instead. If there are improvements to the original
MojoShader that are not present or implemented here, that is also a good
candidate for opening a new issue.
