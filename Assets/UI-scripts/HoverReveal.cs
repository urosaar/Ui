using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Attach to a UI Button. While the cursor hovers over it this script:
//  1. Fades OUT the button's Image (it must stay enabled or hover detection flickers!)
//  2. Activates another UI element and smoothly scales it up + moves it up (like a CSS hover)
//  3. Reverts everything (and hides the element again) when the cursor leaves
//
// Setup:
//  - Drag the element you want to reveal into "Element To Activate"
//  - Make sure "Element To Activate" starts as INACTIVE (unchecked) in the scene
//  - Optionally drag an Image into "Image To Hide" if it's not the button's own image
//
// Note: the button's Image is NEVER disabled - only its alpha is faded. Disabling it
// would make the button invisible to the EventSystem and cause hover flicker.
public class HoverReveal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Revealed Element")]
    [Tooltip("The UI element that gets shown while the cursor is over the button. Should start inactive in the scene.")]
    [SerializeField] private GameObject elementToActivate;

    [Header("Button Image")]
    [Tooltip("Image that fades out while hovering. Leave empty to use this button's own Image.")]
    [SerializeField] private Image imageToHide;

    [Header("Hover Animation")]
    [Tooltip("How much the revealed element scales up on hover (1 = original size).")]
    [SerializeField] private float hoverScale = 1.08f;
    [Tooltip("How far the revealed element moves up (in units) on hover.")]
    [SerializeField] private float hoverOffsetUp = 30f;
    [Tooltip("Seconds it takes to reach the hover state, or return back.")]
    [SerializeField] private float transitionDuration = 0.15f;

    [Header("Raycasting (flicker prevention)")]
    [Tooltip("Should the revealed element's graphics still catch the cursor? Leave false (default) so it never steals hover from the button if it overlaps it.")]
    [SerializeField] private bool revealedElementBlocksRaycasts = false;

    private RectTransform targetRect;
    private Image defaultImage;
    private Color defaultImageColor;
    private Vector3 originalScale;
    private Vector2 originalPosition;

    private void Awake()
    {
        // Remember the button's own Image so we can fade it out/in
        if (imageToHide == null)
        {
            defaultImage = GetComponent<Image>();
        }

        if (imageToHide != null) defaultImageColor = imageToHide.color;
        else if (defaultImage != null) defaultImageColor = defaultImage.color;

        // Make sure the revealed element only appears when we say so
        if (elementToActivate != null && elementToActivate.activeSelf)
        {
            elementToActivate.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (elementToActivate == null) return;

        CacheTarget();
        elementToActivate.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateHover(true));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Animate the revealed element back down, then hide it
        if (elementToActivate != null && elementToActivate.activeSelf)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateHover(false));
        }
    }

    private void OnDisable()
    {
        // If this button (or its parent) is deactivated mid-hover - e.g. clicking it
        // to open another panel - OnPointerExit never fires, leaving the effect stuck.
        // Forcefully reset everything here so no stale hover state survives.
        StopAllCoroutines();
        RevertHoverState();
    }

    private void OnEnable()
    {
        // Defensive reset when coming back to this screen. If the cursor is genuinely
        // over the button, the EventSystem will automatically fire OnPointerEnter
        // again, so reverting here never hides a real hover.
        StopAllCoroutines();
        RevertHoverState();
    }

    private void RevertHoverState()
    {
        // Restore the button's image to full opacity
        if (imageToHide != null || defaultImage != null)
        {
            SetImageAlpha(defaultImageColor.a);
        }

        // Reset the revealed element to its original spot and hide it
        if (elementToActivate != null)
        {
            if (targetRect != null)
            {
                targetRect.localScale = originalScale;
                targetRect.anchoredPosition = originalPosition;
            }
            elementToActivate.SetActive(false);
        }
    }

    private void CacheTarget()
    {
        targetRect = elementToActivate.GetComponent<RectTransform>();
        originalScale = targetRect.localScale;
        originalPosition = targetRect.anchoredPosition;

        // Flicker prevention: if the revealed element overlaps the button and its
        // graphics are raycast targets, the cursor suddenly points at IT instead of
        // the button -> OnPointerExit fires -> loop. Disable its raycasts by default.
        if (!revealedElementBlocksRaycasts)
        {
            foreach (Graphic graphic in elementToActivate.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }
    }

    private void SetImageAlpha(float alpha)
    {
        if (imageToHide == null && defaultImage == null) return;

        Image image = imageToHide != null ? imageToHide : defaultImage;
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private IEnumerator AnimateHover(bool hovering)
    {
        // The state we animate FROM is wherever the element currently is
        Vector3 fromScale = targetRect.localScale;
        Vector2 fromPosition = targetRect.anchoredPosition;

        // The state we animate TO (up + slightly bigger, or back to normal)
        Vector3 toScale = hovering ? originalScale * hoverScale : originalScale;
        Vector2 toPosition = hovering ? originalPosition + new Vector2(0f, hoverOffsetUp) : originalPosition;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            // SmoothStep gives a nice ease-in-out, close to a CSS transition
            float eased = Mathf.SmoothStep(0f, 1f, t);

            targetRect.localScale = Vector3.Lerp(fromScale, toScale, eased);
            targetRect.anchoredPosition = Vector2.Lerp(fromPosition, toPosition, eased);

            // Fade the button's image out while hovering, back in on exit.
            // We only change alpha - the Image stays enabled so it keeps raycasting.
            if (hovering)
            {
                SetImageAlpha(Mathf.Lerp(defaultImageColor.a, 0f, eased));
            }
            else
            {
                SetImageAlpha(Mathf.Lerp(0f, defaultImageColor.a, eased));
            }

            yield return null;
        }

        // Snap to the final values in case we stopped slightly early
        targetRect.localScale = toScale;
        targetRect.anchoredPosition = toPosition;
        SetImageAlpha(hovering ? 0f : defaultImageColor.a);

        // Only hide the element once it's fully back at its original spot
        if (!hovering && targetRect.localScale == originalScale)
        {
            elementToActivate.SetActive(false);
        }
    }
}