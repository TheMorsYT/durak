using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Components;

public class CardMP : NetworkBehaviour
{
    public enum CardSuit { Clubs, Diamonds, Hearts, Spades }
    public enum CardValue { Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10, Jack = 11, Queen = 12, King = 13, Ace = 14, Joker = 15 }

    public NetworkVariable<ulong> ownerId = new NetworkVariable<ulong>(9999);
    public NetworkVariable<int> syncSuit = new NetworkVariable<int>(-1);
    public NetworkVariable<int> syncValue = new NetworkVariable<int>(-1);

    private bool isFaceUp = false;

    public CardSuit suit;
    public CardValue value;

    public Sprite frontSprite;
    public Sprite backSprite;
    private Image cardImage;
    private bool spriteAssigned = false;

    private void Awake()
    {
        cardImage = GetComponent<Image>();
    }
    public override void OnNetworkSpawn()
    {
        if (TryGetComponent(out NetworkTransform netTransform))
        {
            netTransform.enabled = false;
        }
    }

    private void Update()
    {
        if (!spriteAssigned && syncSuit.Value != -1 && GameManagerMP.Instance != null)
        {
            suit = (CardSuit)syncSuit.Value;
            value = (CardValue)syncValue.Value;
            AssignSprite();
            spriteAssigned = true;
        }

        if (GameManagerMP.Instance != null && spriteAssigned)
        {
            bool isMine = (ownerId.Value == NetworkManager.Singleton.LocalClientId);
            bool isAttackCard = transform.parent == GameManagerMP.Instance.tableArea;
            bool isDefendCard = transform.parent != null &&
                    transform.parent.GetComponent<CardMP>() != null &&
                    transform.parent.parent == GameManagerMP.Instance.tableArea;

            bool shouldBeFaceUp = isMine || isAttackCard || isDefendCard;

            if (isFaceUp != shouldBeFaceUp)
            {
                isFaceUp = shouldBeFaceUp;
                cardImage.sprite = isFaceUp ? frontSprite : backSprite;
            }

            transform.localRotation = Quaternion.identity;

            if (isDefendCard)
            {
                transform.SetAsLastSibling();
                transform.localPosition = new Vector3(30, -30, 0);
            }
            else if (isMine || transform.parent != GameManagerMP.Instance.deckArea)
            {
                transform.localScale = Vector3.one;
            }
        }
    }

    private void AssignSprite()
    {
        if (value == CardValue.Joker)
        {
            int spriteIdx = suit == CardSuit.Clubs ? 0 : 1;
            if (GameManagerMP.Instance.jokerSprites.Length > spriteIdx) frontSprite = GameManagerMP.Instance.jokerSprites[spriteIdx];
        }
        else
        {
            int idx = (int)value - 2;
            if (suit == CardSuit.Clubs && GameManagerMP.Instance.clubsSprites.Length > idx) frontSprite = GameManagerMP.Instance.clubsSprites[idx];
            else if (suit == CardSuit.Diamonds && GameManagerMP.Instance.diamondsSprites.Length > idx) frontSprite = GameManagerMP.Instance.diamondsSprites[idx];
            else if (suit == CardSuit.Hearts && GameManagerMP.Instance.heartsSprites.Length > idx) frontSprite = GameManagerMP.Instance.heartsSprites[idx];
            else if (suit == CardSuit.Spades && GameManagerMP.Instance.spadesSprites.Length > idx) frontSprite = GameManagerMP.Instance.spadesSprites[idx];
        }

        cardImage.sprite = isFaceUp ? frontSprite : backSprite;
    }
}