using Arcade.Heist.Game.Models;
using Arcade.Heist.Game.Words;

namespace Arcade.Heist.Game.Engine;

public class PuzzleManager
{
    private readonly WordBank _wordBank;
    private readonly WordScrambler _scrambler;

    public PuzzleManager(WordBank wordBank, WordScrambler scrambler)
    {
        _wordBank = wordBank;
        _scrambler = scrambler;
    }

    public Puzzle GeneratePuzzle(int level)
    {
        int tier = _wordBank.GetTierForLevel(level);

        if (tier == 5)
        {
            var pair = _wordBank.GetWordPair();
            var original = $"{pair[0]} {pair[1]}";
            var scrambled = _scrambler.ScrambleDouble(pair);
            return new Puzzle
            {
                OriginalWord = original,
                ScrambledWord = scrambled,
                DifficultyTier = tier
            };
        }

        var word = _wordBank.GetWord(tier);
        return new Puzzle
        {
            OriginalWord = word,
            ScrambledWord = _scrambler.Scramble(word),
            DifficultyTier = tier
        };
    }

    public bool CheckAnswer(Puzzle puzzle, string answer)
    {
        return string.Equals(
            puzzle.OriginalWord.Trim(),
            answer.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    public Puzzle Rescramble(Puzzle puzzle)
    {
        string newScrambled;
        if (puzzle.DifficultyTier == 5)
        {
            var parts = puzzle.OriginalWord.Split(' ');
            newScrambled = _scrambler.ScrambleDouble(parts);
        }
        else
        {
            newScrambled = _scrambler.Scramble(puzzle.OriginalWord);
        }

        return new Puzzle
        {
            OriginalWord = puzzle.OriginalWord,
            ScrambledWord = newScrambled,
            DifficultyTier = puzzle.DifficultyTier
        };
    }

    public string GetHint(Puzzle puzzle)
    {
        var word = puzzle.OriginalWord;
        if (word.Length <= 3)
            return $"The word starts with **{word[0]}**";

        // Reveal first and last letter
        return $"Hint: **{word[0]}**{"".PadRight(word.Length - 2, '_')}**{word[^1]}** ({word.Length} letters)";
    }

    public string RevealLetter(Puzzle puzzle)
    {
        var word = puzzle.OriginalWord;
        var random = new Random();
        var index = random.Next(1, word.Length - 1); // Reveal a middle letter
        var masked = word.Select((c, i) => i == 0 || i == word.Length - 1 || i == index ? c : '_');
        return $"Spy reveals: **{string.Join("", masked)}**";
    }
}
