using UnityEngine;

[DisallowMultipleComponent]
public class UrathSpin : MonoBehaviour
{
    [SerializeField] Material material;
    [SerializeField] float scrollSpeedX = 0.1f;
    [SerializeField] bool ignoreTimeScale = true; // use unscaled time so it still spins in menus/pauses

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    int propId = -1;
    float startY;
    float startXNorm;
    float startClock;

    Renderer _renderer;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        TryBindMaterial();
    }

    void OnEnable()
    {
        // Scene changes / reactivation: re-establish baselines so it resumes cleanly
        TryBindMaterial();
        startClock = CurrentClock();
    }

    void Update()
    {
        if (material == null || propId == -1)
        {
            // If something hot-swapped the material/shader, try to rebind on the fly
            if (!TryBindMaterial()) return;
            startClock = CurrentClock();
        }

        float t = CurrentClock() - startClock;
        float x = Mathf.Repeat(startXNorm + t * scrollSpeedX, 1f);
        material.SetTextureOffset(propId, new Vector2(x, startY));
    }

    bool TryBindMaterial()
    {
        if (!material)
        {
            if (_renderer) material = _renderer.material; // instance so we can safely change offsets
            if (!material) { enabled = false; return false; }
        }

        propId = material.HasProperty(BaseMapId) ? BaseMapId
              : material.HasProperty(MainTexId) ? MainTexId
              : -1;

        if (propId == -1)
        {
            Debug.LogWarning($"UrathSpin: Material '{material.name}' has no _BaseMap or _MainTex.");
            enabled = false;
            return false;
        }

        // Normalize existing offset once so we never accumulate huge values
        var o = material.GetTextureOffset(propId);
        startY = o.y;
        startXNorm = Mathf.Repeat(o.x, 1f);
        material.SetTextureOffset(propId, new Vector2(startXNorm, startY));

        // Friendly wrap warning
        var tex = material.GetTexture(propId);
        if (tex && tex.wrapMode != TextureWrapMode.Repeat)
            Debug.LogWarning($"UrathSpin: Texture '{tex.name}' wrap mode is {tex.wrapMode}. Set it to Repeat for seamless scrolling.");

        return true;
    }

    float CurrentClock() => ignoreTimeScale ? Time.unscaledTime : Time.time;
}
