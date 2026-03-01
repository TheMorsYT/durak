using UnityEngine;
using UnityEngine.UI;

public class SortSettingsUI : MonoBehaviour
{
    public Toggle manualToggle;
    public Toggle rankToggle;
    public Toggle suitToggle;

    void Start()
    {
        int currentSort = PlayerPrefs.GetInt("SortMethod", 0);
        manualToggle.isOn = currentSort == 0;
        rankToggle.isOn = currentSort == 1;
        suitToggle.isOn = currentSort == 2;

        manualToggle.onValueChanged.AddListener(delegate { OnSortChanged(0, manualToggle); });
        rankToggle.onValueChanged.AddListener(delegate { OnSortChanged(1, rankToggle); });
        suitToggle.onValueChanged.AddListener(delegate { OnSortChanged(2, suitToggle); });
    }

    public void OnSortChanged(int sortType, Toggle changedToggle)
    {
        if (!changedToggle.isOn) return;

        PlayerPrefs.SetInt("SortMethod", sortType);
        PlayerPrefs.Save();

        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();

        if (GameManager.Instance != null && !GameManager.Instance.isDealing)
        {
            GameManager.Instance.SortPlayerHand();
        }
    }
}