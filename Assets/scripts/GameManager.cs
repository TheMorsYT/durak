using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

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
    public Image trumpCardUI;
    public Image mainDeckUI;
    public GameObject gameOverScreen;
    public Text gameOverText;

    public bool isFirstTurn = true;
    public bool isPlayerAttacker = true;
    public bool isBotTaking = false;
    public bool isGameOver = false;

    public bool isTurnChanging = false;

    public Sprite[] clubsSprites;
    public Sprite[] diamondsSprites;
    public Sprite[] heartsSprites;
    public Sprite[] spadesSprites;
    public Sprite[] jokerSprites;

    private List<GameObject> deck = new List<GameObject>();
    public Card.CardSuit trumpSuit;

    void Start()
    {
        CreateDeck();
        ShuffleDeck();
        SetTrumpCard();
        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        yield return StartCoroutine(RefillHandsWithAnimation());
        DetermineFirstTurn();
    }

    void Update()
    {
        if (isGameOver || isTurnChanging) return;

        int cardsOnTable = tableArea.GetComponentsInChildren<Card>().Length;

        if (cardsOnTable > 0)
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

    void DetermineFirstTurn()
    {
        int minPlayerTrump = 100;
        int minEnemyTrump = 100;
        foreach (Transform cardTrans in playerHand)
        {
            Card card = cardTrans.GetComponent<Card>();
            if (card.suit == trumpSuit && (int)card.value < minPlayerTrump)
                minPlayerTrump = (int)card.value;
        }
        foreach (Transform cardTrans in enemyHand)
        {
            Card card = cardTrans.GetComponent<Card>();
            if (card.suit == trumpSuit && (int)card.value < minEnemyTrump)
                minEnemyTrump = (int)card.value;
        }

        if (minPlayerTrump < minEnemyTrump) isPlayerAttacker = true;
        else if (minEnemyTrump < minPlayerTrump) isPlayerAttacker = false;
        else isPlayerAttacker = true;

        if (!isPlayerAttacker)
        {
            EnemyAI bot = GetComponent<EnemyAI>();
            if (bot != null) bot.TryToAttack();
        }
    }

    public void DrawCards()
    {
        StartCoroutine(RefillHandsWithAnimation());
    }

    IEnumerator RefillHandsWithAnimation()
    {
        Transform first = isPlayerAttacker ? playerHand : enemyHand;
        Transform second = isPlayerAttacker ? enemyHand : playerHand;
        bool firstFaceUp = isPlayerAttacker;
        bool secondFaceUp = !isPlayerAttacker;

        while (deck.Count > 0 && (first.childCount < 6 || second.childCount < 6))
        {
            if (first.childCount < 6 && deck.Count > 0)
            {
                GiveCardTo(first, firstFaceUp);
                yield return new WaitForSeconds(0.15f);
            }
            if (second.childCount < 6 && deck.Count > 0)
            {
                GiveCardTo(second, secondFaceUp);
                yield return new WaitForSeconds(0.15f);
            }
        }

        if (deck.Count <= 1 && mainDeckUI != null) mainDeckUI.gameObject.SetActive(false);
        if (deck.Count == 0 && trumpCardUI != null)
        {
            Color c = trumpCardUI.color; c.a = 0.35f; trumpCardUI.color = c;
        }
    }

    void GiveCardTo(Transform hand, bool faceUp)
    {
        if (deck.Count > 0)
        {
            GameObject card = deck[0];
            deck.RemoveAt(0);
            card.transform.SetParent(hand, false);
            card.transform.SetAsLastSibling();
            card.SetActive(true);
            card.GetComponent<Card>().FlipCard(faceUp);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayDeal();
        }
    }

    public void TakeCards()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayTake();
        isBotTaking = false;
        Transform targetHand = isPlayerAttacker ? enemyHand : playerHand;

        Card[] allCardsOnTable = tableArea.GetComponentsInChildren<Card>();
        foreach (Card c in allCardsOnTable)
        {
            c.transform.SetParent(tableArea, true);
        }

        while (tableArea.GetComponentsInChildren<Card>().Length > 0)
        {
            Card cardData = tableArea.GetComponentsInChildren<Card>()[0];
            Transform cardTransform = cardData.transform;
            CardMovement cardMove = cardTransform.GetComponent<CardMovement>();

            cardTransform.SetParent(targetHand, false);
            cardTransform.localPosition = Vector3.zero;
            cardTransform.SetAsLastSibling();

            if (cardMove != null) cardMove.defaultParent = targetHand;

            CanvasGroup cg = cardTransform.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true;

            cardData.FlipCard(!isPlayerAttacker);
        }

        if (isFirstTurn) isFirstTurn = false;
        DrawCards();
        CheckWinCondition();

        if (!isPlayerAttacker)
        {
            EnemyAI bot = GetComponent<EnemyAI>();
            if (bot != null) bot.TryToAttack();
        }
    }

    public void SendToBito()
    {
        if (isTurnChanging) return;

        if (isBotTaking)
        {
            StopAllCoroutines();
            isTurnChanging = false;
            TakeCards();
            return;
        }

        isTurnChanging = true;

        bitoButton.SetActive(false);
        takeButton.SetActive(false);

        if (SoundManager.Instance != null) SoundManager.Instance.PlayBito();

        Card[] allCardsOnTable = tableArea.GetComponentsInChildren<Card>();
        foreach (Card c in allCardsOnTable)
        {
            c.transform.SetParent(tableArea, true);
        }

        while (tableArea.GetComponentsInChildren<Card>().Length > 0)
        {
            Transform card = tableArea.GetComponentsInChildren<Card>()[0].transform;
            card.SetParent(discardPile, false);
            card.gameObject.SetActive(false);
        }

        if (isFirstTurn) isFirstTurn = false;
        if (bitoVisual != null) bitoVisual.SetActive(true);

        DrawCards();

        if (CheckWinCondition())
        {
            isTurnChanging = false;
            return;
        }

        isPlayerAttacker = !isPlayerAttacker;
        EnemyAI bot = GetComponent<EnemyAI>();
        if (bot != null)
        {
            if (!isPlayerAttacker) bot.TryToAttack();
            bot.UpdateMemory();
        }

        isTurnChanging = false;
    }

    void CreateDeck()
    {
        int deckSize = PlayerPrefs.GetInt("DeckSize", 36);
        int startValue = (deckSize == 36) ? 6 : 2;
        deck.Clear();

        for (int s = 0; s < 4; s++)
        {
            for (int v = startValue; v <= 14; v++)
            {
                GameObject newCard = Instantiate(cardPrefab, deckArea);

                newCard.name = ((Card.CardSuit)s).ToString() + "_" + ((Card.CardValue)v).ToString();

                newCard.SetActive(false);
                Card cardScript = newCard.GetComponent<Card>();
                cardScript.suit = (Card.CardSuit)s;
                cardScript.value = (Card.CardValue)v;

                int idx = (int)v - 2;
                if (s == 0) cardScript.frontSprite = clubsSprites[idx];
                else if (s == 1) cardScript.frontSprite = diamondsSprites[idx];
                else if (s == 2) cardScript.frontSprite = heartsSprites[idx];
                else if (s == 3) cardScript.frontSprite = spadesSprites[idx];

                deck.Add(newCard);
            }
        }

        if (deckSize == 54)
        {
            AddJokerToDeck(Card.CardSuit.Clubs, 0);
            AddJokerToDeck(Card.CardSuit.Hearts, 1);
        }
    }

    void AddJokerToDeck(Card.CardSuit suit, int spriteIdx)
    {
        GameObject newCard = Instantiate(cardPrefab, deckArea);

        newCard.name = "Joker_" + suit.ToString();

        newCard.SetActive(false);
        Card cardScript = newCard.GetComponent<Card>();
        cardScript.suit = suit;
        cardScript.value = Card.CardValue.Joker;
        if (jokerSprites.Length > spriteIdx) cardScript.frontSprite = jokerSprites[spriteIdx];
        deck.Add(newCard);
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            GameObject temp = deck[i];
            int rand = Random.Range(i, deck.Count);
            deck[i] = deck[rand];
            deck[rand] = temp;
        }
        if (SoundManager.Instance != null) SoundManager.Instance.PlayShuffle();
    }

    void SetTrumpCard()
    {
        if (deck.Count > 0)
        {
            Card ts = deck[deck.Count - 1].GetComponent<Card>();
            trumpSuit = ts.suit;
            trumpCardUI.sprite = ts.frontSprite;
        }
    }

    public bool AreAllCardsDefended()
    {
        if (tableArea.childCount == 0) return false;
        foreach (Transform t in tableArea) { if (t.childCount == 0) return false; }
        return true;
    }

    public int GetMaxAttackCards()
    {
        int max = isFirstTurn ? 5 : 6;
        int defCount = isPlayerAttacker ? enemyHand.childCount : playerHand.childCount;
        int currentOnTable = tableArea.GetComponentsInChildren<Card>().Length;
        return Mathf.Min(max, defCount + currentOnTable);
    }

    public bool HasCardsToToss(Transform hand)
    {
        if (tableArea.childCount == 0 || tableArea.childCount >= GetMaxAttackCards()) return false;
        var tableValues = tableArea.GetComponentsInChildren<Card>().Select(c => c.value);
        return hand.Cast<Transform>().Any(t => tableValues.Contains(t.GetComponent<Card>().value));
    }

    public void StartBotTakeTimer() { StartCoroutine(BotTakeCoroutine()); }

    IEnumerator BotTakeCoroutine()
    {
        isBotTaking = true;
        yield return new WaitForSeconds(HasCardsToToss(playerHand) ? 5.0f : 1.0f);
        if (isBotTaking) TakeCards();
    }

    public void PlayerTakesCards()
    {
        if (isTurnChanging) return;
        isTurnChanging = true;

        StartCoroutine(PlayerTakeCoroutine());
    }

    IEnumerator PlayerTakeCoroutine()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();

        takeButton.SetActive(false);
        bitoButton.SetActive(false);

        EnemyAI bot = GetComponent<EnemyAI>();
        if (bot != null && HasCardsToToss(enemyHand)) yield return StartCoroutine(bot.TossAllPossibleCardsCoroutine());
        yield return new WaitForSeconds(1.0f);

        TakeCards();

        isTurnChanging = false;
    }

    public bool CheckWinCondition()
    {
        if (deck.Count == 0)
        {
            int lang = PlayerPrefs.GetInt("GameLanguage", 0); 

            if (playerHand.childCount == 0 && enemyHand.childCount == 0) { EndGame(lang == 0 ? "Нічия!" : "Draw!"); return true; }
            if (playerHand.childCount == 0) { EndGame(lang == 0 ? "Виграш!" : "You Win!"); return true; }
            if (enemyHand.childCount == 0) { EndGame(lang == 0 ? "Програш!" : "You Lose!"); return true; }
        }
        return false;
    }
    void EndGame(string msg)
    {
        isGameOver = true;
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
        if (gameOverText != null) gameOverText.text = msg;
        bitoButton.SetActive(false);
        takeButton.SetActive(false);
    }

    public void RestartGame()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        SceneManager.LoadScene("MainMenu");
    }
}