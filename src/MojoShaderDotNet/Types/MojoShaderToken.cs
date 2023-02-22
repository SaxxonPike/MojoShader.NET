namespace MojoShaderDotNet.Types;

public enum MojoShaderToken
{
    /// <summary>
    /// start past ASCII character values.
    /// </summary>
    Unknown = 256,

    /// <summary>
    /// These are all C-like constructs. Tokens &lt; 256 may be single
    ///  chars (like '+' or whatever). These are just multi-char sequences
    ///  (like "+=" or whatever).
    /// </summary>
    Identifier,
    IntLiteral,
    FloatLiteral,
    StringLiteral,
    RShiftAssign,
    LShiftAssign,
    AddAssign,
    SubAssign,
    MultAssign,
    DivAssign,
    ModAssign,
    XorAssign,
    AndAssign,
    OrAssign,
    Increment,
    Decrement,
    RShift,
    LShift,
    AndAnd,
    OrOr,
    Leq,
    Geq,
    Eql,
    Neq,
    Hash,
    HashHash,

    /// <summary>
    /// This is returned if the preprocessor isn't stripping comments. Note
    ///  that in asm files, the ';' counts as a single-line comment, same as
    ///  "//". Note that both eat newline tokens: all of the ones inside a
    ///  multiline comment, and the ending newline on a single-line comment.
    /// </summary>
    MultiComment,

    SingleComment,

    /// <summary>
    /// This is returned at the end of input...no more to process.
    /// </summary>
    Eoi,

    /// <summary>
    /// This is returned for char sequences we think are bogus. You'll have
    ///  to judge for yourself. In most cases, you'll probably just fail with
    ///  bogus syntax without explicitly checking for this token.
    /// </summary>
    BadChars,

    /// <summary>
    /// This is returned if there's an error condition (the error is returned
    ///  as a NULL-terminated string from preprocessor_nexttoken(), instead
    ///  of actual token data). You can continue getting tokens after this
    ///  is reported. It happens for things like missing #includes, etc.
    /// </summary>
    PreprocessingError,

    /// <summary>
    /// These are all caught by the preprocessor. Caller won't ever see them,
    ///  except TOKEN_PP_PRAGMA.
    ///  They control the preprocessor (#includes new files, etc).
    /// </summary>
    PpInclude,
    PpLine,
    PpDefine,
    PpUndef,
    PpIf,
    PpIfDef,
    PpIfNdef,
    PpElse,
    PpElIf,
    PpEndIf,

    /// <summary>
    /// caught, becomes TOKEN_PREPROCESSING_ERROR
    /// </summary>
    PpError,
    PpPragma,

    /// <summary>
    /// caught, becomes TOKEN_PREPROCESSING_ERROR
    /// </summary>
    IncompleteComment,

    /// <summary>
    /// used internally, never returned.
    /// </summary>
    PpUnaryMinus,

    /// <summary>
    /// used internally, never returned.
    /// </summary>
    PpUnaryPlus
}