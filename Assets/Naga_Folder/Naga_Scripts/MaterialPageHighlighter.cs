using System.Collections.Generic;
using UnityEngine;

public class PageMeshHighlightManager : MonoBehaviour
{
    [System.Serializable]
    public class MeshHighlightEntry
    {
        [Tooltip("The target Renderer (MeshRenderer or SkinnedMeshRenderer) to highlight.")]
        [SerializeField] private Renderer meshRenderer;

        [Tooltip("Automatically highlight when this page opens.")]
        [SerializeField] private bool autoHighlightOnPageEnter = false;

        public Renderer MeshRenderer => meshRenderer;
        public bool AutoHighlightOnPageEnter => autoHighlightOnPageEnter;
    }

    [System.Serializable]
    public class PageHighlightConfig
    {
        [Header("Page Index")]
        public int pageIndex = 0;

        [Header("Mesh Renderers")]
        public List<MeshHighlightEntry> meshEntries = new List<MeshHighlightEntry>();
    }

    [Header("Highlight Material")]
    [SerializeField] private Material highlightMaterial;

    [Header("Page Configurations")]
    [SerializeField] private List<PageHighlightConfig> pageConfigs = new List<PageHighlightConfig>();

    private readonly HashSet<Renderer> highlightedRenderers = new HashSet<Renderer>();

    // Tracks renderers that have been disabled so auto-highlight does not re-enable them on revisit
    private readonly HashSet<Renderer> completedRenderers = new HashSet<Renderer>();

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    private void Start()
    {
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

    private void HandlePageChanged(int currentPageIndex)
    {
        ClearAllHighlightsGlobal();

        PageHighlightConfig config = GetConfigByPageIndex(currentPageIndex);

        if (config == null)
            return;

        foreach (MeshHighlightEntry entry in config.meshEntries)
        {
            if (entry.MeshRenderer != null && entry.AutoHighlightOnPageEnter)
            {
                // Only auto-highlight if it hasn't been explicitly disabled/completed previously
                if (!completedRenderers.Contains(entry.MeshRenderer))
                {
                    ApplyHighlightMaterial(entry.MeshRenderer);
                }
            }
        }
    }

    //==========================================================
    // UNITY EVENT HELPERS
    //==========================================================

    public void EnableElement0ByPageIndex(int pageIndex) => EnableElementHighlight(pageIndex, 0);
    public void DisableElement0ByPageIndex(int pageIndex) => DisableElementHighlight(pageIndex, 0);

    public void EnableElement1ByPageIndex(int pageIndex) => EnableElementHighlight(pageIndex, 1);
    public void DisableElement1ByPageIndex(int pageIndex) => DisableElementHighlight(pageIndex, 1);

    public void EnableElement2ByPageIndex(int pageIndex) => EnableElementHighlight(pageIndex, 2);
    public void DisableElement2ByPageIndex(int pageIndex) => DisableElementHighlight(pageIndex, 2);

    //==========================================================
    // PUBLIC FUNCTIONS
    //==========================================================

    public void EnableElementHighlight(int pageIndex, int elementIndex)
    {
        PageHighlightConfig config = GetConfigByPageIndex(pageIndex);

        if (config == null)
            return;

        if (elementIndex < 0 || elementIndex >= config.meshEntries.Count)
            return;

        Renderer renderer = config.meshEntries[elementIndex].MeshRenderer;

        if (renderer != null)
        {
            // If re-enabled explicitly, unmark from completed memory
            completedRenderers.Remove(renderer);
            ApplyHighlightMaterial(renderer);
        }
    }

    public void DisableElementHighlight(int pageIndex, int elementIndex)
    {
        PageHighlightConfig config = GetConfigByPageIndex(pageIndex);

        if (config == null)
            return;

        if (elementIndex < 0 || elementIndex >= config.meshEntries.Count)
            return;

        Renderer renderer = config.meshEntries[elementIndex].MeshRenderer;

        if (renderer != null)
        {
            // Remember that this renderer has been disabled
            completedRenderers.Add(renderer);
            RemoveHighlightMaterial(renderer);
        }
    }

    public void EnableAllHighlightsForPageIndex(int pageIndex)
    {
        PageHighlightConfig config = GetConfigByPageIndex(pageIndex);

        if (config == null)
            return;

        foreach (MeshHighlightEntry entry in config.meshEntries)
        {
            if (entry.MeshRenderer != null)
            {
                completedRenderers.Remove(entry.MeshRenderer);
                ApplyHighlightMaterial(entry.MeshRenderer);
            }
        }
    }

    public void DisableAllHighlightsForPageIndex(int pageIndex)
    {
        PageHighlightConfig config = GetConfigByPageIndex(pageIndex);

        if (config == null)
            return;

        foreach (MeshHighlightEntry entry in config.meshEntries)
        {
            if (entry.MeshRenderer != null)
            {
                completedRenderers.Add(entry.MeshRenderer);
                RemoveHighlightMaterial(entry.MeshRenderer);
            }
        }
    }

    //==========================================================
    // HIGHLIGHT FUNCTIONS
    //==========================================================

    private void ApplyHighlightMaterial(Renderer renderer)
    {
        if (renderer == null || highlightMaterial == null)
            return;

        Material[] mats = renderer.sharedMaterials;

        // Already highlighted?
        foreach (Material mat in mats)
        {
            if (mat == highlightMaterial)
            {
                highlightedRenderers.Add(renderer);
                return;
            }
        }

        List<Material> newMats = new List<Material>(mats);
        newMats.Add(highlightMaterial);

        renderer.sharedMaterials = newMats.ToArray();
        highlightedRenderers.Add(renderer);
    }

    private void RemoveHighlightMaterial(Renderer renderer)
    {
        if (renderer == null)
            return;

        Material[] mats = renderer.sharedMaterials;

        List<Material> newMats = new List<Material>();

        foreach (Material mat in mats)
        {
            if (mat != highlightMaterial)
                newMats.Add(mat);
        }

        renderer.sharedMaterials = newMats.ToArray();
        highlightedRenderers.Remove(renderer);
    }

    private void ClearAllHighlightsGlobal()
    {
        // 1. Clear currently tracked active highlights
        foreach (Renderer renderer in highlightedRenderers)
        {
            if (renderer == null)
                continue;

            RemoveHighlightMaterialDirect(renderer);
        }
        highlightedRenderers.Clear();

        // 2. Fallback check on all configured renderers to ensure no legacy highlight materials remain on revisit
        foreach (PageHighlightConfig config in pageConfigs)
        {
            foreach (MeshHighlightEntry entry in config.meshEntries)
            {
                if (entry.MeshRenderer != null)
                {
                    RemoveHighlightMaterialDirect(entry.MeshRenderer);
                }
            }
        }
    }

    private void RemoveHighlightMaterialDirect(Renderer renderer)
    {
        if (renderer == null)
            return;

        Material[] mats = renderer.sharedMaterials;
        List<Material> newMats = new List<Material>();

        bool modified = false;
        foreach (Material mat in mats)
        {
            if (mat != highlightMaterial)
            {
                newMats.Add(mat);
            }
            else
            {
                modified = true;
            }
        }

        if (modified)
        {
            renderer.sharedMaterials = newMats.ToArray();
        }
    }

    //==========================================================
    // HELPERS
    //==========================================================

    private PageHighlightConfig GetConfigByPageIndex(int pageIndex)
    {
        foreach (PageHighlightConfig config in pageConfigs)
        {
            if (config.pageIndex == pageIndex)
                return config;
        }

        return null;
    }
}