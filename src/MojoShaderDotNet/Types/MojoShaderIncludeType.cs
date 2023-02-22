namespace MojoShaderDotNet.Types;

/// <summary>
/// Used with the MOJOSHADER_includeOpen callback. Maps to D3DXINCLUDE_TYPE.
/// [MOJOSHADER_includeType; mojoshader.h]
/// </summary>
public enum MojoShaderIncludeType
{
    /// <summary>
    /// MOJOSHADER_INCLUDETYPE_LOCAL.
    /// local header: #include "blah.h"
    /// </summary>
    Local,

    /// <summary>
    /// MOJOSHADER_INCLUDETYPE_SYSTEM.
    /// system header: #include &lt;blah.h&gt;
    /// </summary>
    System
}