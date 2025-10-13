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
    public bool isFrontLightOn;
    public bool isBackLightOn;

    [Header("Light Colors")]
    public Color frontLightOnColor;
    public Color frontLightOffColor;
    public Color backLightOnColor;
    public Color backLightOffColor;

    [Header("Lights")]
    public List<Light> lights;

    [Header("Controls")]
    public KeyCode toggleKey = KeyCode.H; // Key to toggle headlights

    void Start()
    {
        isFrontLightOn = false;
        isBackLightOn = false;
        UpdateLights();
    }

    void Update()
    {
        // Toggle headlights on key press
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFrontLights();
        }
    }

    void ToggleFrontLights()
    {
        isFrontLightOn = !isFrontLightOn;
        isBackLightOn = isFrontLightOn; // Back lights follow front lights
        UpdateLights();
    }

    public void OperateBackLights()
    {
        // Used by CarController during braking
        UpdateLights();
    }

    public void OperateFrontLights()
    {
        // Also used by CarController when necessary
        UpdateLights();
    }

    void UpdateLights()
    {
        foreach (var light in lights)
        {
            if (light.side == Side.Front)
            {
                light.lightObj.SetActive(isFrontLightOn);
                light.lightMat.color = isFrontLightOn ? frontLightOnColor : frontLightOffColor;
            }
            else if (light.side == Side.Back)
            {
                light.lightObj.SetActive(isBackLightOn);
                light.lightMat.color = isBackLightOn ? backLightOnColor : backLightOffColor;
            }
        }
    }
}
