using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("SFX Clips")]
    public AudioClip mergeClip;
    public AudioClip wildMergeClip;
    public AudioClip bombClip;
    public AudioClip winClip;
    public AudioClip loseClip;
    public AudioClip hintClip;
    public AudioClip undoClip;
    public AudioClip moveClip;

    public void PlayMergeSound()      => Play(mergeClip);
    public void PlayWildMergeSound()  => Play(wildMergeClip != null ? wildMergeClip : mergeClip);
    public void PlayBombSound()       => Play(bombClip);
    public void PlayWinSound()        => Play(winClip);
    public void PlayLoseSound()       => Play(loseClip);
    public void PlayHintSound()       => Play(hintClip);
    public void PlayUndoSound()       => Play(undoClip);
    public void PlayMoveSound()       => Play(moveClip);

    private void Play(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip);
    }
}
