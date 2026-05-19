using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [Header("Shake Settings")]
    public float defaultDuration = 0.08f;
    public float defaultStrength = 0.05f;

    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        originalPosition = transform.position;
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultStrength);
    }

    public void Shake(float duration, float strength)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, strength));
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        originalPosition = transform.position;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float offsetX = Random.Range(-strength, strength);
            float offsetY = Random.Range(-strength, strength);

            transform.position = originalPosition + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        transform.position = originalPosition;
        shakeCoroutine = null;
    }
}