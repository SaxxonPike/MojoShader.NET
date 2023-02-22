namespace MojoShaderDotNet.Types;

public class MojoShaderError
{
    /// <summary>
    /// Human-readable error, if there is one. Will be NULL if there was no
    ///  error. The string will be UTF-8 encoded, and English only. Most of
    ///  these shouldn't be shown to the end-user anyhow.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Filename where error happened. This can be NULL if the information
    ///  isn't available.
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    /// Position of error, if there is one. Will be <see cref="MojoShaderPosition.None"/> if
    ///  there was no error, <see cref="MojoShaderPosition.Before"/> if there was an error
    ///  before processing started, and <see cref="MojoShaderPosition.After"/> if there was
    ///  an error during final processing. If >= 0, MOJOSHADER_parse() sets
    ///  this to the byte offset (starting at zero) into the bytecode you
    ///  supplied, and MOJOSHADER_assemble(), MOJOSHADER_parseAst(), and
    ///  MOJOSHADER_compile() sets this to a a line number in the source code
    ///  you supplied (starting at one).
    /// </summary>
    public int ErrorPosition { get; set; } = (int)MojoShaderPosition.None;

    public MojoShaderError Clone() => (MojoShaderError)MemberwiseClone();

    public override string ToString() => 
        $"{Error ?? "<unknown>"} in {Filename ?? "<unknown>"} at {ErrorPosition:D} (0x{ErrorPosition:X8})";
}