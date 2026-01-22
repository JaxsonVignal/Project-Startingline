using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System.Collections;

public class MainMenuCameraManager : MonoBehaviour
{
    [Header("Cinemachine Virtual Cameras")]
    [Tooltip("Array of virtual cameras to cycle through")]
    public CinemachineVirtualCamera[] virtualCameras;

    [Tooltip("Dolly carts corresponding to each camera (optional, same order as cameras)")]
    public CinemachineDollyCart[] dollyCarts;

    [Header("Transition Settings")]
    [Tooltip("Duration of fade transition in seconds")]
    public float fadeDuration = 1f;

    [Tooltip("Switch cameras when dolly track completes")]
    public bool switchOnTrackComplete = true;

    [Tooltip("How long to stay on each camera before switching (if not using track complete)")]
    public float cameraDisplayTime = 8f;

    [Tooltip("Time camera moves while screen is black")]
    public float cameraMoveTime = 2f;

    [Tooltip("Enable automatic camera cycling")]
    public bool autoCycle = true;

    [Tooltip("Keep fade active while camera is moving")]
    public bool keepFadeDuringMove = true;

    [Tooltip("Reset dolly carts to start when switching cameras")]
    public bool resetDollyOnSwitch = true;

    [Header("Fade Panel")]
    [Tooltip("UI Image used for fade effect (assign a black fullscreen panel)")]
    public Image fadePanel;

    private int currentCameraIndex = 0;
    private CinemachineBrain cinemachineBrain;
    private Coroutine cycleCoroutine;
    private bool isTransitioning = false;

    void Start()
    {
        // Get or add Cinemachine Brain to main camera
        cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
        if (cinemachineBrain == null)
        {
            cinemachineBrain = Camera.main.gameObject.AddComponent<CinemachineBrain>();
        }

        // Set blend to instant cut (no blending)
        cinemachineBrain.m_DefaultBlend.m_Time = 0f;
        cinemachineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;

        // Setup fade panel
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            Color c = fadePanel.color;
            c.a = 0f;
            fadePanel.color = c;
        }

        // Initialize cameras and dolly carts
        if (virtualCameras.Length > 0)
        {
            // Stop all dolly carts initially
            StopAllDollyCarts();

            SetActiveCamera(0);

            if (autoCycle && !switchOnTrackComplete)
            {
                cycleCoroutine = StartCoroutine(AutoCycleCameras());
            }
        }
        else
        {
            Debug.LogWarning("No virtual cameras assigned to MainMenuCameraManager!");
        }
    }

    void StopAllDollyCarts()
    {
        if (dollyCarts == null) return;

        for (int i = 0; i < dollyCarts.Length; i++)
        {
            if (dollyCarts[i] != null)
            {
                dollyCarts[i].enabled = false;
            }
        }
    }

    void Update()
    {
        // Check if current dolly track is complete
        if (switchOnTrackComplete && autoCycle && !isTransitioning)
        {
            if (dollyCarts != null && currentCameraIndex < dollyCarts.Length)
            {
                CinemachineDollyCart currentCart = dollyCarts[currentCameraIndex];
                if (currentCart != null && currentCart.enabled)
                {
                    // Check if cart has reached the end of the path
                    CinemachinePathBase path = currentCart.m_Path;
                    if (path != null)
                    {
                        float maxPos = path.MaxPos;
                        float currentPosition = currentCart.m_Position;

                        // Debug log to see values
                        // Debug.Log($"Cart {currentCameraIndex}: Position={currentPosition}, MaxPos={maxPos}");

                        // If cart is at or near the end of the path
                        if (currentPosition >= maxPos - 0.01f)
                        {
                            SwitchToNextCamera();
                        }
                    }
                }
            }
        }
    }

    void SetActiveCamera(int index)
    {
        // Disable all cameras
        for (int i = 0; i < virtualCameras.Length; i++)
        {
            if (virtualCameras[i] != null)
            {
                virtualCameras[i].Priority = 0;
            }
        }

        // Stop all dolly carts
        StopAllDollyCarts();

        // Enable selected camera
        if (index >= 0 && index < virtualCameras.Length && virtualCameras[index] != null)
        {
            virtualCameras[index].Priority = 10;
            currentCameraIndex = index;

            // Start the dolly cart for this camera
            if (dollyCarts != null && index < dollyCarts.Length && dollyCarts[index] != null)
            {
                if (resetDollyOnSwitch)
                {
                    dollyCarts[index].m_Position = 0f; // Reset to start
                }
                dollyCarts[index].enabled = true; // Start moving
            }
        }
    }

    IEnumerator AutoCycleCameras()
    {
        while (true)
        {
            yield return new WaitForSeconds(cameraDisplayTime);
            SwitchToNextCamera();
        }
    }

    public void SwitchToNextCamera()
    {
        int nextIndex = (currentCameraIndex + 1) % virtualCameras.Length;
        SwitchToCamera(nextIndex);
    }

    public void SwitchToPreviousCamera()
    {
        int prevIndex = currentCameraIndex - 1;
        if (prevIndex < 0) prevIndex = virtualCameras.Length - 1;
        SwitchToCamera(prevIndex);
    }

    public void SwitchToCamera(int index)
    {
        if (index < 0 || index >= virtualCameras.Length) return;
        StartCoroutine(TransitionToCamera(index));
    }

    IEnumerator TransitionToCamera(int targetIndex)
    {
        isTransitioning = true;

        if (fadePanel == null)
        {
            // No fade panel, just switch directly
            SetActiveCamera(targetIndex);
            isTransitioning = false;
            yield break;
        }

        // Fade to black
        yield return StartCoroutine(FadeToBlack());

        // Switch camera while screen is black
        SetActiveCamera(targetIndex);

        if (keepFadeDuringMove)
        {
            // Stay black while camera moves
            yield return new WaitForSeconds(cameraMoveTime);

            // Then fade from black
            yield return StartCoroutine(FadeFromBlack());
        }
        else
        {
            // Immediate fade from black (old behavior)
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(FadeFromBlack());
        }

        isTransitioning = false;
    }

    IEnumerator FadeToBlack()
    {
        float elapsed = 0f;
        Color c = fadePanel.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            fadePanel.color = c;
            yield return null;
        }

        c.a = 1f;
        fadePanel.color = c;
    }

    IEnumerator FadeFromBlack()
    {
        float elapsed = 0f;
        Color c = fadePanel.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            fadePanel.color = c;
            yield return null;
        }

        c.a = 0f;
        fadePanel.color = c;
    }

    public void SetAutoCycle(bool enabled)
    {
        autoCycle = enabled;

        if (autoCycle && cycleCoroutine == null)
        {
            cycleCoroutine = StartCoroutine(AutoCycleCameras());
        }
        else if (!autoCycle && cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
            cycleCoroutine = null;
        }
    }

    void OnDestroy()
    {
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
        }
    }
}