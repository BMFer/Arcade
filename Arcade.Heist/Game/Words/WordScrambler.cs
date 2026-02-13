namespace Arcade.Heist.Game.Words;

public class WordScrambler
{
    private static readonly Random _rng = new();

    public string Scramble(string word)
    {
        var chars = word.ToLowerInvariant().ToCharArray();
        var original = new string(chars);

        // Try up to 20 times to get a different arrangement
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Shuffle(chars);
            var scrambled = new string(chars);
            if (scrambled != original)
                return scrambled;
        }

        // Fallback: swap first and last characters
        (chars[0], chars[^1]) = (chars[^1], chars[0]);
        return new string(chars);
    }

    public string ScrambleDouble(string[] words)
    {
        var scrambled1 = Scramble(words[0]);
        var scrambled2 = Scramble(words[1]);
        return $"{scrambled1} {scrambled2}";
    }

    private static void Shuffle(char[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
