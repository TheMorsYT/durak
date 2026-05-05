using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameManagerMP : NetworkBehaviour
{
    public static GameManagerMP Instance { get; private set; }

    [Header("Board References")]
    public GameObject cardPrefabMP;
    public Transform[] seats;
    public Transform playerHand;
    public Transform enemyHand;
    public Transform deckArea;
    public Transform tableArea;
    public Transform discardPile;

    [Header("Action UI")]
    public GameObject bitoVisual;
    public GameObject bitoButton;
    public GameObject takeButton;
    public GameObject transferZone;

    [Header("Deck UI")]
    public Image trumpCardUI;
    public Image mainDeckUI;
    public TMP_Text deckCardsText;

    [Header("Game Over UI")]
    public GameObject gameOverScreen;
    public Text gameOverText;
    public TMP_Text waitingPlayersText;

    [Header("Mode")]
    public bool isTransferMode = true;

    [Header("Profiles")]
    public GameObject[] playerProfiles;
    public Image[] timerRingsAttack;
    public Image[] timerRingsDefend;
    public TMP_Text[] nicknameTexts;
    public Image[] avatarImages;

    [Header("Card Sprites")]
    public Sprite[] clubsSprites;
    public Sprite[] diamondsSprites;
    public Sprite[] heartsSprites;
    public Sprite[] spadesSprites;
    public Sprite[] jokerSprites;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base.OnDestroy();
    }

    public void StartSimpleMatch()
    {
    }

    public void RestartGame()
    {
    }

    public void RequestRestartReady()
    {
    }

    public void OnBitoButtonClicked()
    {
    }

    public void OnPassButtonClicked()
    {
    }

    public void OnTakeButtonClicked()
    {
    }

    public void LeaveMultiplayerGame()
    {
    }
}
