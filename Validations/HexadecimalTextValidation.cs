using ReactiveUI.Validation.States;
using System.Linq;

namespace CommanderX16Launcher.Validations
{
    public class HexadecimalTextValidation
    {
        public static IValidationState Validate(string text, bool validationEnabled = false)
        {
            if (!validationEnabled)
                return ValidationState.Valid;

            return Validate(text);
        }

        public static IValidationState Validate(string text)
        {
            if (!string.IsNullOrWhiteSpace(text) && !text.All(char.IsAsciiHexDigit))
                return new ValidationState(false, "Invalid Hex Address");

            return ValidationState.Valid;
        }
    }
}
