using UnityEngine;

[DisallowMultipleComponent]
public class UrathSpin : MonoBehaviour
{
    [SerializeField] Material material;
    [SerializeField] bool skybox;
    [SerializeField] float scrollSpeedX = 0.1f;
    [SerializeField] bool ignoreTimeScale = true;

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    static readonly int RotationId = Shader.PropertyToID("_Rotation");

    int propId = -1;
    float startY, startXNorm, startClock, startRotationDeg;
    Renderer _renderer;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        RebindNow();
    }

    void OnEnable()
    {
        RebindNow();
        startClock = CurrentClock();
    }

    void Update()
    {
        if (skybox)
        {
            if (!material) { RebindNow(); startClock = CurrentClock(); if (!material) return; }
            float t = CurrentClock() - startClock;
            float rot = Mathf.Repeat(startRotationDeg + t * scrollSpeedX, 360f);
            material.SetFloat(RotationId, rot);
            return;
        }

        if (!material || propId == -1)
        {
            RebindNow();
            startClock = CurrentClock();
            if (propId == -1) return;
        }

        float tt = CurrentClock() - startClock;
        float x = Mathf.Repeat(startXNorm + tt * scrollSpeedX, 1f);
        material.SetTextureOffset(propId, new Vector2(x, startY));
    }

    public void RebindNow()
    {
        if (skybox)
        {
            if (!material) material = RenderSettings.skybox;
            if (!material) { enabled = false; return; }

            if (ReferenceEquals(material, RenderSettings.skybox))
            {
                material = new Material(RenderSettings.skybox);
                RenderSettings.skybox = material;
            }

            if (!material.HasProperty(RotationId))
            {
                Debug.LogWarning($"UrathSpin: Skybox material '{material.name}' has no _Rotation property.");
                enabled = false; return;
            }

            startRotationDeg = material.GetFloat(RotationId);
            propId = -1;
            return;
        }

        if (_renderer) material = _renderer.material;
        if (!material) { enabled = false; return; }

        propId = material.HasProperty(BaseMapId) ? BaseMapId
              : material.HasProperty(MainTexId) ? MainTexId
              : -1;

        if (propId == -1)
        {
            Debug.LogWarning($"UrathSpin: Material '{material.name}' has no _BaseMap or _MainTex.");
            enabled = false; return;
        }

        var o = material.GetTextureOffset(propId);
        startY = o.y;
        startXNorm = Mathf.Repeat(o.x, 1f);
        material.SetTextureOffset(propId, new Vector2(startXNorm, startY));

        var tex = material.GetTexture(propId);
        if (tex && tex.wrapMode != TextureWrapMode.Repeat)
            Debug.LogWarning($"UrathSpin: Texture '{tex.name}' wrap is {tex.wrapMode}. Set to Repeat.");
    }

    float CurrentClock() => ignoreTimeScale ? Time.unscaledTime : Time.time;
}
