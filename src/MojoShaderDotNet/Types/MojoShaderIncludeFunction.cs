namespace MojoShaderDotNet.Types;

/// <summary>
/// This callback allows an app to handle #include statements for the
///  preprocessor. When the preprocessor sees an #include, it will call this
///  function to obtain the contents of the requested file. This is optional;
///  the preprocessor will open files directly if no callback is supplied, but
///  this allows an app to retrieve data from something other than the
///  traditional filesystem (for example, headers packed in a .zip file or
///  headers generated on-the-fly).
///
/// This function maps to ID3DXInclude::Open()
///
/// The callback returns zero on error, non-zero on success.
/// </summary>
/// <param name="incType">
/// Specifies the type of header we wish to include.
/// </param>
/// <param name="fName">
/// Specifies the name of the file specified on the #include line.
/// </param>
/// <param name="parent">
/// A string of the entire source file containing the include, in
///  its original, not-yet-preprocessed state. Note that this is just the
///  contents of the specific file, not all source code that the preprocessor
///  has seen through other includes, etc.
/// </param>
public delegate string MojoShaderIncludeFunction(
    MojoShaderIncludeType incType,
    string fName,
    string parent);