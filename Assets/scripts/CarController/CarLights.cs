using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarLights : MonoBehaviour
{
    public enum Side
    {
        Front,
        Back
    }

    [System.Serializable]
    public struct Light
    {
        public GameObject lightObj;
        public Material lightMat;
        public Side side;
    }

    [Header("Light States")]
    public bool isFrontLightOn;    // controlled by key (L)
    public bool isBackLightOn;     // used by CarController to indicate braking when set externally

    [Header("Light Colors")]
    public Color frontLightOnColor;
    public Color frontLightOffColor;
    public Color backLightOnColor;   // dim tail light color (headlights on)
    public Color backLightOffColor;
    public Color brakeLightColor;    // bright color for braking

    [Header("Lights")]
    public List<Light> lights;

    [Header("Controls")]
    public KeyCode toggleKey = KeyCode.L; // key to toggle front lights

    // internal state to track braking (derived from isBackLightOn when CarController calls OperateBackLights)
    private bool isBraking = false;

    void Start()
    {
        isFrontLightOn = false;
        isBackLightOn = false;
        isBraking = false;
        UpdateLights();
    }

    void Update()
    {
        // Toggle headlights by key
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFrontLights();
        }
    }

    void ToggleFrontLights()
    {
        isFrontLightOn = !isFrontLightOn;

        // When headlights toggle we do NOT forcibly change isBackLightOn field (that is set by CarController when braking).
        // But tail lights should be visible whenever front lights are on, so UpdateLights will reflect that.
        UpdateLights();
    }

    // Kept signature so existing CarController calls (carLights.OperateBackLights()) still work.
    // CarController sets carLights.isBackLightOn = true/false before calling this.
    public void OperateBackLights()
    {
        // Interpret external isBackLightOn as "braking" if set by CarController
        isBraking = isBackLightOn;
        UpdateLights();
    }

    // Kept for compatibility if CarController calls this (or if you want to call it manually)
    public void OperateFrontLights()
    {
        UpdateLights();
    }

    void UpdateLights()
    {
        foreach (var light in lights)
        {
            if (light.side == Side.Front)
            {
                // Headlights follow isFrontLightOn
                light.lightObj.SetActive(isFrontLightOn);
                light.lightMat.color = isFrontLightOn ? frontLightOnColor : frontLightOffColor;
            }
            else if (light.side == Side.Back)
            {
                // Tail/brake logic:
                // - Tail lights visible when headlights are on (dim).
                // - Brake lights visible (bright) when CarController sets isBackLightOn = true and calls OperateBackLights().
                // - If neither, back lights are off.
                bool shouldBeOn = isFrontLightOn || isBraking;

                light.lightObj.SetActive(shouldBeOn);

                if (isBraking)
                {
                    light.lightMat.color = brakeLightColor;
                }
                else if (isFrontLightOn)
                {
                    light.lightMat.color = backLightOnColor;
                }
                else
                {
                    light.lightMat.color = backLightOffColor;
                }
            }
        }
    }

    // Optional: allow CarController to directly tell this component braking started/stopped.
    // If you want to use this instead of setting isBackLightOn property, call carLights.SetBraking(true/false)
    public void SetBraking(bool braking)
    {
        isBraking = braking;
        UpdateLights();
    }
}
