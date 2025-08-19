using UnityEngine;
using UnityEditor;
using System.Linq;

public static class SwapLitToPSX
{
    [MenuItem("Tools/PSX/Convert Selected Lit Materials")]
    public static void ConvertSelected()
    {
        var psx = Shader.Find("PSX/AffineLitLike");
        if (!psx) { Debug.LogError("Shader not found: PSX/AffineLitLike"); return; }

        foreach (var obj in Selection.objects)
        {
            var mat = obj as Material;
            if (!mat) continue;
            if (mat.shader == psx) continue; // already converted

            // Only convert standard URP/Lit materials (optional: remove this check to force)
            // if (mat.shader.name != "Universal Render Pipeline/Lit") continue;

            // Swap shader. Because property names match URP/Lit, Unity preserves values.
            mat.shader = psx;

            // Ensure render states match prior settings (URP Lit GUI relies on these)
            // (Nothing else needed; LitShader GUI will manage keywords when the inspector touches it)
            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Converted selected materials to PSX/AffineLitLike.");
    }
}
