using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HorizontalLayoutGroup))]
public sealed class HandLayoutAdjuster : MonoBehaviour
{
    [Header("Налаштування")]
    public float defaultSpacing = 5f;
    public float cardWidth = 100f;

    private HorizontalLayoutGroup layoutGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        layoutGroup = GetComponent<HorizontalLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        RecalculateSpacing();
    }

    private void OnTransformChildrenChanged()
    {
        RecalculateSpacing();
    }

    private void OnRectTransformDimensionsChange()
    {
        RecalculateSpacing();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        layoutGroup ??= GetComponent<HorizontalLayoutGroup>();
        rectTransform ??= GetComponent<RectTransform>();
        RecalculateSpacing();
    }

    public void RecalculateSpacing()
    {
        if (layoutGroup == null || rectTransform == null)
        {
            return;
        }

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
