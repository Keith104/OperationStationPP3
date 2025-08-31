using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SwapShadersPerPlatform : MonoBehaviour
{
    public List<Material> materials = new List<Material>();

    [Header("Shader names")]
    public string urpLitShaderName = "Universal Render Pipeline/Lit";
    public string psxShaderName = "PSX/AffineUnlit";

    public enum VariantHandling { AffectRootParent, CloneAndOperate }
    public VariantHandling variantHandling = VariantHandling.AffectRootParent;

    [Header("Post FX Pixelation Material (the one in your screenshot)")]
    public Material pixelationMaterial;
    public string pixelScaleProperty = "_PixelScale";
    public float pixelScale_Standalone = 8f;
    public float pixelScale_WebGL = 8f;

    void Awake()
    {
        if (materials != null && materials.Count > 0)
        {
#if UNITY_WEBGL
            foreach (var mat in materials) if (mat) ToURPLit(GetTarget(mat));
#else
            foreach (var mat in materials) if (mat) ToPSX(GetTarget(mat));
#endif
        }

        ApplyPixelScalePerPlatform();
    }

    void ApplyPixelScalePerPlatform()
    {
        if (!pixelationMaterial) return;
        if (!pixelationMaterial.HasProperty(pixelScaleProperty)) return;

#if UNITY_WEBGL
        float ps = pixelScale_WebGL;
#elif UNITY_ANDROID
        float ps = pixelScale_Android;
#elif UNITY_IOS
        float ps = pixelScale_iOS;
#else
        float ps = pixelScale_Standalone;
#endif
        pixelationMaterial.SetFloat(pixelScaleProperty, ps);
    }

    Material GetTarget(Material m)
    {
        if (!m) return null;
#if UNITY_EDITOR
        if (IsVariant(m))
        {
            switch (variantHandling)
            {
                case VariantHandling.AffectRootParent:
                    return GetRootParent(m) ?? m;
                case VariantHandling.CloneAndOperate:
                    var clone = new Material(m);
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
        try { return m && m.isVariant; } catch { return false; }
    }

    static Material GetRootParent(Material m)
    {
        var cur = m;
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

    void ToURPLit(Material m)
    {
        if (!m) return;

        var baseMap = GetTex(m, "_BaseMap") ?? GetTex(m, "_MainTex");
        var baseScale = GetScale(m, "_BaseMap", "_MainTex");
        var baseOff = GetOffset(m, "_BaseMap", "_MainTex");
        var baseCol = GetColor(m, "_BaseColor", "_Color", Color.white);

        bool wantTransparent =
            (Has(m, "_Transparent") && m.GetFloat("_Transparent") > 0.5f) ||
            baseCol.a < 0.999f;

        var lit = Shader.Find(urpLitShaderName);
        if (!lit) return;

        m.shader = lit;
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

        if (wantTransparent)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 1f);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Geometry;
        }
    }

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
        if (!psx) return;

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

    static bool Has(Material m, string p) => m && m.HasProperty(p);
    static Color GetColor(Material m, string p1, string p2, Color d)
        => Has(m, p1) ? m.GetColor(p1) : (Has(m, p2) ? m.GetColor(p2) : d);
    static Texture GetTex(Material m, string p) => Has(m, p) ? m.GetTexture(p) : null;
    static Vector2 GetScale(Material m, string p1, string p2)
        => Has(m, p1) ? m.GetTextureScale(p1) : (Has(m, p2) ? m.GetTextureScale(p2) : Vector2.one);
    static Vector2 GetOffset(Material m, string p1, string p2)
        => Has(m, p1) ? m.GetTextureOffset(p1) : (Has(m, p2) ? m.GetTextureOffset(p2) : Vector2.zero);
}
