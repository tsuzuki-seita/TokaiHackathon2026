using UnityEngine;

// Shared score storage so each mini-game scene in this project can report its own result (0..1)
// and any scene (e.g. a final results/hub scene) can read the combined total across all of them.
// Backed by PlayerPrefs, so it survives scene loads without needing a DontDestroyOnLoad object.
public static class MiniGameScoreBoard
{
    const string TotalKey = "MiniGameScoreBoard_Total";
    const string ScoreKeyPrefix = "MiniGameScoreBoard_Score_";

    // Records (or overwrites, if replayed) this mini-game's score and updates the running total.
    public static void ReportScore(string miniGameId, float score01)
    {
        var previous = GetScore(miniGameId);
        PlayerPrefs.SetFloat(ScoreKeyPrefix + miniGameId, score01);
        PlayerPrefs.SetFloat(TotalKey, TotalScore - previous + score01);
        PlayerPrefs.Save();
    }

    public static float GetScore(string miniGameId) => PlayerPrefs.GetFloat(ScoreKeyPrefix + miniGameId, 0f);

    public static float TotalScore => PlayerPrefs.GetFloat(TotalKey, 0f);
}
