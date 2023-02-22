// using MojoShaderDotNet.Profiles;
//
// namespace MojoShaderDotNet.Types;
//
// /// <summary>
// /// [mojoshader_internal.h]
// /// </summary>
// public class MojoShaderInstructionDefinition
// {
//     /// <summary>
//     /// INSTRUCTION_STATE macro
//     /// </summary>
//     public static MojoShaderInstruction CreateState(
//         MojoShaderOpcode op,
//         string opStr,
//         int slots,
//         MojoShaderInstructionArgs args,
//         MojoShaderShaderType type,
//         int writeMask) =>
//         new()
//         {
//             State = true,
//             Op = op,
//             OpStr = opStr,
//             Slots = slots,
//             Args = args,
//             Type = type,
//             WriteMask = writeMask
//         };
//
//     /// <summary>
//     /// INSTRUCTION macro
//     /// </summary>
//     public static MojoShaderInstruction Create(
//         MojoShaderOpcode op,
//         string opStr,
//         int slots,
//         MojoShaderInstructionArgs args,
//         MojoShaderShaderType type,
//         int writeMask) =>
//         new()
//         {
//             State = false,
//             Op = op,
//             OpStr = opStr,
//             Slots = slots,
//             Args = args,
//             Type = type,
//             WriteMask = writeMask
//         };
// }