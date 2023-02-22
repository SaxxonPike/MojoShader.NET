namespace MojoShaderDotNet.Types;

/// <summary>
/// This function is optional. Even if you are dealing with shader source
///  code, you don't need to explicitly use the preprocessor, as the compiler
///  and assembler will use it behind the scenes. In fact, you probably never
///  need this function unless you are debugging a custom tool (or debugging
///  MojoShader itself).
///
/// Preprocessing roughly follows the syntax of an ANSI C preprocessor, as
///  Microsoft's Direct3D assembler and HLSL compiler use this syntax. Please
///  note that we try to match the output you'd get from Direct3D's
///  preprocessor, which has some quirks if you're expecting output that matches
///  a generic C preprocessor.
///
/// This function maps to D3DXPreprocessShader().
/// </summary>
/// <param name="fileName">
/// A UTF-8 filename. It can be NULL. We do not
///  actually access this file, as we obtain our data from (source). This
///  string is copied when we need to report errors while processing (source),
///  as opposed to errors in a file referenced via the #include directive in
///  (source). If this is NULL, then errors will report the filename as NULL,
///  too.
/// </param>
/// <param name="source">
/// A string of UTF-8 text to preprocess.
/// </param>
public delegate MojoShaderPreprocessData MojoShaderPreprocessFunction(
    string fileName,
    string source,
    MojoShaderPreprocessOptions options);