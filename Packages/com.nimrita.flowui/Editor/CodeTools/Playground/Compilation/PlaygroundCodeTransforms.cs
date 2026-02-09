#if UNITY_EDITOR
using System.Text.RegularExpressions;

namespace Nimrita.FlowUI.Editor.Playground
{
    /// <summary>
    /// Central place for source transforms applied before compilation.
    /// </summary>
    public static class PlaygroundCodeTransforms
    {
        private static readonly Regex ForWhileForeachRegex = new Regex(
            @"(?<header>\b(for|while|foreach)\s*\([^)]*\)\s*)\{",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex DoWhileRegex = new Regex(
            @"\bdo\s*\{",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private const string LoopGuardCheck = "LoopGuard.Check();";

        public static string ApplyAll(string userCode)
        {
            if (string.IsNullOrEmpty(userCode))
            {
                return userCode;
            }

            string transformed = InjectLoopGuards(userCode);
            return transformed;
        }

        private static string InjectLoopGuards(string code)
        {
            // Insert LoopGuard.Check() as the first statement inside loop bodies with braces.
            string result = ForWhileForeachRegex.Replace(code, match =>
            {
                string header = match.Groups["header"].Value;
                return $"{header}{{ {LoopGuardCheck}";
            });

            result = DoWhileRegex.Replace(result, match =>
            {
                return $"do {{ {LoopGuardCheck}";
            });

            return result;
        }
    }
}
#endif
