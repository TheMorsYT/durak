using UnityEngine;
using UnityEngine.UI; 
using TMPro;          

public class LocalizedText : MonoBehaviour
{
    [TextArea(1, 3)] public string textUkr;
    [TextArea(1, 3)] public string textEng;

    private Text standardText;
    private TextMeshProUGUI tmpText;

    void Awake()
    {
 
        standardText = GetComponent<Text>();
        tmpText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        if (standardText == null) standardText = GetComponent<Text>();
        if (tmpText == null) tmpText = GetComponent<TextMeshProUGUI>();

        int lang = PlayerPrefs.GetInt("GameLanguage", 0);
        string newText = (lang == 0) ? textUkr : textEng;

        if (standardText != null)
        {
            standardText.text = newText;
        }

        if (tmpText != null)
        {
            tmpText.text = newText;
        }
    }
}