using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SwapShadersPerPlatform : MonoBehaviour
{
    [Tooltip("List the *material assets* to flip between PSX and URP/Lit.")]
    public List<Material> materials = new List<Material>();

    [Header("Shader names")]
    public string urpLitShaderName = "Universal Render Pipeline/Lit";
    public string psxShaderName = "PSX/AffineUnlit";

    void Awake()
    {
        if (materials == null || materials.Count == 0) return;

#if UNITY_WEBGL
        foreach (var mat in materials) if (mat) ToURPLit(mat);
#else
        foreach (var mat in materials) if (mat) ToPSX(mat);
#endif
    }

    // ----- Helpers -----

    void ToURPLit(Material m)
    {
        // Cache source properties (handles your PSX mat or already-Lit mat)
        var baseMap = GetTex(m, "_BaseMap") ?? GetTex(m, "_MainTex");
        var baseScale = GetScale(m, "_BaseMap", "_MainTex");
        var baseOff = GetOffset(m, "_BaseMap", "_MainTex");
        var baseCol = GetColor(m, "_BaseColor", "_Color", Color.white);

        bool wantTransparent =
            (Has(m, "_Transparent") && m.GetFloat("_Transparent") > 0.5f) ||
            baseCol.a < 0.999f;

        var lit = Shader.Find(urpLitShaderName);
        if (!lit) { Debug.LogWarning("URP/Lit shader not found."); return; }

        m.shader = lit; // switch in-place. :contentReference[oaicite:4]{index=4}

        // Re-apply base color & map using URP property names
        m.SetColor("_BaseColor", baseCol);

        if (baseMap)
        {
            m.SetTexture("_BaseMap", baseMap);
            m.SetTextureScale("/_BaseMap".Substring(1), baseScale);  // "_BaseMap" → property OK, but keep explicit next lines:
            m.SetTextureScale("_BaseMap", baseScale);
            m.SetTextureOffset("_BaseMap", baseOff);
            m.EnableKeyword("_BASEMAP"); // ensure variant that samples albedo is used. :contentReference[oaicite:5]{index=5}
        }
        else
        {
            m.DisableKeyword("_BASEMAP");
        }

        // URP "Surface Type: Transparent" settings via properties/keywords
        if (wantTransparent)
        {
            m.SetFloat("_Surface", 1f); // Transparent
            m.SetFloat("_Blend", 0f); // Alpha
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

    void ToPSX(Material m)
    {
        var baseMap = GetTex(m, "_BaseMap") ?? GetTex(m, "_MainTex");
        var baseScale = GetScale(m, "_BaseMap", "_MainTex");
        var baseOff = GetOffset(m, "_BaseMap", "_MainTex");
        var baseCol = GetColor(m, "_BaseColor", "_Color", Color.white);
        var wasTransparent = (Has(m, "_Surface") && Mathf.Approximately(m.GetFloat("_Surface"), 1f))
                             || baseCol.a < 0.999f;

        var psx = Shader.Find(psxShaderName);
        if (!psx) { Debug.LogWarning("PSX shader not found."); return; }

        m.shader = psx;

        // Your PSX shader used _BaseMap/_BaseColor and an optional _Transparent toggle.
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
