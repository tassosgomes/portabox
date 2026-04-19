namespace PortaBox.Modules.Gestao.Application.Validators;

public static class CpfValidator
{
    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new string(value.Where(char.IsDigit).ToArray());
    }

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());

        if (digits.Length != 11)
        {
            return false;
        }

        if (digits.Distinct().Count() == 1)
        {
            return false;
        }

        return digits[9] == CalculateVerifierDigit(digits, 9) &&
               digits[10] == CalculateVerifierDigit(digits, 10);
    }

    private static char CalculateVerifierDigit(string digits, int length)
    {
        var sum = 0;

        for (var index = 0; index < length; index++)
        {
            sum += (digits[index] - '0') * ((length + 1) - index);
        }

        var remainder = sum % 11;
        var verifier = remainder < 2 ? 0 : 11 - remainder;

        return (char)('0' + verifier);
    }
}
