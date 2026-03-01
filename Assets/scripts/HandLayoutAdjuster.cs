using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HorizontalLayoutGroup))]
public class HandLayoutAdjuster : MonoBehaviour
{
    private HorizontalLayoutGroup layoutGroup;
    private RectTransform rectTransform;

    [Header("Налаштування")]
    public float defaultSpacing = 5f;
    public float cardWidth = 100f;  

    void Start()
    {
        layoutGroup = GetComponent<HorizontalLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        int cardCount = transform.childCount;

        if (cardCount < 2)
        {
            layoutGroup.spacing = defaultSpacing;
            return;
        }

        float availableWidthForSpacing = rectTransform.rect.width - (cardWidth * cardCount);

        float requiredSpacing = availableWidthForSpacing / (cardCount - 1);

        layoutGroup.spacing = Mathf.Min(defaultSpacing, requiredSpacing);
    }
}