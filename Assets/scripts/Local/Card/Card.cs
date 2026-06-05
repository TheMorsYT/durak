using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public static readonly Vector2 FixedSize = new Vector2(100f, 150f);

    public enum CardSuit { Clubs, Diamonds, Hearts, Spades }
    public enum CardValue
    {
        Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7,
        Eight = 8, Nine = 9, Ten = 10, Jack = 11, Queen = 12, King = 13,
        Ace = 14, Joker = 15
    }

    public CardSuit suit;
    public CardValue value;

    public Sprite frontSprite;
    public Sprite backSprite;

    private Image cardImage;
    private bool isFaceUp = false;
    private void Awake()
    {
        cardImage = GetComponent<Image>();
        ApplyFixedSize();
    }

    private void OnEnable()
    {
        ApplyFixedSize();
    }

    public void FlipCard(bool showFace)
    {
        if (isFaceUp != showFace) SoundManager.Instance?.PlayFlip();

        isFaceUp = showFace;
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }

        cardImage.sprite = isFaceUp ? frontSprite : backSprite;
        ApplyFixedSize();
    }

    private void ApplyFixedSize()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = FixedSize;
            rectTransform.localScale = Vector3.one;
        }
        else
        {
            transform.localScale = Vector3.one;
        }

        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }

        if (cardImage != null)
        {
            cardImage.preserveAspect = false;
        }

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = FixedSize.x;
        layoutElement.minHeight = FixedSize.y;
        layoutElement.preferredWidth = FixedSize.x;
        layoutElement.preferredHeight = FixedSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
        layoutElement.ignoreLayout = false;
    }

}
