using UnityEngine;

public class GameSettings : MonoBehaviour
{
    [Header("Performance")]
    public int targetFrameRate = 120;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }
}