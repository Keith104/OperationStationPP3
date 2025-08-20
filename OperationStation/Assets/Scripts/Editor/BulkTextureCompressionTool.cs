#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// Place this file anywhere under an Assets/Editor/ folder
// Menu: Tools → Texture Compression Bulk Tool
public class BulkTextureCompressionTool : EditorWindow
{
    // UI state
    private enum CompressionChoice
    {
        Uncompressed,        // TextureImporterCompression.Uncompressed
        LowQuality,          // TextureImporterCompression.CompressedLQ
        NormalQuality,       // TextureImporterCompression.Compressed
        HighQuality          // TextureImporterCompression.CompressedHQ
    }

    private enum MaxSizeChoice
    {
        LeaveAsIs = 0,
        _32 = 32,
        _64 = 64,
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192
    }

    private CompressionChoice compressionChoice = CompressionChoice.HighQuality;
    private MaxSizeChoice maxSizeChoice = MaxSizeChoice.LeaveAsIs;
    private bool useCrunch = true;
    private int crunchQuality = 100; // 0–100 ("Compressor Quality" slider)
    private bool skipNormalMaps = true; // optional safety

    // Where to apply
    private bool applyDefault = true;      // base (non-platform) importer settings
    private bool applyStandalone = true;
    private bool applyAndroid = true;
    private bool applyiOS = true;          // "iPhone" in Unity API
    private bool applyWebGL = false;

    private string status = string.Empty;

