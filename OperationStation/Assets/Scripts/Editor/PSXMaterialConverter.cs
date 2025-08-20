#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class PSXMaterialConverter : EditorWindow
{
    // ---- Defaults / UI state ----
    const string kDefaultShader = "PSX/AffineUnlit";
    string targetShaderName = kDefaultShader;

    bool inferTransparency = true;      // detect from renderQueue / known props
    bool copyBaseMap = true;
    bool copyBaseColor = true;
    bool copyCutoff = true;
    bool copyTilingOffset = true;
    float fallbackCutoff = 0.5f;

    [MenuItem("Tools/PSX/Material Converter")]
    public static void ShowWindow()
    {
        var w = GetWindow<PSXMaterialConverter>();
        w.titleContent = new GUIContent("PSX Material Converter");
        w.minSize = new Vector2(380, 320);
        w.Show();
    }

    // Quick actions without opening the window
    [MenuItem("Tools/PSX/Convert Selected Materials")]
    public static void ConvertSelectedMenu() => ConvertMaterials(GatherSelectedMaterials(), kDefaultShader, true, true, true, true, 0.5f, true);

    [MenuItem("Tools/PSX/Convert All Materials In Project")]
    public static void ConvertAllInProjectMenu()
    {
        var guids = AssetDatabase.FindAssets("t:Material");
        var all = guids.Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).Where(m => m != null).ToList();
        ConvertMaterials(all, kDefaultShader, true, true, true, true, 0.5f, true);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Target Shader", EditorStyles.boldLabel);
        targetShaderName = EditorGUILayout.TextField(new GUIContent("Shader Name"), targetShaderName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Copy Inputs", EditorStyles.boldLabel);
        copyBaseMap = EditorGUILayout.Toggle(new GUIContent("Base Map"), copyBaseMap);
        copyBaseColor = EditorGUILayout.Toggle(new GUIContent("Base Color"), copyBaseColor);
        copyCutoff = EditorGUILayout.Toggle(new GUIContent("Alpha Cutoff"), copyCutoff);
        copyTilingOffset = EditorGUILayout.Toggle(new GUIContent("Tiling & Offset"), copyTilingOffset);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Surface Detection", EditorStyles.boldLabel);
        inferTransparency = EditorGUILayout.Toggle(new GUIContent("Infer Transparent from source"), inferTransparency);
        fallbackCutoff = EditorGUILayout.Slider(new GUIContent("Fallback Cutoff"), fallbackCutoff, 0f, 1f);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Convert Selected"))
                ConvertMaterials(GatherSelectedMaterials(), targetShaderName, copyBaseMap, copyBaseColor, copyCutoff, copyTilingOffset, fallbackCutoff, inferTransparency);

            if (GUILayout.Button("Convert All In Project"))
            {
                var guids = AssetDatabase.FindAssets("t:Material");
                var all = guids.Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).Where(m => m != null).ToList();
                ConvertMaterials(all, targetShaderName, copyBaseMap, copyBaseColor, copyCutoff, copyTilingOffset, fallbackCutoff, inferTransparency);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Converts to a single PSX/AffineUnlit shader and sets correct queue/tag/blend/zwrite for opaque or transparent. Selection can include scene objects or material assets.", MessageType.Info);
    }

    // ---- Core conversion ----
    static void ConvertMaterials(List<Material> mats, string shaderName,
                                 bool copyBaseMap, bool copyBaseColor, bool copyCutoff, bool copyTilingOffset,
                                 float fallbackCutoff, bool inferTransparency)
    {
        if (mats == null || mats.Count == 0)
        {
            EditorUtility.DisplayDialog("PSX Converter", "No materials found in selection.", "OK");
            return;
        }

        Shader target = Shader.Find(shaderName);
        if (target == null)
        {
            EditorUtility.DisplayDialog("PSX Converter", $"Shader '{shaderName}' not found.", "OK");
            return;
        }

        Undo.RecordObjects(mats.ToArray(), "Convert to PSX/AffineUnlit");

        int converted = 0;
        foreach (var m in mats)
        {
            if (m == null) continue;

            // Cache common props from the source
            Texture srcBaseMap = GetFirstTex(m, "_BaseMap", "_MainTex");
            Color srcColor = GetFirstColor(m, "_BaseColor", "_Color", Color.white);
            float srcCutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : fallbackCutoff;

            bool wasTransparent = false;
            if (inferTransparency)
            {
                wasTransparent =
                    m.renderQueue >= (int)RenderQueue.Transparent - 10 ||                         // transparent-ish queue
                    (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) ||               // URP Lit (0=Opaque, 1=Transparent)
                    (m.HasProperty("_Mode") && m.GetFloat("_Mode") >= 2.0f);                      // Built-in Standard: 2/3 = Fade/Transparent
            }

            // Assign shader
            m.shader = target;

            // Reapply inputs
            if (copyBaseMap && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", srcBaseMap);
            if (copyBaseColor && m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", srcColor);
            if (copyCutoff && m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", srcCutoff);

            if (copyTilingOffset && srcBaseMap != null && m.HasProperty("_BaseMap"))
            {
                // carry over ST from old material if present
                Vector2 scale = Vector2.one, offset = Vector2.zero;
                try { scale = m.GetTextureScale("_BaseMap"); } catch { }
                try { offset = m.GetTextureOffset("_BaseMap"); } catch { }
                m.SetTextureScale("_BaseMap", scale);
                m.SetTextureOffset("_BaseMap", offset);
            }

            // Set our single-toggle transparency flag if present
            if (m.HasProperty("_Transparent")) m.SetFloat("_Transparent", wasTransparent ? 1f : 0f);

            // Apply render states & tags to match URP expectations
            ApplySurfaceState(m, wasTransparent);

            EditorUtility.SetDirty(m);
            converted++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("PSX Converter", $"Converted {converted} material(s).", "OK");
    }

    static void ApplySurfaceState(Material mat, bool transparent)
    {
        // Flip keyword for other tooling if you want
        if (transparent) mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        else mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

        // Tag + queue so URP sorts/render-groups correctly
        mat.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque"); // official override tag API
        mat.renderQueue = transparent ? (int)RenderQueue.Transparent : (int)RenderQueue.Geometry;

        // Our shader exposes these as ints; set them so the pass reads them
        if (mat.HasProperty("_SrcBlend"))
            mat.SetInt("_SrcBlend", transparent ? (int)BlendMode.SrcAlpha : (int)BlendMode.One);
        if (mat.HasProperty("_DstBlend"))
            mat.SetInt("_DstBlend", transparent ? (int)BlendMode.OneMinusSrcAlpha : (int)BlendMode.Zero);
        if (mat.HasProperty("_ZWrite"))
            mat.SetInt("_ZWrite", transparent ? 0 : 1);
    }

    // ---- Helpers ----
    static Texture GetFirstTex(Material m, params string[] names)
    {
        foreach (var n in names) if (m.HasProperty(n)) { var t = m.GetTexture(n); if (t) return t; }
        return null;
    }
    static Color GetFirstColor(Material m, string n0, string n1, Color fallback)
    {
        if (m.HasProperty(n0)) return m.GetColor(n0);
        if (m.HasProperty(n1)) return m.GetColor(n1);
        return fallback;
    }

    static List<Material> GatherSelectedMaterials()
    {
        var list = new List<Material>();
        foreach (var obj in Selection.objects) // works for assets or scene selection
        {
            if (obj is Material mat) { if (!list.Contains(mat)) list.Add(mat); continue; }
            if (obj is GameObject go)
            {
                var r = go.GetComponent<Renderer>();
                if (r) foreach (var m in r.sharedMaterials) if (m && !list.Contains(m)) list.Add(m);
            }
        }
        return list;
    }
}
#endif
