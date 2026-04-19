namespace PortaBox.Modules.Gestao.Application.Validators;

public static class CnpjValidator
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

        if (digits.Length != 14)
        {
            return false;
        }

        if (digits.Distinct().Count() == 1)
        {
            return false;
        }

        return digits[12] == CalculateVerifierDigit(digits, 12) &&
               digits[13] == CalculateVerifierDigit(digits, 13);
    }

    private static char CalculateVerifierDigit(string digits, int length)
    {
        ReadOnlySpan<int> firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        ReadOnlySpan<int> secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var weights = length == 12 ? firstWeights : secondWeights;
        var sum = 0;

        for (var index = 0; index < length; index++)
        {
            sum += (digits[index] - '0') * weights[index];
        }

        var remainder = sum % 11;
        var verifier = remainder < 2 ? 0 : 11 - remainder;

        return (char)('0' + verifier);
    }
}
