namespace Lumina.Core;

public static class InstanceWizardPolicy
{
    public static bool CanCreateInstance(bool signedIn, string? name, InstanceValidationResult validation) =>
        signedIn &&
        !string.IsNullOrWhiteSpace(name) &&
        validation.IsValid;
}
