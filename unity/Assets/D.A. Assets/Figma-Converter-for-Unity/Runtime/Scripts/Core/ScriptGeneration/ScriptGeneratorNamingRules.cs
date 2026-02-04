using DA_Assets.Constants;
using DA_Assets.Extensions;
using DA_Assets.FCU.Model;
using System;
using System.Linq;

namespace DA_Assets.FCU
{
    public enum ScriptGeneratorNameType
    {
        Field,
        Method,
        Class
    }

    public static class ScriptGeneratorNamingRules
    {
        public static ScriptGeneratorValidationResult Validate(
            string input,
            ScriptGeneratorNameType nameType,
            ScriptGeneratorSettings settings)
        {
            string sanitized = (input ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(sanitized))
            {
                return ScriptGeneratorValidationResult.Invalid(sanitized, FcuLocKey.scriptgen_validation_empty.Localize());
            }

            int maxLength = GetMaxLength(nameType, settings);
            if (maxLength > 0 && sanitized.Length > maxLength)
            {
                return ScriptGeneratorValidationResult.Invalid(sanitized, FcuLocKey.scriptgen_validation_length.Localize(maxLength));
            }

            if (!IsValidStartCharacter(sanitized[0]))
            {
                return ScriptGeneratorValidationResult.Invalid(sanitized, FcuLocKey.scriptgen_validation_startchar.Localize());
            }

            for (int i = 0; i < sanitized.Length; i++)
            {
                if (!IsValidBodyCharacter(sanitized[i]))
                {
                    return ScriptGeneratorValidationResult.Invalid(sanitized, FcuLocKey.scriptgen_validation_charset.Localize());
                }
            }

            if (CsSharpKeywords.Keywords.Contains(sanitized))
            {
                return ScriptGeneratorValidationResult.Invalid(sanitized, FcuLocKey.scriptgen_validation_keyword.Localize());
            }

            return ScriptGeneratorValidationResult.Valid(sanitized);
        }

        public static string GetRulesDescription(ScriptGeneratorNameType nameType, ScriptGeneratorSettings settings)
        {
            int maxLength = GetMaxLength(nameType, settings);
            return FcuLocKey.scriptgen_rules_description.Localize(maxLength);
        }

        private static int GetMaxLength(ScriptGeneratorNameType type, ScriptGeneratorSettings settings)
        {
            if (settings == null)
            {
                return 0;
            }

            return type switch
            {
                ScriptGeneratorNameType.Method => settings.MethodNameMaxLenght,
                ScriptGeneratorNameType.Class => settings.ClassNameMaxLenght,
                _ => settings.FieldNameMaxLenght
            };
        }

        private static bool IsValidStartCharacter(char character) => char.IsLetter(character) || character == '_';
        private static bool IsValidBodyCharacter(char character) => char.IsLetterOrDigit(character) || character == '_';
    }

    public readonly struct ScriptGeneratorValidationResult
    {
        public ScriptGeneratorValidationResult(bool isValid, string sanitizedValue, string message)
        {
            IsValid = isValid;
            SanitizedValue = sanitizedValue ?? string.Empty;
            Message = message;
        }

        public bool IsValid { get; }
        public string SanitizedValue { get; }
        public string Message { get; }

        public static ScriptGeneratorValidationResult Invalid(string sanitizedValue, string message) =>
            new ScriptGeneratorValidationResult(false, sanitizedValue, message);

        public static ScriptGeneratorValidationResult Valid(string sanitizedValue) =>
            new ScriptGeneratorValidationResult(true, sanitizedValue, null);
    }
}
