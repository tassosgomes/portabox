namespace PortaBox.Modules.Gestao.Application.Common;

public static class Masking
{
    public static string Cnpj(string? value)
    {
        var digits = DigitsOnly(value);
        if (digits.Length == 0)
        {
            return string.Empty;
        }

        if (digits.Length <= 4)
        {
            return digits;
        }

        var visibleDigits = digits.Length >= 7 ? digits[^7..] : digits;
        return string.Concat("****", visibleDigits);
    }

    public static string Cpf(string? value)
    {
        var digits = DigitsOnly(value);
        if (digits.Length != 11)
        {
            return string.Empty;
        }

        return $"***.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-**";
    }

    public static string Celular(string? value)
    {
        var digits = DigitsOnly(value);
        if (digits.Length >= 13)
        {
            var countryCode = digits[..2];
            var areaCode = digits.Substring(2, 2);
            var prefix = digits.Substring(4, 1);
            var suffix = digits[^4..];
            return $"+{countryCode} {areaCode} {prefix}****-{suffix}";
        }

        if (digits.Length > 4)
        {
            return string.Concat(new string('*', digits.Length - 4), digits[^4..]);
        }

        return digits;
    }

    private static string DigitsOnly(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());
    }
}
