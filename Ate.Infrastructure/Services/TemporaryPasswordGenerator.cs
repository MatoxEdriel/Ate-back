using System.Security.Cryptography;

namespace Ate.Infrastructure.Services;

internal static class TemporaryPasswordGenerator
{
    private const int Length = 12;
    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijkmnpqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string SymbolChars = "*#$%&@!";

    public static string Generate()
    {
        var allChars = UpperChars + LowerChars + DigitChars + SymbolChars;
        var chars = new List<char>
        {
            UpperChars[RandomNumberGenerator.GetInt32(UpperChars.Length)],
            LowerChars[RandomNumberGenerator.GetInt32(LowerChars.Length)],
            DigitChars[RandomNumberGenerator.GetInt32(DigitChars.Length)],
            SymbolChars[RandomNumberGenerator.GetInt32(SymbolChars.Length)]
        };

        while (chars.Count < Length)
        {
            chars.Add(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
        }

        var shuffled = chars.ToArray();
        RandomNumberGenerator.Shuffle(shuffled.AsSpan());
        return new string(shuffled);
    }
}
