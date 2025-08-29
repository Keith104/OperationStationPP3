// ChangeCameraRenderer.cs
// Attach to a Camera. Sets the URP renderer by index.

using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class ChangeCameraRenderer : MonoBehaviour
{
    [Tooltip("Index from the URP Asset's Renderer List (Project Settings > Graphics > URP Asset).")]
    [Min(0)]
    public int rendererIndex = 0;

    [Tooltip("Apply every frame (useful if something else might change it). Otherwise applies on load/validate only.")]
    public bool applyEveryFrame = false;

    UniversalAdditionalCameraData camData;

    void Awake() => Cache();
    void OnEnable() => Apply();
#if UNITY_EDITOR
    void OnValidate() { Cache(); Apply(); }
#endif
    void Update() { if (applyEveryFrame) Apply(); }

    void Cache()
    {
        if (camData == null)
            camData = GetComponent<UniversalAdditionalCameraData>();
        if (camData == null)
            camData = gameObject.AddComponent<UniversalAdditionalCameraData>();
    }

    void Apply()
    {
        if (camData == null) return;

        camData.SetRenderer(rendererIndex);
    }
}
