using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class LiquidFillController : MonoBehaviour
{
    [Header("Increase Settings")]
    [Tooltip("Renderer used when increasing liquid.")]
    [SerializeField] private Renderer increaseRenderer;

    [Tooltip("Material used when increasing liquid.")]
    [SerializeField] private Material increaseMaterial;

    [Tooltip("Active ONLY while filling/increasing liquid.")]
    [SerializeField] private GameObject increaseFlowObject;

    [Tooltip("The local _FillHeight to reach when filling (e.g., 0.15).")]
    [SerializeField] private float targetIncreaseFillHeight = 0.15f;

    [Tooltip("Time in seconds to complete the filling process.")]
    [SerializeField] private float increaseDuration = 2.0f;

    [Header("Decrease Settings")]
    [Tooltip("Renderer used when decreasing liquid.")]
    [SerializeField] private Renderer decreaseRenderer;

    [Tooltip("Material used when decreasing liquid.")]
    [SerializeField] private Material decreaseMaterial;

    [Tooltip("Active ONLY while draining/decreasing liquid.")]
    [SerializeField] private GameObject decreaseFlowObject;

    [Tooltip("The local _FillHeight to reach when draining (e.g., 0.0 or -0.05).")]
    [SerializeField] private float targetDecreaseFillHeight = 0.0f;

    [Tooltip("Time in seconds to complete the draining process.")]
    [SerializeField] private float decreaseDuration = 2.0f;

    [Header("Completion Event")]
    [SerializeField] private UnityEvent onFillCompleted;

    // Shader Property Hash
    private static readonly int FillHeightProperty = Shader.PropertyToID("_FillHeight");

    private Coroutine fillRoutine;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (increaseFlowObject != null) increaseFlowObject.SetActive(false);
        if (decreaseFlowObject != null) decreaseFlowObject.SetActive(false);
    }

    // =========================================================================
    // SINGLE COMBINED PUBLIC API FUNCTION
    // =========================================================================

    /// <summary>
    /// Single function to control both actions.
    /// Pass TRUE to Increase liquid level.
    /// Pass FALSE to Decrease liquid level.
    /// </summary>
    /// <param name="isIncreasing">True = Fill / Increase, False = Drain / Decrease</param>
    public void StartFill(bool isIncreasing)
    {
        Renderer targetRenderer = isIncreasing ? increaseRenderer : decreaseRenderer;
        Material targetMaterial = isIncreasing ? increaseMaterial : decreaseMaterial;
        GameObject flowObject = isIncreasing ? increaseFlowObject : decreaseFlowObject;
        float targetHeight = isIncreasing ? targetIncreaseFillHeight : targetDecreaseFillHeight;
        float duration = isIncreasing ? increaseDuration : decreaseDuration;

        SetupAndAnimate(targetRenderer, targetMaterial, flowObject, targetHeight, duration);
    }

    // =========================================================================
    // CORE LOGIC
    // =========================================================================

    private void SetupAndAnimate(Renderer targetRenderer, Material targetMaterial, GameObject flowObject, float targetHeight, float duration)
    {
        if (targetRenderer == null) return;

        if (targetMaterial != null)
        {
            targetRenderer.material = targetMaterial;
        }

        if (fillRoutine != null)
            StopCoroutine(fillRoutine);

        fillRoutine = StartCoroutine(AnimateFill(targetRenderer, flowObject, targetHeight, duration));
    }

    private IEnumerator AnimateFill(Renderer activeRenderer, GameObject flowObject, float targetHeight, float duration)
    {
        float startHeight = GetCurrentFillHeight(activeRenderer);
        float elapsedTime = 0f;

        // Turn on the specific flow object while filling/draining
        if (flowObject != null)
            flowObject.SetActive(true);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            float currentHeight = Mathf.Lerp(startHeight, targetHeight, Mathf.SmoothStep(0f, 1f, t));
            SetFillHeight(activeRenderer, currentHeight);

            yield return null;
        }

        SetFillHeight(activeRenderer, targetHeight);

        // Turn off flow object when complete
        if (flowObject != null)
            flowObject.SetActive(false);

        fillRoutine = null;

        // Trigger completion event
        onFillCompleted?.Invoke();
    }

    // =========================================================================
    // MATERIAL PROPERTY HELPERS
    // =========================================================================

    private float GetCurrentFillHeight(Renderer rend)
    {
        if (rend == null) return 0f;

        rend.GetPropertyBlock(propertyBlock);

        if (rend.HasPropertyBlock() && propertyBlock.GetFloat(FillHeightProperty) != 0f)
        {
            return propertyBlock.GetFloat(FillHeightProperty);
        }

        return rend.sharedMaterial != null
            ? rend.sharedMaterial.GetFloat(FillHeightProperty)
            : 0f;
    }

    private void SetFillHeight(Renderer rend, float value)
    {
        if (rend == null) return;

        rend.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(FillHeightProperty, value);
        rend.SetPropertyBlock(propertyBlock);
    }
}