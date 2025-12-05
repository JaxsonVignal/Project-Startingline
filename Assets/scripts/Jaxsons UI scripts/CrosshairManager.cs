using UnityEngine;
using UnityEngine.UI;

public class CrosshairManager : MonoBehaviour
{
    public static CrosshairManager Instance { get; private set; }

    [Header("Crosshair UI Elements")]
    public Image defaultCrosshair;
    public Image scopeReticle;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Make sure both UI elements start properly
        if (defaultCrosshair != null)
            defaultCrosshair.enabled = true;

        if (scopeReticle != null)
            scopeReticle.enabled = false;
    }

    public void ShowDefaultCrosshair()
    {
        if (defaultCrosshair != null)
            defaultCrosshair.enabled = true;

        if (scopeReticle != null)
            scopeReticle.enabled = false;
    }

    public void ShowScopeReticle(Sprite reticleSprite, float scale, Color color)
    {
        if (scopeReticle != null && reticleSprite != null)
        {
            scopeReticle.sprite = reticleSprite;
            scopeReticle.color = color;
            scopeReticle.transform.localScale = Vector3.one * scale;
            scopeReticle.enabled = true;
        }

        if (defaultCrosshair != null)
            defaultCrosshair.enabled = false;
    }

    public void HideAllCrosshairs()
    {
        if (defaultCrosshair != null)
            defaultCrosshair.enabled = false;

        if (scopeReticle != null)
            scopeReticle.enabled = false;
    }
}