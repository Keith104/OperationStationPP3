using UnityEngine;

[DisallowMultipleComponent]
public class UrathSpin : MonoBehaviour
{
    [SerializeField] Material material;
    [SerializeField] float scrollSpeedX;

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    int propId = 1;
    float startY;

    private void Awake()
    {
        if(!material)
        {
            var r = GetComponent<Renderer>();
            if (r) material = r.material;
        }

        if(!material) { enabled = false; return; }

        propId = material.HasProperty(BaseMapId) ? BaseMapId : material.HasProperty(MainTexId) ? MainTexId : -1;

        if(propId == -1)
        {
            Debug.LogWarning($"UrathSpin: Material '{material.name}' has no _BaseMap or _MainTex.");
            enabled = false;
            return;
        }

        // Cache the starting Y offset and normalize any huge X offset once
        var o = material.GetTextureOffset(propId);
        startY = o.y;
        material.SetTextureOffset(propId, new Vector2((Mathf.Repeat(o.x, 1f)), startY));
        
        // Warn if texture wont tile
        var tex = material.GetTexture(propId);
        if (tex && tex.wrapMode != TextureWrapMode.Repeat)
            Debug.LogWarning($"UrathSpin: Texture '{tex.name}' wrap mode is {tex.wrapMode}. Set it to Repeat for seamless scrolling");
    }

    private void Update()
    {
        // Using Time.tim so the value never grows and precision never degrades
        float x = Mathf.Repeat(Time.time * scrollSpeedX, 1f);
        material.SetTextureOffset(propId, new Vector2(x, startY));
    }

}
