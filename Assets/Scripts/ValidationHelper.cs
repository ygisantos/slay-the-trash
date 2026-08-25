using System.Text.RegularExpressions;

public static class ValidationHelper
{
    // =========================
    // BASIC
    // =========================

    public static bool IsNullOrEmpty(string value)
    {
        return string.IsNullOrEmpty(value);
    }

    public static bool IsNullOrWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static bool IsNotEmpty(string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }


    // =========================
    // LENGTH
    // =========================

    public static bool IsMinLength(string value, int minLength)
    {
        return !string.IsNullOrEmpty(value) && value.Length >= minLength;
    }

    public static bool IsMaxLength(string value, int maxLength)
    {
        return !string.IsNullOrEmpty(value) && value.Length <= maxLength;
    }

    public static bool IsLengthBetween(string value, int minLength, int maxLength)
    {
        return !string.IsNullOrEmpty(value)
            && value.Length >= minLength
            && value.Length <= maxLength;
    }

    public static bool IsExactLength(string value, int length)
    {
        return !string.IsNullOrEmpty(value) && value.Length == length;
    }


    // =========================
    // NUMBERS
    // =========================

    public static bool IsNumber(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && double.TryParse(value, out _);
    }

    public static bool IsInteger(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && int.TryParse(value, out _);
    }

    public static bool IsPositiveNumber(string value)
    {
        return double.TryParse(value, out double number) && number > 0;
    }

    public static bool IsNonNegativeNumber(string value)
    {
        return double.TryParse(value, out double number) && number >= 0;
    }

    public static bool IsNegativeNumber(string value)
    {
        return double.TryParse(value, out double number) && number < 0;
    }

    public static bool IsNumberBetween(string value, double min, double max)
    {
        return double.TryParse(value, out double number)
            && number >= min
            && number <= max;
    }


    // =========================
    // TEXT FORMAT
    // =========================

    public static bool IsNumbersOnly(string value)
    {
        return !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"^\d+$");
    }

    public static bool IsLettersOnly(string value)
    {
        return !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"^[a-zA-Z]+$");
    }

    public static bool IsLettersAndSpacesOnly(string value)
    {
        return !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"^[a-zA-Z\s]+$");
    }

    public static bool IsAlphaNumeric(string value)
    {
        return !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"^[a-zA-Z0-9]+$");
    }

    public static bool IsAlphaNumericWithSpaces(string value)
    {
        return !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"^[a-zA-Z0-9\s]+$");
    }

    public static bool IsUsername(string value)
    {
        return !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"^[a-zA-Z0-9_]+$");
    }


    // =========================
    // EMAIL
    // =========================

    public static bool IsEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Regex.IsMatch(
            value,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
        );
    }


    // =========================
    // PHONE
    // =========================

    public static bool IsPhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Regex.IsMatch(value, @"^\+?[0-9]{7,15}$");
    }

    public static bool IsPhilippinePhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // 09XXXXXXXXX
        // +639XXXXXXXXX
        return Regex.IsMatch(
            value,
            @"^(09\d{9}|\+639\d{9})$"
        );
    }


    // =========================
    // PASSWORD
    // =========================

    public static bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        // At least:
        // 8 characters
        // 1 uppercase
        // 1 lowercase
        // 1 number
        // 1 special character
        return Regex.IsMatch(
            password,
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$"
        );
    }

    public static bool IsPasswordLengthValid(
        string password,
        int minLength = 8,
        int maxLength = 128)
    {
        return !string.IsNullOrEmpty(password)
            && password.Length >= minLength
            && password.Length <= maxLength;
    }


    // =========================
    // MATCHING
    // =========================

    public static bool IsMatch(string value1, string value2)
    {
        return value1 == value2;
    }

    public static bool IsMatchIgnoreCase(string value1, string value2)
    {
        return string.Equals(
            value1,
            value2,
            System.StringComparison.OrdinalIgnoreCase
        );
    }


    // =========================
    // URL
    // =========================

    public static bool IsUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Regex.IsMatch(
            value,
            @"^(https?|ftp)://[^\s/$.?#].[^\s]*$",
            RegexOptions.IgnoreCase
        );
    }


    // =========================
    // SPECIAL CHARACTERS
    // =========================

    public static bool ContainsNumber(string value)
    {
        return !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"\d");
    }

    public static bool ContainsLetter(string value)
    {
        return !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"[a-zA-Z]");
    }

    public static bool ContainsSpecialCharacter(string value)
    {
        return !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"[^a-zA-Z0-9\s]");
    }

    public static bool ContainsUppercase(string value)
    {
        return !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"[A-Z]");
    }

    public static bool ContainsLowercase(string value)
    {
        return !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"[a-z]");
    }


    // =========================
    // COMMON VALIDATION
    // =========================

    public static bool IsValidName(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && Regex.IsMatch(value, @"^[a-zA-Z\s.'-]+$");
    }

    public static bool IsValidDate(string value)
    {
        return System.DateTime.TryParse(value, out _);
    }

    public static bool IsValidGuid(string value)
    {
        return System.Guid.TryParse(value, out _);
    }
}