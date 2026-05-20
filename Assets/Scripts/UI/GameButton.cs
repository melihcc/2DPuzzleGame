using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Figma/React tasarımındaki GameButton bileşenini Unity'de karşılar.
/// Primary / Secondary / Icon varyantlarını destekler.
/// </summary>
[RequireComponent(typeof(Button))]
public class GameButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum Variant { Primary, Secondary, Icon }

    [Header("Variant")]
    public Variant variant = Variant.Primary;

    [Header("References")]
    public Image      backgroundImage;
    public Image      gradientOverlayImage; // isteğe bağlı: white/20 parlama
    public TMP_Text   label;
    public Image      outlineImage;         // Secondary için ayrı border Image (opsiyonel)

    [Header("Tap Animation")]
    [Tooltip("Basılınca ölçek çarpanı (primary/secondary=0.98, icon=0.95)")]
    public float pressScale    = 0.98f;
    public float pressDuration = 0.06f;

    // ─── Renkler (Figma design system) ────────────────────────────────────────
    private static readonly Color PrimaryColor   = new Color(1.00f, 0.84f, 0.00f); // #FFD700
    private static readonly Color PrimaryColor2  = new Color(1.00f, 0.65f, 0.00f); // #FFA500 (gradient sonu)
    private static readonly Color TextDark       = new Color(0.102f, 0.102f, 0.180f); // #1A1A2E
    private static readonly Color SurfaceColor   = new Color(0.145f, 0.145f, 0.271f); // #252545
    private static readonly Color DisabledColor  = new Color(0.40f, 0.40f, 0.40f);
    private static readonly Color GoldText       = new Color(1.00f, 0.84f, 0.00f);
    private static readonly Color Overlay        = new Color(1f, 1f, 1f, 0.20f);

    private Button       button;
    private CanvasGroup  canvasGroup;
    private Vector3      originalScale;
    private Coroutine    scaleCoroutine;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        button        = GetComponent<Button>();
        originalScale = transform.localScale;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        ApplyVariantStyle();
    }

    // ─── Stil uygulama ────────────────────────────────────────────────────────

    private void ApplyVariantStyle()
    {
        switch (variant)
        {
            case Variant.Primary:
                // Gold: gradient sprite yoksa düz #FFD700, varsa sprite halleder
                if (backgroundImage != null)
                    backgroundImage.color = PrimaryColor;

                if (gradientOverlayImage != null)
                    gradientOverlayImage.color = Overlay;

                if (label != null)
                    label.color = TextDark;

                if (outlineImage != null)
                    outlineImage.gameObject.SetActive(false);

                pressScale = 0.98f;
                break;

            case Variant.Secondary:
                // Transparent arka plan, gold metin.
                // Border için: Inspector'da outlineImage alanına ayrı bir Image bağla
                // (Outline component OutOfMemoryException'a yol açtığı için kod ile eklenmez)
                if (backgroundImage != null)
                    backgroundImage.color = Color.clear;

                if (gradientOverlayImage != null)
                    gradientOverlayImage.gameObject.SetActive(false);

                if (label != null)
                    label.color = GoldText;

                if (outlineImage != null)
                {
                    outlineImage.gameObject.SetActive(true);
                    outlineImage.color = PrimaryColor;
                }

                pressScale = 0.98f;
                break;

            case Variant.Icon:
                // Koyu yuvarlak arka plan
                if (backgroundImage != null)
                    backgroundImage.color = SurfaceColor;

                if (gradientOverlayImage != null)
                    gradientOverlayImage.gameObject.SetActive(false);

                if (outlineImage != null)
                    outlineImage.gameObject.SetActive(false);

                pressScale = 0.95f;
                break;
        }
    }

    // ─── Disabled ─────────────────────────────────────────────────────────────

    public void SetDisabled(bool isDisabled)
    {
        if (button != null) button.interactable = !isDisabled;
        // opacity: 1.0 → enabled, 0.5 → disabled (Figma: disabled:opacity-50)
        if (canvasGroup != null) canvasGroup.alpha = isDisabled ? 0.5f : 1f;

        if (isDisabled && backgroundImage != null && variant == Variant.Primary)
            backgroundImage.color = DisabledColor;
        else if (!isDisabled && variant == Variant.Primary && backgroundImage != null)
            backgroundImage.color = PrimaryColor;
    }

    // ─── Tap animasyonu (whileTap: scale) ────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        ScaleTo(originalScale * pressScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ScaleTo(originalScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Parmak buton dışına çıkarsa da geri dönsün
        ScaleTo(originalScale);
    }

    private void ScaleTo(Vector3 target)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(target));
    }

    private IEnumerator ScaleRoutine(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float   timer = 0f;

        while (timer < pressDuration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, target, timer / pressDuration);
            yield return null;
        }

        transform.localScale = target;
        scaleCoroutine = null;
    }

#if UNITY_EDITOR
    // Editörde varyant değişince stili önizle
    private void OnValidate()
    {
        if (!Application.isPlaying)
            ApplyVariantStyle();
    }
#endif
}
