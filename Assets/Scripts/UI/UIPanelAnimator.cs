// using System.Collections;
// using UnityEngine;

// /// <summary>
// /// Modular UI animator that applies mathematical easing to CanvasGroups and RectTransforms.
// /// Eliminates instant UI popping with zero runtime memory allocations.
// /// </summary>
// [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
// [DisallowMultipleComponent]
// public class UIPanelAnimator : MonoBehaviour
// {
//     public enum TransitionStyle
//     {
//         ElasticPop,       // Best for: Main Menu, Pause, Game Over (Tactile & Snappy)
//         SlideFromRight,   // Best for: Sub-menus like How To Play or Credits
//         SlideFromBottom,  // Best for: Sheets, inventory, or action menus
//         FadeOnly          // Best for: Subtle overlays or HUD elements
//     }

//     [Header("Animation Settings")]
//     [SerializeField] private TransitionStyle transitionStyle = TransitionStyle.ElasticPop;
//     [Tooltip("Duration of the transition in seconds. Keep under 0.3s for snappy mobile feel.")]
//     [SerializeField] [Range(0.05f, 0.75f)] private float duration = 0.25f;

//     [Header("Slide Configuration")]
//     [Tooltip("Distance in screen pixels the panel slides from when using a Slide style.")]
//     [SerializeField] private float slideOffset = 800f;

//     // Zero-GC Cached References
//     private CanvasGroup canvasGroup;
//     private RectTransform rectTransform;
//     private Coroutine activeTransitionRoutine;

//     // Immutable Initial State
//     private Vector2 initialAnchoredPosition;
//     private Vector3 initialScale;

//     private void Awake()
//     {
//         //canvasGroup = GetComponent<CanvasGroup>();
//         //rectTransform = GetComponent<RectTransform>();

//         initialAnchoredPosition = rectTransform.anchoredPosition;
//         initialScale = rectTransform.localScale;
//     }

//     /// <summary>
//     /// Animates the panel into view and enables interaction.
//     /// </summary>
//     public void AnimateShow()
//     {
//         if (activeTransitionRoutine != null) StopCoroutine(activeTransitionRoutine);

//         gameObject.SetActive(true);
//         canvasGroup.interactable = true;
//         canvasGroup.blocksRaycasts = true;

//         activeTransitionRoutine = StartCoroutine(ExecuteShowRoutine());
//     }

//     /// <summary>
//     /// Animates the panel out of view and disables interaction.
//     /// </summary>
//     public void AnimateHide(bool immediate = false)
//     {
//         if (activeTransitionRoutine != null) StopCoroutine(activeTransitionRoutine);

//         canvasGroup.interactable = false;
//         canvasGroup.blocksRaycasts = false;

//         if (immediate)
//         {
//             canvasGroup.alpha = 0f;
//             gameObject.SetActive(false);
//         }
//         else
//         {
//             activeTransitionRoutine = StartCoroutine(ExecuteHideRoutine());
//         }
//     }

//     private IEnumerator ExecuteShowRoutine()
//     {
//         float elapsedTime = 0f;
//         canvasGroup.alpha = 0f;

//         // Pre-setup starting positions/scales based on chosen style
//         switch (transitionStyle)
//         {
//             case TransitionStyle.ElasticPop:
//                 rectTransform.localScale = initialScale * 0.7f;
//                 rectTransform.anchoredPosition = initialAnchoredPosition;
//                 break;
//             case TransitionStyle.SlideFromRight:
//                 rectTransform.anchoredPosition = initialAnchoredPosition + new Vector2(slideOffset, 0f);
//                 rectTransform.localScale = initialScale;
//                 break;
//             case TransitionStyle.SlideFromBottom:
//                 rectTransform.anchoredPosition = initialAnchoredPosition + new Vector2(0f, -slideOffset);
//                 rectTransform.localScale = initialScale;
//                 break;
//             case TransitionStyle.FadeOnly:
//                 rectTransform.anchoredPosition = initialAnchoredPosition;
//                 rectTransform.localScale = initialScale;
//                 break;
//         }

//         while (elapsedTime < duration)
//         {
//             // Use unscaled time so UI animations still play smoothly when Time.timeScale == 0 (Paused game!)
//             elapsedTime += Time.unscaledDeltaTime;
//             float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

//             // Linear alpha fade is visually clean
//             canvasGroup.alpha = normalizedTime;

//             // Apply mathematical easing to geometry
//             switch (transitionStyle)
//             {
//                 case TransitionStyle.ElasticPop:
//                     float popProgress = EaseOutBack(normalizedTime);
//                     rectTransform.localScale = Vector3.LerpUnclamped(initialScale * 0.7f, initialScale, popProgress);
//                     break;

