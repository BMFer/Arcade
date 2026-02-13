namespace Arcade.Heist.Game.Words;

public class WordBank
{
    private static readonly Random _rng = new();

    // Tier 1: 4-5 letter common words (Level 1)
    private static readonly string[] Tier1 =
    [
        "gold", "safe", "lock", "keys", "coin", "cash", "door", "wall", "maze", "trap",
        "code", "wire", "risk", "loot", "grab", "dash", "hide", "move", "plan", "team",
        "mask", "dark", "lamp", "rope", "bags", "hunt", "snag", "slip", "plot", "crew",
        "bank", "drop", "fire", "jump", "race", "spin", "turn", "gain", "rank", "push",
        "bolt", "gate", "path", "ring", "card", "dice", "luck", "edge", "mark", "haul"
    ];

    // Tier 2: 6-7 letter words (Level 2)
    private static readonly string[] Tier2 =
    [
        "heist", "vault", "escape", "cipher", "decode", "shadow", "stealth", "breach",
        "guards", "sensor", "camera", "tunnel", "ladder", "window", "signal", "target",
        "reward", "danger", "puzzle", "scheme", "master", "thief", "bandit", "robber",
        "sniper", "ambush", "escape", "detect", "bypass", "shield", "hammer", "gadget",
        "dagger", "stolen", "ransom", "locker", "bunker", "agents", "decoys", "patrol",
        "motion", "access", "permit", "recon", "snatch", "hijack", "scrawl", "clutch",
        "plunge", "sprint"
    ];

    // Tier 3: 8-9 letter words (Level 3)
    private static readonly string[] Tier3 =
    [
        "treasure", "skeleton", "criminal", "dynamite", "disguise", "sabotage", "getaway",
        "criminal", "diamonds", "security", "password", "infrared", "biometry", "decrypts",
        "strategy", "lockpick", "blackout", "fugitive", "handcuff", "conspire",
        "firewall", "guardian", "protocol", "operatic", "illusion", "dominion", "overhaul",
        "fortress", "betrayal", "deadbolt", "perimeter", "intrusion", "espionage",
        "blueprint", "masterkey", "speakeasy", "crackdown", "moonlight", "underdog",
        "checkmate"
    ];

    // Tier 4: Themed heist words (Level 4)
    private static readonly string[] Tier4 =
    [
        "accomplice", "undercover", "extraction", "safecrack", "infiltrate",
        "distraction", "rendezvous", "silhouette", "encryption", "detonator",
        "blueprints", "masterplan", "conspiracy", "classified", "checkpoint",
        "chameleon", "operative", "espionage", "counterfeit", "neutralize",
        "dispatcher", "jailbreak", "mastermind", "barricade", "contingency",
        "locksmith", "decryption", "fugitives", "retrieval", "blackmail",
        "stronghold", "overwatch", "tripwire", "syndicate", "deception",
        "safehouse", "wheelsman", "ringleader", "scrambler", "warehouse"
    ];

    // Tier 5: Word pairs for double scrambles (Level 5)
    private static readonly string[][] Tier5 =
    [
        ["crown", "jewels"], ["master", "plan"], ["double", "agent"],
        ["secret", "code"], ["hidden", "safe"], ["laser", "grid"],
        ["smoke", "bomb"], ["vault", "door"], ["guard", "tower"],
        ["alarm", "system"], ["night", "shift"], ["power", "surge"],
        ["final", "heist"], ["grand", "theft"], ["ghost", "protocol"],
        ["black", "market"], ["blood", "diamond"], ["silent", "alarm"],
        ["escape", "route"], ["inside", "job"], ["gold", "reserve"],
        ["panic", "room"], ["watch", "tower"], ["steel", "cage"],
        ["flash", "bang"], ["getaway", "car"], ["shadow", "ops"],
        ["dead", "drop"], ["blind", "spot"], ["cold", "case"]
    ];

    public string GetWord(int tier)
    {
        var words = tier switch
        {
            1 => Tier1,
            2 => Tier2,
            3 => Tier3,
            4 => Tier4,
            _ => Tier1
        };

        return words[_rng.Next(words.Length)];
    }

    public string[] GetWordPair()
    {
        return Tier5[_rng.Next(Tier5.Length)];
    }

    public int GetTierForLevel(int level) => Math.Clamp(level, 1, 5);
}
