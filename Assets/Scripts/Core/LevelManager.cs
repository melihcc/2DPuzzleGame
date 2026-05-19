using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Levels")]
    public LevelData[] levels;

    public LevelData CurrentLevel { get; private set; }

    public int CurrentLevelArrayIndex { get; private set; }

    public int CurrentLevelIndex => CurrentLevel.levelIndex;
    public int GridWidth => CurrentLevel.gridWidth;
    public int GridHeight => CurrentLevel.gridHeight;
    public int MoveLimit => CurrentLevel.moveLimit;
    public int TargetScore => CurrentLevel.targetScore;

    private void Awake()
    {
        int savedLevelIndex = PlayerPrefs.GetInt("CurrentLevelArrayIndex", 0);
        LoadLevel(savedLevelIndex);
    }

    public void LoadLevel(int arrayIndex)
    {
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("No levels assigned to LevelManager!");
            return;
        }

        if (arrayIndex >= levels.Length)
            arrayIndex = 0;

        if (arrayIndex < 0)
            arrayIndex = 0;

        CurrentLevelArrayIndex = arrayIndex;
        CurrentLevel = levels[CurrentLevelArrayIndex];
    }

    public void LoadNextLevel()
    {
        int nextIndex = CurrentLevelArrayIndex + 1;

        if (nextIndex >= levels.Length)
            nextIndex = 0;

        PlayerPrefs.SetInt("CurrentLevelArrayIndex", nextIndex);
        PlayerPrefs.Save();

        LoadLevel(nextIndex);
    }

    public void ReloadCurrentLevel()
    {
        LoadLevel(CurrentLevelArrayIndex);
    }
}