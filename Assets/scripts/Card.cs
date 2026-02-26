using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
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
    }

    public void FlipCard(bool showFace)
    {
        if (isFaceUp != showFace)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayFlip();
        }

        isFaceUp = showFace;

        if (isFaceUp)
        {
            cardImage.sprite = frontSprite;
        }
        else
        {
            cardImage.sprite = backSprite;
        }
    }
}