using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using System.Collections.Generic;



public class MenuController : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] private TMP_Text volumeTextValue = null;
    [SerializeField] private Slider volumeSlider = null;
    [SerializeField] private float defaultVolume = 1.0f;

    [Header("Gameplay Settings")]
    [SerializeField] private TMP_Text senTextValue = null;
    [SerializeField] private Slider senSlider = null;
    [SerializeField] private int defaultSen = 4;
    public int mainSen = 4;

    [Header("Toggle Settings")]
    [SerializeField] private Toggle invertYToggle = null;

    [Header("Graphics Settings")]
    [SerializeField] private Slider brightnessSlider = null;
    [SerializeField] private TMP_Text brightnessTextValue = null;
    [SerializeField] private float defaultBrightness = 1;

    [Space(10)]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullScreenToggle;

    private int _qualityLevel;
    private bool _isFullScreen;
    private float _brightnessLevel;

    [Header("Confirmation Box")]
    [SerializeField] private GameObject comfirmationPrompt = null;

    [Header("Levels To Load")]
    public string _newGameLevel;
    private string levelToLoad;
    [SerializeField] private GameObject noSavedGameDialog = null;

    [Header("Resolution Dropdowns")]
    public TMP_Dropdown resolutionDropdown;
    private Resolution[] resolutions;

    private void Start()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + "x" + resolutions[i].height; 
            options.Add(option);

            if(resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }
        resolutionDropdown.AddOptions(options); 
        resolutionDropdown.value = currentResolutionIndex; 
        resolutionDropdown.RefreshShownValue();
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }


    public void NewGameDialogYes() //For the yes to start a new game
    {
        SceneManager.LoadScene(_newGameLevel);
    }

    public void LoadGameDialogYes() //For the yes to load a current save
    {
        if (PlayerPrefs.HasKey("SavedLevel")) // Check if player has a current save 
        {
            levelToLoad = PlayerPrefs.GetString("SavedLevel");
            SceneManager.LoadScene(levelToLoad);
        }
        else // if player has no current save bring up no save found dialog 
        {
            noSavedGameDialog.SetActive(true);
        }
    }

    public void ExitGame() // Quit the game
    {
        Application.Quit();
    }

    public void SetVolume(float volume) // Change the Master Volume for the whole game 
    {
        AudioListener.volume = volume;
        volumeTextValue.text = volume.ToString("0.0");
    }

    public void VolumeApply() // Apply the changes you made to the audio settings
    {
        PlayerPrefs.SetFloat("masterVolume", AudioListener.volume);
        StartCoroutine(ComfirmationBox());
    }

    public void SetSen(float sen) // Set Sensitvity 
    {
        mainSen = Mathf.RoundToInt (sen);
        senTextValue.text = sen.ToString("0");
    }

    public void GameplayApply() // Apply Gameplay settings
    {
        if (invertYToggle.isOn)
        {
            PlayerPrefs.SetInt("masterInvertY", 1);
            // invert Y
        }
        else
        {
            PlayerPrefs.SetInt("masterInvertY", 0);
            // don't invert Y
        }

        PlayerPrefs.SetFloat("masterSen", mainSen);
        StartCoroutine (ComfirmationBox());
    }

    public void setBrightness(float brightness) // set brightness
    {
        _brightnessLevel = brightness;
        brightnessTextValue.text = brightness.ToString("0.0");
    }

    public void setFullScreen(bool isFullscreen) // Set full screen
    {
        _isFullScreen = isFullscreen;
    }
    
    public void SetQuality(int qualityIndex) // set quality
    {
        _qualityLevel = qualityIndex;
    }

    public void GraphicsApply() // apply quality settings
    {
        PlayerPrefs.SetFloat("masterBrightness", _brightnessLevel);
        // change your brightness with your post processing

        PlayerPrefs.SetInt("masterQuality", _qualityLevel);
        QualitySettings.SetQualityLevel(_qualityLevel);

        PlayerPrefs.SetInt("masterFullscreen", (_isFullScreen ? 1 : 0));
        Screen.fullScreen = _isFullScreen;

        StartCoroutine (ComfirmationBox());
    }

    public void ResetButton(string MenuType) // Reset all volume values back to default settings
    {

        if (MenuType == "Graphics")
        {
            brightnessSlider.value = defaultBrightness;
            brightnessTextValue.text = defaultBrightness.ToString("0.0");
            qualityDropdown.value = 1;
            QualitySettings.SetQualityLevel(1);

            fullScreenToggle.isOn = false;
            Screen.fullScreen = false;

            Resolution currentResolution = Screen.currentResolution;
            Screen.SetResolution(currentResolution.width, currentResolution.height, Screen.fullScreen);
            resolutionDropdown.value = resolutions.Length;
            GraphicsApply();
        }

        if(MenuType == "Audio")
        {
            AudioListener.volume = defaultVolume;
            volumeSlider.value = defaultVolume;
            volumeTextValue.text = defaultVolume.ToString("0.0");
            VolumeApply();
        }

        if(MenuType == "Gameplay")
        {
            senTextValue.text = defaultSen.ToString("0");
            senSlider.value = defaultSen;
            mainSen = defaultSen;
            invertYToggle.isOn = false;
            GameplayApply();
        }
    }

    public IEnumerator ComfirmationBox() // Bot that pops up when a save has been made
    {
        comfirmationPrompt.SetActive(true);
        yield return new WaitForSeconds(2);
        comfirmationPrompt.SetActive(false);
    }
}
