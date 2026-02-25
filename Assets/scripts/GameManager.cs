using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform playerHand;
    public Transform enemyHand;
    public Transform deckArea;
    public Transform tableArea;
    public Transform discardPile;
    public GameObject bitoVisual;
    public GameObject bitoButton;
    public GameObject takeButton;
    public bool isFirstTurn = true;
    public bool isPlayerAttacker = true;

    public bool isBotTaking = false;

    public Sprite[] clubsSprites;
    public Sprite[] diamondsSprites;
    public Sprite[] heartsSprites;
    public Sprite[] spadesSprites;

    public Image trumpCardUI;
    public Image mainDeckUI;

    public GameObject gameOverScreen;
    public Text gameOverText;
    public bool isGameOver = false;

    private List<GameObject> deck = new List<GameObject>();
    public Card.CardSuit trumpSuit;

    void Start()
    {
        CreateDeck();
        ShuffleDeck();
        SetTrumpCard();
        DealCards();
    }

    void Update()
    {
        if (isGameOver) return;

        if (tableArea.childCount > 0)
        {
            if (isPlayerAttacker)
            {
                takeButton.SetActive(false);
                bitoButton.SetActive(AreAllCardsDefended() || isBotTaking);
            }
            else
            {
                bitoButton.SetActive(false);
                takeButton.SetActive(true);
            }
        }
        else
        {
            bitoButton.SetActive(false);
            takeButton.SetActive(false);
        }
    }

    public int GetMaxAttackCards()
    {
        int maxByRules = isFirstTurn ? 5 : 6;
        Transform defenderHand = isPlayerAttacker ? enemyHand : playerHand;

        int defenderCapacity = defenderHand.childCount;
        foreach (Transform t in tableArea)
        {
            if (t.childCount > 0) defenderCapacity++;
        }

        return Mathf.Min(maxByRules, defenderCapacity);
    }


    public bool HasCardsToToss(Transform hand)
    {
        if (tableArea.childCount == 0) return false;
        if (tableArea.childCount >= GetMaxAttackCards()) return false;

        Card[] tableCards = tableArea.GetComponentsInChildren<Card>();
        foreach (Transform cardTrans in hand)
        {
            Card handCard = cardTrans.GetComponent<Card>();
            foreach (Card tc in tableCards)
            {
                if (handCard.value == tc.value) return true;
            }
        }
        return false;
    }

    public void StartBotTakeTimer()
    {
        if (isBotTaking) return;
        StartCoroutine(BotTakeCoroutine());
    }

    IEnumerator BotTakeCoroutine()
    {
        isBotTaking = true;


        if (!HasCardsToToss(playerHand))
        {
            yield return new WaitForSeconds(1.0f);
        }
        else
        {
            yield return new WaitForSeconds(5.0f); 
        }

        if (isBotTaking) TakeCards();
    }

    public void PlayerTakesCards()
    {
        StartCoroutine(PlayerTakeCoroutine());
    }

    IEnumerator PlayerTakeCoroutine()
    {
        takeButton.SetActive(false);
        EnemyAI bot = GetComponent<EnemyAI>();

        bool botTossed = false;

        if (bot != null && HasCardsToToss(enemyHand))
        {
            botTossed = true;
            yield return StartCoroutine(bot.TossAllPossibleCardsCoroutine());
        }


        if (botTossed)
        {
            yield return new WaitForSeconds(2.0f);
        }

        TakeCards();
    }

    public void TakeCards()
    {
        isBotTaking = false;
        Transform targetHand = isPlayerAttacker ? enemyHand : playerHand;

        Card[] allCardsOnTable = tableArea.GetComponentsInChildren<Card>();
        foreach (Card c in allCardsOnTable)
        {
            c.transform.SetParent(tableArea, true);
        }

        while (tableArea.childCount > 0)
        {
            Transform cardTransform = tableArea.GetChild(0);
            CardMovement cardMove = cardTransform.GetComponent<CardMovement>();
            Card cardData = cardTransform.GetComponent<Card>();

            cardTransform.SetParent(targetHand, false);
            if (cardMove != null) cardMove.defaultParent = targetHand;

            CanvasGroup cg = cardTransform.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true;

            if (!isPlayerAttacker) cardData.FlipCard(true);
            else cardData.FlipCard(false);
        }

        if (isFirstTurn) isFirstTurn = false;

        DrawCards(); 


        if (CheckWinCondition()) return;


        if (!isPlayerAttacker)
        {
            EnemyAI bot = GetComponent<EnemyAI>();
            if (bot != null) bot.TryToAttack();
        }
    }

    public void SendToBito()
    {
        if (isBotTaking)
        {
            StopAllCoroutines();
            TakeCards();
            return;
        }

        while (tableArea.childCount > 0)
        {
            Transform card = tableArea.GetChild(0);
            card.SetParent(discardPile, false);
            card.gameObject.SetActive(false);
        }

        if (isFirstTurn) isFirstTurn = false;
        if (bitoVisual != null) bitoVisual.SetActive(true);

        DrawCards();

        if (CheckWinCondition()) return;

        isPlayerAttacker = !isPlayerAttacker;

        if (!isPlayerAttacker)
        {
            EnemyAI bot = GetComponent<EnemyAI>();
            if (bot != null) bot.TryToAttack();
        }
    }

    public bool AreAllCardsDefended()
    {
        if (tableArea.childCount == 0) return false;
        foreach (Transform attackCard in tableArea)
        {
            if (attackCard.childCount == 0) return false;
        }
        return true;
    }

    public void DrawCards()
    {
        if (isPlayerAttacker)
        {
            RefillHand(playerHand, true);
            RefillHand(enemyHand, false);
        }
        else
        {
            RefillHand(enemyHand, false);
            RefillHand(playerHand, true);
        }

        if (deck.Count <= 1 && mainDeckUI != null) mainDeckUI.gameObject.SetActive(false);
        if (deck.Count == 0 && trumpCardUI != null)
        {
            Color ghostColor = trumpCardUI.color;
            ghostColor.a = 0.35f;
            trumpCardUI.color = ghostColor;
        }
    }

    void RefillHand(Transform hand, bool faceUp)
    {
        int cardsNeeded = 6 - hand.childCount;
        for (int i = 0; i < cardsNeeded; i++)
        {
            if (deck.Count > 0) GiveCardTo(hand, faceUp);
        }
    }

    void CreateDeck()
    {
        for (int s = 0; s < 4; s++)
        {
            for (int v = 6; v <= 14; v++)
            {
                GameObject newCard = Instantiate(cardPrefab, deckArea);
                newCard.name = "Card_" + (Card.CardSuit)s + "_" + (Card.CardValue)v;
                newCard.SetActive(false);

                Card cardScript = newCard.GetComponent<Card>();
                cardScript.suit = (Card.CardSuit)s;
                cardScript.value = (Card.CardValue)v;

                int spriteIndex = v - 6;
                if (s == 0) cardScript.frontSprite = clubsSprites[spriteIndex];
                else if (s == 1) cardScript.frontSprite = diamondsSprites[spriteIndex];
                else if (s == 2) cardScript.frontSprite = heartsSprites[spriteIndex];
                else if (s == 3) cardScript.frontSprite = spadesSprites[spriteIndex];

                cardScript.FlipCard(false);
                deck.Add(newCard);
            }
        }
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            GameObject temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    void SetTrumpCard()
    {
        if (deck.Count > 0)
        {
            GameObject bottomCard = deck[deck.Count - 1];
            Card trumpCardScript = bottomCard.GetComponent<Card>();
            trumpSuit = trumpCardScript.suit;
            trumpCardUI.sprite = trumpCardScript.frontSprite;
        }
    }

    void DealCards()
    {
        for (int i = 0; i < 6; i++)
        {
            GiveCardTo(playerHand, true);
            GiveCardTo(enemyHand, false);
        }
    }

    void GiveCardTo(Transform hand, bool faceUp)
    {
        if (deck.Count > 0)
        {
            GameObject cardToDeal = deck[0];
            deck.RemoveAt(0);
            cardToDeal.transform.SetParent(hand, false);
            cardToDeal.SetActive(true);
            cardToDeal.GetComponent<Card>().FlipCard(faceUp);
        }
    }


    bool CheckWinCondition()
    {
        if (deck.Count == 0)
        {
            if (playerHand.childCount == 0 && enemyHand.childCount == 0) { EndGame("Нічия!"); return true; }
            if (playerHand.childCount == 0) { EndGame("Ти виграв!"); return true; }
            if (enemyHand.childCount == 0) { EndGame("Ти програв!"); return true; }
        }
        return false;
    }

    void EndGame(string message)
    {
        isGameOver = true;
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
        if (gameOverText != null) gameOverText.text = message;

        bitoButton.SetActive(false);
        takeButton.SetActive(false);
    }


    public void RestartGame()
    {

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {

        SceneManager.LoadScene("MainMenu");
    }
}