    [MenuItem("Tools/Texture Compression Bulk Tool")]
    public static void ShowWindow()
    {
        var win = GetWindow<BulkTextureCompressionTool>(true, "Bulk Texture Compression", true);
        win.minSize = new Vector2(520, 460);
        win.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(6);
        EditorGUILayout.LabelField("Bulk set texture Compression, Crunch, and Quality", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This scans the entire project (t:Texture2D). Consider committing or backing up first.", MessageType.Info);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            // Compression dropdown like the Texture Importer UI
            compressionChoice = (CompressionChoice)EditorGUILayout.EnumPopup(new GUIContent("Compression", "Matches the Texture Importer 'Compression' dropdown"), compressionChoice);

            // Max Size (optional)
            maxSizeChoice = (MaxSizeChoice)EditorGUILayout.EnumPopup(new GUIContent("Max Size", "Optional. Choose a max size to set, or Leave As Is to not change it."), maxSizeChoice);

            // Crunch
            using (new EditorGUI.DisabledScope(compressionChoice == CompressionChoice.Uncompressed))
            {
                useCrunch = EditorGUILayout.Toggle(new GUIContent("Use Crunch Compression", "Enable crunch where supported (ignored if Uncompressed)"), useCrunch);
                crunchQuality = EditorGUILayout.IntSlider(new GUIContent("Compressor Quality", "0 (smallest/lowest) — 100 (largest/highest)"), crunchQuality, 0, 100);
            }

            skipNormalMaps = EditorGUILayout.Toggle(new GUIContent("Skip Normal Maps", "Avoid modifying textures marked as Normal Map."), skipNormalMaps);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Apply To:", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            applyDefault = EditorGUILayout.ToggleLeft(new GUIContent("Default Importer Settings", "Affects the base importer (all platforms unless overridden)."), applyDefault);
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Platform Overrides:");
            using (new EditorGUI.IndentLevelScope())
            {
                applyStandalone = EditorGUILayout.ToggleLeft("Standalone", applyStandalone);
                applyAndroid = EditorGUILayout.ToggleLeft("Android", applyAndroid);
                applyiOS = EditorGUILayout.ToggleLeft("iPhone", applyiOS); // Unity uses "iPhone" as the name
                applyWebGL = EditorGUILayout.ToggleLeft("WebGL", applyWebGL);
            }
        }

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Count Textures"))
            {
                int count = AssetDatabase.FindAssets("t:Texture2D").Length;
                status = $"Found {count} Texture2D assets.";
            }

            if (GUILayout.Button("Apply Now"))
            {
                ApplyToAllTextures();
            }
        }

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.HelpBox(status, MessageType.None);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Notes:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "• Platform overrides are written only for the boxes you check; others are untouched.\n" +
            "• The tool only modifies: Compression, Use Crunch, Compressor Quality, and (optionally) Max Size.\n" +
            "• Crunch is ignored when Compression is Uncompressed or unsupported by platform/format.\n" +
            "• Uncheck 'Skip Normal Maps' if you intentionally want to change normals.",
            MessageType.Info);
    }

    private void ApplyToAllTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        if (guids == null || guids.Length == 0)
        {
            status = "No Texture2D assets found.";
            return;
        }

        int changedCount = 0;
        int processed = 0;
        bool canceled = false;

        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                float progress = (float)i / Mathf.Max(1, guids.Length);
                if (EditorUtility.DisplayCancelableProgressBar("Bulk Texture Compression", $"Processing {System.IO.Path.GetFileName(path)}", progress))
                {
                    canceled = true;
                    break;
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                if (skipNormalMaps && importer.textureType == TextureImporterType.NormalMap)
                {
                    processed++;
                    continue;
                }

                bool changed = ApplySettings(importer);
                if (changed)
                {
                    changedCount++;
                    importer.SaveAndReimport();
                }
                processed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        if (canceled)
        {
            status = $"Canceled. Processed {processed}/{guids.Length}. Modified {changedCount}.";
        }
        else
        {
            status = $"Done. Processed {processed} textures. Modified {changedCount}.";
        }
    }

    private bool ApplySettings(TextureImporter importer)
    {
        bool changed = false;

        // Map our dropdown to TextureImporterCompression
        TextureImporterCompression targetCompression = TextureImporterCompression.Uncompressed;
        switch (compressionChoice)
        {
            case CompressionChoice.Uncompressed:
                targetCompression = TextureImporterCompression.Uncompressed;
                break;
            case CompressionChoice.LowQuality:
                targetCompression = TextureImporterCompression.CompressedLQ;
                break;
            case CompressionChoice.NormalQuality:
                targetCompression = TextureImporterCompression.Compressed;
                break;
            case CompressionChoice.HighQuality:
                targetCompression = TextureImporterCompression.CompressedHQ;
                break;
        }

        // Apply to default importer settings
        if (applyDefault)
        {
            if (importer.textureCompression != targetCompression)
            {
                importer.textureCompression = targetCompression;
                changed = true;
            }

            bool allowCrunch = targetCompression != TextureImporterCompression.Uncompressed;
            bool desiredCrunch = allowCrunch && useCrunch;

            if (importer.crunchedCompression != desiredCrunch)
            {
                importer.crunchedCompression = desiredCrunch;
                changed = true;
            }

            if (importer.compressionQuality != crunchQuality)
            {
                importer.compressionQuality = crunchQuality;
                changed = true;
            }

            int targetMaxSize = (int)maxSizeChoice;
            if (targetMaxSize > 0 && importer.maxTextureSize != targetMaxSize)
            {
                importer.maxTextureSize = targetMaxSize;
                changed = true;
            }
        }

        // Apply to platform overrides
        foreach (var platform in GetSelectedPlatforms())
        {
            var pts = importer.GetPlatformTextureSettings(platform);
            bool localChanged = false;

            if (!pts.overridden)
            {
                pts.overridden = true;
                localChanged = true;
            }

#if UNITY_2018_1_OR_NEWER
            if (pts.textureCompression != targetCompression)
            {
                pts.textureCompression = targetCompression;
                localChanged = true;
            }
#endif
            bool allowCrunch = targetCompression != TextureImporterCompression.Uncompressed;
            bool desiredCrunch = allowCrunch && useCrunch;
            if (pts.crunchedCompression != desiredCrunch)
            {
                pts.crunchedCompression = desiredCrunch;
                localChanged = true;
            }

            if (pts.compressionQuality != crunchQuality)
            {
                pts.compressionQuality = crunchQuality;
                localChanged = true;
            }

            int targetMaxSize = (int)maxSizeChoice;
            if (targetMaxSize > 0 && pts.maxTextureSize != targetMaxSize)
            {
                pts.maxTextureSize = targetMaxSize;
                localChanged = true;
            }

            if (localChanged)
            {
                importer.SetPlatformTextureSettings(pts);
                changed = true;
            }
        }

        // DO NOT modify any other importer fields (Format, Resize Algorithm, sRGB, etc.)
        return changed;
    }

    private IEnumerable<string> GetSelectedPlatforms()
    {
        if (applyStandalone) yield return "Standalone";
        if (applyAndroid) yield return "Android";
        if (applyiOS) yield return "iPhone"; // Unity platform name is "iPhone"
        if (applyWebGL) yield return "WebGL";
    }
}
#endif