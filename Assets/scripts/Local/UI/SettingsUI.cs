using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public Slider volumeSlider;

    [Header("Language Toggles (Radio Buttons)")]
    public Toggle toggleUkr;
    public Toggle toggleEng;

    private bool isInitialized = false;

    void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("GameVolume", 0.5f);
            volumeSlider.onValueChanged.AddListener(ChangeVolume);
        }

        int currentLang = PlayerPrefs.GetInt("GameLanguage", 0);

        if (toggleUkr != null && toggleEng != null)
        {
            toggleUkr.isOn = (currentLang == 0);
            toggleEng.isOn = (currentLang == 1);

            toggleUkr.onValueChanged.AddListener((isOn) => { if (isOn) ChangeLanguage(0); });
            toggleEng.onValueChanged.AddListener((isOn) => { if (isOn) ChangeLanguage(1); });
        }

        isInitialized = true;
    }

    public void ChangeVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("GameVolume", value);
    }

    private void ChangeLanguage(int index)
    {
        PlayerPrefs.SetInt("GameLanguage", index);

        if (isInitialized && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClick();
        }

        LocalizedText[] activeTexts = Object.FindObjectsByType<LocalizedText>(FindObjectsSortMode.None);
        foreach (LocalizedText lt in activeTexts)
        {
            lt.UpdateText();
        }
    }
}