//                 case TransitionStyle.SlideFromRight:
//                     float slideRightProgress = EaseOutCubic(normalizedTime);
//                     rectTransform.anchoredPosition = Vector2.LerpUnclamped(initialAnchoredPosition + new Vector2(slideOffset, 0f), initialAnchoredPosition, slideRightProgress);
//                     break;

//                 case TransitionStyle.SlideFromBottom:
//                     float slideBottomProgress = EaseOutCubic(normalizedTime);
//                     rectTransform.anchoredPosition = Vector2.LerpUnclamped(initialAnchoredPosition + new Vector2(0f, -slideOffset), initialAnchoredPosition, slideBottomProgress);
//                     break;
//             }

//             yield return null;
//         }

//         // Snap to exact target values to guarantee precision
//         canvasGroup.alpha = 1f;
//         rectTransform.localScale = initialScale;
//         rectTransform.anchoredPosition = initialAnchoredPosition;
//         activeTransitionRoutine = null;
//     }

//     private IEnumerator ExecuteHideRoutine()
//     {
//         float elapsedTime = 0f;
//         float startAlpha = canvasGroup.alpha;
//         Vector3 startScale = rectTransform.localScale;
//         Vector2 startPos = rectTransform.anchoredPosition;

//         while (elapsedTime < (duration * 0.75f)) // Hide slightly faster than show for snappy UX
//         {
//             elapsedTime += Time.unscaledDeltaTime;
//             float normalizedTime = Mathf.Clamp01(elapsedTime / (duration * 0.75f));

//             canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalizedTime);

//             if (transitionStyle == TransitionStyle.ElasticPop)
//             {
//                 // Smoothly shrink away without overshoot when closing
//                 rectTransform.localScale = Vector3.Lerp(startScale, initialScale * 0.8f, EaseInCubic(normalizedTime));
//             }
//             else if (transitionStyle == TransitionStyle.SlideFromRight)
//             {
//                 rectTransform.anchoredPosition = Vector2.Lerp(startPos, initialAnchoredPosition + new Vector2(slideOffset, 0f), EaseInCubic(normalizedTime));
//             }
//             else if (transitionStyle == TransitionStyle.SlideFromBottom)
//             {
//                 rectTransform.anchoredPosition = Vector2.Lerp(startPos, initialAnchoredPosition + new Vector2(0f, -slideOffset), EaseInCubic(normalizedTime));
//             }

//             yield return null;
//         }

//         canvasGroup.alpha = 0f;
//         gameObject.SetActive(false);
//         activeTransitionRoutine = null;
//     }

//     #region Pure Mathematical Easing Formulas (No Assets Required)

//     /// <summary>
//     /// Produces a satisfying overshoot snap. The secret sauce for tactile mobile UI!
//     /// </summary>
//     private float EaseOutBack(float x)
//     {
//         const float c1 = 1.70158f;
//         const float c3 = c1 + 1f;
//         return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
//     }

//     private float EaseOutCubic(float x)
//     {
//         return 1f - Mathf.Pow(1f - x, 3f);
//     }

//     private float EaseInCubic(float x)
//     {
//         return x * x * x;
//     }

//     #endregion
// }













































using System.Collections;
using UnityEngine;

