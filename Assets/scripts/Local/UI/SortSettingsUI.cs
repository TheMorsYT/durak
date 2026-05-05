using UnityEngine;
using UnityEngine.UI;
using Durak.Architecture.Singleplayer.Core;

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

        SoundManager.Instance?.PlayClick();

        MatchControllerSP controller = MatchControllerSP.Instance;
        if (controller != null && !controller.IsDealInProgress)
        {
            controller.SortPlayerHand();
        }
    }
}
