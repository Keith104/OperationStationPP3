using UnityEngine;

[DisallowMultipleComponent]
public class UrathSpin : MonoBehaviour
{
    [SerializeField] Material material;          // Optional override
    [SerializeField] float scrollSpeedX = 0.1f;
    [SerializeField] bool ignoreTimeScale = true;

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    int propId = -1;
    float startY, startXNorm, startClock;
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
        if (!material || propId == -1)
        {   // material changed at runtime? rebind
            RebindNow();
            startClock = CurrentClock();
            if (propId == -1) return;
        }

        float t = CurrentClock() - startClock;
        float x = Mathf.Repeat(startXNorm + t * scrollSpeedX, 1f);
        material.SetTextureOffset(propId, new Vector2(x, startY));
    }

    public void RebindNow()
    {
        if (_renderer)
        {
            // Always prefer the renderer’s current instance material
            material = _renderer.material;  // creates/uses instance for safe property edits
        }
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