/// <summary>
/// Modular UI animator that applies mathematical easing to CanvasGroups and RectTransforms[cite: 15].
/// Uses Lazy Initialization to guarantee safety even if UI panels start disabled in the Inspector.
/// </summary>
[RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
[DisallowMultipleComponent]
public class UIPanelAnimator : MonoBehaviour
{
    public enum TransitionStyle
    {
        ElasticPop,       // Best for: Main Menu, Pause, Game Over (Tactile & Snappy)[cite: 15]
        SlideFromRight,   // Best for: Sub-menus like How To Play or Credits[cite: 15]
        SlideFromBottom,  // Best for: Sheets, inventory, or action menus[cite: 15]
        FadeOnly          // Best for: Subtle overlays or HUD elements[cite: 15]
    }

    [Header("Animation Settings")]
    [SerializeField] private TransitionStyle transitionStyle = TransitionStyle.ElasticPop;
    [Tooltip("Duration of the transition in seconds. Keep under 0.3s for snappy mobile feel[cite: 15].")]
    [SerializeField][Range(0.05f, 0.75f)] private float duration = 0.25f;

    [Header("Slide Configuration")]
    [Tooltip("Distance in screen pixels the panel slides from when using a Slide style[cite: 15].")]
    [SerializeField] private float slideOffset = 800f;

    // Zero-GC Cached References[cite: 15]
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine activeTransitionRoutine;

    // Immutable Initial State[cite: 15]
    private Vector2 initialAnchoredPosition;
    private Vector3 initialScale;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    /// <summary>
    /// LAZY INITIALIZATION: Guarantees components are cached even if the GameObject 
    /// started disabled in the Editor and skipped its Unity Awake() cycle.
    /// </summary>
    private void InitializeIfNeeded()
    {
        if (isInitialized) return;

        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            initialAnchoredPosition = rectTransform.anchoredPosition;
            initialScale = rectTransform.localScale;
        }

        isInitialized = true;
    }

    /// <summary>
    /// Animates the panel into view and enables interaction[cite: 15].
    /// </summary>
    public void AnimateShow()
    {
        InitializeIfNeeded(); // Defensive check protects against startup race conditions

        if (activeTransitionRoutine != null) StopCoroutine(activeTransitionRoutine);

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        activeTransitionRoutine = StartCoroutine(ExecuteShowRoutine());
    }

    /// <summary>
    /// Animates the panel out of view and disables interaction[cite: 15].
    /// </summary>
    public void AnimateHide(bool immediate = false)
    {
        InitializeIfNeeded(); // Defensive check protects against inactive hierarchy bugs

        if (activeTransitionRoutine != null) StopCoroutine(activeTransitionRoutine);

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (immediate)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
        else
        {
            activeTransitionRoutine = StartCoroutine(ExecuteHideRoutine());
        }
    }

    private IEnumerator ExecuteShowRoutine()
    {
        float elapsedTime = 0f;
        canvasGroup.alpha = 0f;

        // Pre-setup starting positions/scales based on chosen style[cite: 15]
        switch (transitionStyle)
        {
            case TransitionStyle.ElasticPop:
                rectTransform.localScale = initialScale * 0.7f;
                rectTransform.anchoredPosition = initialAnchoredPosition;
                break;
            case TransitionStyle.SlideFromRight:
                rectTransform.anchoredPosition = initialAnchoredPosition + new Vector2(slideOffset, 0f);
                rectTransform.localScale = initialScale;
                break;
            case TransitionStyle.SlideFromBottom:
                rectTransform.anchoredPosition = initialAnchoredPosition + new Vector2(0f, -slideOffset);
                rectTransform.localScale = initialScale;
                break;
            case TransitionStyle.FadeOnly:
                rectTransform.anchoredPosition = initialAnchoredPosition;
                rectTransform.localScale = initialScale;
                break;
        }

        while (elapsedTime < duration)
        {
            // Use unscaled time so UI animations still play smoothly when Time.timeScale == 0[cite: 15]
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

            // Linear alpha fade is visually clean[cite: 15]
            canvasGroup.alpha = normalizedTime;

            // Apply mathematical easing to geometry[cite: 15]
            switch (transitionStyle)
            {
                case TransitionStyle.ElasticPop:
                    float popProgress = EaseOutBack(normalizedTime);
                    rectTransform.localScale = Vector3.LerpUnclamped(initialScale * 0.7f, initialScale, popProgress);
                    break;

                case TransitionStyle.SlideFromRight:
                    float slideRightProgress = EaseOutCubic(normalizedTime);
                    rectTransform.anchoredPosition = Vector2.LerpUnclamped(initialAnchoredPosition + new Vector2(slideOffset, 0f), initialAnchoredPosition, slideRightProgress);
                    break;

                case TransitionStyle.SlideFromBottom:
                    float slideBottomProgress = EaseOutCubic(normalizedTime);
                    rectTransform.anchoredPosition = Vector2.LerpUnclamped(initialAnchoredPosition + new Vector2(0f, -slideOffset), initialAnchoredPosition, slideBottomProgress);
                    break;
            }

            yield return null;
        }

        // Snap to exact target values to guarantee precision[cite: 15]
        canvasGroup.alpha = 1f;
        rectTransform.localScale = initialScale;
        rectTransform.anchoredPosition = initialAnchoredPosition;
        activeTransitionRoutine = null;
    }

    private IEnumerator ExecuteHideRoutine()
    {
        float elapsedTime = 0f;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        Vector3 startScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
        Vector2 startPos = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;

        while (elapsedTime < (duration * 0.75f)) // Hide slightly faster than show for snappy UX[cite: 15]
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / (duration * 0.75f));

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalizedTime);

            if (transitionStyle == TransitionStyle.ElasticPop)
            {
                rectTransform.localScale = Vector3.Lerp(startScale, initialScale * 0.8f, EaseInCubic(normalizedTime));
            }
            else if (transitionStyle == TransitionStyle.SlideFromRight)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, initialAnchoredPosition + new Vector2(slideOffset, 0f), EaseInCubic(normalizedTime));
            }
            else if (transitionStyle == TransitionStyle.SlideFromBottom)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, initialAnchoredPosition + new Vector2(0f, -slideOffset), EaseInCubic(normalizedTime));
            }

            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        activeTransitionRoutine = null;
    }

    #region Pure Mathematical Easing Formulas (No Assets Required)[cite: 15]

    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    private float EaseInCubic(float x)
    {
        return x * x * x;
    }

    #endregion
}