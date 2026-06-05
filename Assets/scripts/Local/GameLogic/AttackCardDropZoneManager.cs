using UnityEngine;
using UnityEngine.UI;

public class AttackCardDropZoneManager : MonoBehaviour
{
    private void Start()
    {
        EnsureDropZone();
    }

    private void OnEnable()
    {
        EnsureDropZone();
    }

    private void EnsureDropZone()
    {
        Card card = GetComponent<Card>();
        if (card == null)
        {
            return;
        }

        DropZone existingDropZone = GetComponentInChildren<DropZone>();
        if (existingDropZone != null)
        {
            existingDropZone.targetAttackCard = card;
            return;
        }

        GameObject dropZoneObj = new GameObject("DefenseDropZone");
        dropZoneObj.transform.SetParent(transform, false);
        dropZoneObj.transform.SetAsFirstSibling();

        RectTransform rectTransform = dropZoneObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        Image image = dropZoneObj.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0);
        image.raycastTarget = true;

        DropZone dropZone = dropZoneObj.AddComponent<DropZone>();
        dropZone.targetAttackCard = card;
    }
}
