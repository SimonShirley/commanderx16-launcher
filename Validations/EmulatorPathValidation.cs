using ReactiveUI.Validation.States;
using System.IO;

namespace CommanderX16Launcher.Validations
{
    public class EmulatorPathValidation
    {
        public static IValidationState ValidateEmpty(string emulatorPath) => Validate(emulatorPath, true);
        public static IValidationState Validate(string emulatorPath) => Validate(emulatorPath, false);

        private static IValidationState Validate(string emulatorPath, bool canBeEmpty)
        {
            if (canBeEmpty && string.IsNullOrWhiteSpace(emulatorPath))
                return ValidationState.Valid;

            if (File.Exists(emulatorPath) && (emulatorPath.EndsWith("x16emu") || emulatorPath.EndsWith("x16emu.exe")))
                return ValidationState.Valid;

            return new ValidationState(false, "Emulator Path is not the Commander X16 Emulator");
        }
    }
}
