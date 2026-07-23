using System.Collections.Generic;
using UnityEngine;

public class PageMeshHighlightManager : MonoBehaviour
{
    [System.Serializable]
    public class MeshHighlightEntry
    {
        [Tooltip("The target MeshRenderer to highlight.")]
        [SerializeField] private MeshRenderer meshRenderer;

        [Tooltip("Automatically highlight when this page opens.")]
        [SerializeField] private bool autoHighlightOnPageEnter = false;

        public MeshRenderer MeshRenderer => meshRenderer;
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

    private readonly HashSet<MeshRenderer> highlightedRenderers = new HashSet<MeshRenderer>();

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
                ApplyHighlightMaterial(entry.MeshRenderer);
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

        MeshRenderer renderer = config.meshEntries[elementIndex].MeshRenderer;

        if (renderer != null)
            ApplyHighlightMaterial(renderer);
    }

    public void DisableElementHighlight(int pageIndex, int elementIndex)
    {
        PageHighlightConfig config = GetConfigByPageIndex(pageIndex);

        if (config == null)
            return;

        if (elementIndex < 0 || elementIndex >= config.meshEntries.Count)
            return;

        MeshRenderer renderer = config.meshEntries[elementIndex].MeshRenderer;

        if (renderer != null)
            RemoveHighlightMaterial(renderer);
    }

    public void EnableAllHighlightsForPageIndex(int pageIndex)
    {
        PageHighlightConfig config = GetConfigByPageIndex(pageIndex);

        if (config == null)
            return;

        foreach (MeshHighlightEntry entry in config.meshEntries)
        {
            if (entry.MeshRenderer != null)
                ApplyHighlightMaterial(entry.MeshRenderer);
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
                RemoveHighlightMaterial(entry.MeshRenderer);
        }
    }

    //==========================================================
    // HIGHLIGHT FUNCTIONS
    //==========================================================

    private void ApplyHighlightMaterial(MeshRenderer renderer)
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

    private void RemoveHighlightMaterial(MeshRenderer renderer)
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
        foreach (MeshRenderer renderer in highlightedRenderers)
        {
            if (renderer == null)
                continue;

            Material[] mats = renderer.sharedMaterials;

            List<Material> newMats = new List<Material>();

            foreach (Material mat in mats)
            {
                if (mat != highlightMaterial)
                    newMats.Add(mat);
            }

            renderer.sharedMaterials = newMats.ToArray();
        }

        highlightedRenderers.Clear();
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