using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource sfxSource;

    [Header("SFX")]
    public AudioClip mergeClip;

    public void PlayMergeSound()
    {
        if (sfxSource == null || mergeClip == null)
            return;

        sfxSource.PlayOneShot(mergeClip);
    }
}