using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SwapShadersPerPlatform : MonoBehaviour
{
    [Tooltip("List the *material assets* (Materials or Material Variants).")]
    public List<Material> materials = new List<Material>();

    [Header("Shader names")]
    public string urpLitShaderName = "Universal Render Pipeline/Lit";
    public string psxShaderName = "PSX/AffineUnlit";

    public enum VariantHandling
    {
        /// <summary>Find the root parent of a variant and operate there. Affects all children.</summary>
        AffectRootParent,
        /// <summary>Make a runtime clone of the (variant) material, break inheritance, operate on that clone.</summary>
        CloneAndOperate
    }

    [Header("Material Variant handling (Editor-only semantics)")]
    public VariantHandling variantHandling = VariantHandling.AffectRootParent;

    void Awake()
    {
        if (materials == null || materials.Count == 0) return;

#if UNITY_WEBGL
        foreach (var mat in materials) if (mat) ToURPLit(GetTarget(mat));
#else
        foreach (var mat in materials) if (mat) ToPSX(GetTarget(mat));
#endif
    }

    // ----- Variant routing -----

    Material GetTarget(Material m)
    {
        if (!m) return null;

#if UNITY_EDITOR
        // In the Editor, variants expose parent/isVariant. In Player builds they are flattened already.
        if (IsVariant(m))
        {
            switch (variantHandling)
            {
                case VariantHandling.AffectRootParent:
                    return GetRootParent(m) ?? m; // fallback to m just in case
                case VariantHandling.CloneAndOperate:
                    // Break inheritance by cloning the variant into a plain Material that keeps current values.
                    // IMPORTANT: this clone is not saved as an asset (runtime only); it will affect objects using this material reference only if
                    // those objects reference this instance directly (common in scene materials). For asset-level swaps, use Editor tooling.
                    var clone = new Material(m); // copies shader + values (including overrides as currently resolved)
                    // Also copy keywords & render settings that Unity doesn’t always copy consistently across pipelines.
                    clone.CopyMatchingPropertiesFromMaterial(m);
                    return clone;
            }
        }
#endif

        return m;
    }

#if UNITY_EDITOR
    static bool IsVariant(Material m)
    {
        // Material.isVariant is Editor-only. If running older Unity without the API, wrap in try/catch.
        try { return m && m.isVariant; } catch { return false; }
    }

    static Material GetRootParent(Material m)
    {
        var cur = m;
        // Material.parent is Editor-only; at runtime it is null and the hierarchy is flattened.
        // Walk up to the root so we swap once and all children follow.
        int guard = 64;
        while (cur != null && guard-- > 0)
        {
            var p = cur.parent;
            if (p == null) break;
            cur = p;
        }
        return cur;
    }
#endif

    // ----- URP/Lit conversion -----

    void ToURPLit(Material m)
    {
        if (!m) return;

        // Cache source properties (handles PSX or already-Lit)
        var baseMap = GetTex(m, "_BaseMap") ?? GetTex(m, "_MainTex");
        var baseScale = GetScale(m, "_BaseMap", "_MainTex");
        var baseOff = GetOffset(m, "_BaseMap", "_MainTex");
        var baseCol = GetColor(m, "_BaseColor", "_Color", Color.white);

        bool wantTransparent =
            (Has(m, "_Transparent") && m.GetFloat("_Transparent") > 0.5f) ||
            baseCol.a < 0.999f;

        var lit = Shader.Find(urpLitShaderName);
        if (!lit) { Debug.LogWarning("URP/Lit shader not found."); return; }

        m.shader = lit; // swap in-place (root, variant-clone, or normal material)

        // Re-apply base color & map using URP property names
        m.SetColor("_BaseColor", baseCol);

        if (baseMap)
        {
            m.SetTexture("_BaseMap", baseMap);
            m.SetTextureScale("_BaseMap", baseScale);
            m.SetTextureOffset("_BaseMap", baseOff);
            m.EnableKeyword("_BASEMAP");
        }
        else
        {
            m.DisableKeyword("_BASEMAP");
        }

        // Surface type / blending
        if (wantTransparent)
        {
            m.SetFloat("_Surface", 1f); // Transparent
            m.SetFloat("_Blend", 0f);   // Alpha
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            m.SetFloat("_Surface", 0f); // Opaque
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 1f);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Geometry;
        }
    }

    // ----- PSX conversion -----

    void ToPSX(Material m)
    {
        if (!m) return;

        var baseMap = GetTex(m, "_BaseMap") ?? GetTex(m, "_MainTex");
        var baseScale = GetScale(m, "_BaseMap", "_MainTex");
        var baseOff = GetOffset(m, "_BaseMap", "_MainTex");
        var baseCol = GetColor(m, "_BaseColor", "_Color", Color.white);

        var wasTransparent =
            (Has(m, "_Surface") && Mathf.Approximately(m.GetFloat("_Surface"), 1f)) ||
            baseCol.a < 0.999f;

        var psx = Shader.Find(psxShaderName);
        if (!psx) { Debug.LogWarning("PSX shader not found."); return; }

        m.shader = psx;

        if (Has(m, "_BaseColor")) m.SetColor("_BaseColor", baseCol);
        if (Has(m, "_BaseMap") && baseMap)
        {
            m.SetTexture("_BaseMap", baseMap);
            m.SetTextureScale("_BaseMap", baseScale);
            m.SetTextureOffset("_BaseMap", baseOff);
        }
        if (Has(m, "_Transparent")) m.SetFloat("_Transparent", wasTransparent ? 1f : 0f);
    }

    // ----- Small utility methods -----

    static bool Has(Material m, string p) => m && m.HasProperty(p);
    static Color GetColor(Material m, string p1, string p2, Color d)
        => Has(m, p1) ? m.GetColor(p1) : (Has(m, p2) ? m.GetColor(p2) : d);
    static Texture GetTex(Material m, string p) => Has(m, p) ? m.GetTexture(p) : null;
    static Vector2 GetScale(Material m, string p1, string p2)
        => Has(m, p1) ? m.GetTextureScale(p1) : (Has(m, p2) ? m.GetTextureScale(p2) : Vector2.one);
    static Vector2 GetOffset(Material m, string p1, string p2)
        => Has(m, p1) ? m.GetTextureOffset(p1) : (Has(m, p2) ? m.GetTextureOffset(p2) : Vector2.zero);
}
