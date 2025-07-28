using System.Collections.Generic;

public static class LevelLibrary
{
    // 📜 Static list of ALL levels
    public static readonly List<LevelConfig> AllLevels = new List<LevelConfig>
    {
        new Level1(),
         new Level2(),
        // new Level3(),
        // … add more here
    };

    // ✅ This now expects level numbers starting at 1 (not 0)
    public static LevelConfig GetLevel(int levelNumber)
    {
        int index = levelNumber - 1;

        if (index < 0 || index >= AllLevels.Count)
            return null;

        return AllLevels[index];
    }

    public static int TotalLevels => AllLevels.Count;
}
