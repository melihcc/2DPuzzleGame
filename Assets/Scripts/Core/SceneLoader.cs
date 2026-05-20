using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void LoadLevelSelect() => SceneManager.LoadScene("LevelSelect");
    public static void LoadGameplay()    => SceneManager.LoadScene("Gameplay");
}
