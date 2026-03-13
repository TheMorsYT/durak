using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using Unity.Netcode;

public class GameManagerMP : NetworkBehaviour
{
    public static GameManagerMP Instance { get; private set; }

    public GameObject cardPrefabMP;
    public Transform[] seats;
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

    public bool isTransferMode = true;
    public GameObject transferZone;

    public Sprite[] clubsSprites;
    public Sprite[] diamondsSprites;
    public Sprite[] heartsSprites;
    public Sprite[] spadesSprites;
    public Sprite[] jokerSprites;

    private List<GameObject> serverDeck = new List<GameObject>();
    public CardMP.CardSuit trumpSuit;

    public NetworkVariable<ulong> currentAttackerId = new NetworkVariable<ulong>(9999);
    public NetworkVariable<ulong> currentDefenderId = new NetworkVariable<ulong>(9999);
    public NetworkVariable<int> playersInGame = new NetworkVariable<int>(2);
    public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false);

    public NetworkVariable<bool> attackerPassed = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> firstAttackWave = new NetworkVariable<bool>(true);

    public NetworkVariable<ulong> p0_Id = new NetworkVariable<ulong>(9999);
    public NetworkVariable<ulong> p1_Id = new NetworkVariable<ulong>(9999);
    public NetworkVariable<ulong> p2_Id = new NetworkVariable<ulong>(9999);
    public NetworkVariable<ulong> p3_Id = new NetworkVariable<ulong>(9999);

    private List<ulong> bitoVotes = new List<ulong>();
    private bool localHasVotedBito = false;

    private float heroSpacing;
    private float villainSpacing;
    private Vector2 heroSize;
    private Vector2 villainSize;
    private Vector3[] defaultSeatPos;
    private Quaternion[] defaultSeatRot;

    private void Awake()
    {
        Instance = this;

        int savedMode = PlayerPrefs.GetInt("GameMode", 1);
        isTransferMode = (savedMode == 1);

        if (seats == null || seats.Length == 0) return;

        HorizontalLayoutGroup h0 = seats[0].GetComponent<HorizontalLayoutGroup>();
        HorizontalLayoutGroup h1 = seats[1].GetComponent<HorizontalLayoutGroup>();
        heroSpacing = h0 != null ? h0.spacing : 0f;
        villainSpacing = h1 != null ? h1.spacing : -35f;

        heroSize = seats[0].GetComponent<RectTransform>().sizeDelta;
        villainSize = seats[1].GetComponent<RectTransform>().sizeDelta;

        defaultSeatPos = new Vector3[seats.Length];
        defaultSeatRot = new Quaternion[seats.Length];

        for (int i = 0; i < seats.Length; i++)
        {
            defaultSeatPos[i] = seats[i].GetComponent<RectTransform>().anchoredPosition3D;
            defaultSeatRot[i] = seats[i].GetComponent<RectTransform>().localRotation;
        }
    }

    public override void OnNetworkSpawn()
    {
        p0_Id.OnValueChanged += (oldVal, newVal) => RotateTableLocally();
        p1_Id.OnValueChanged += (oldVal, newVal) => RotateTableLocally();
        p2_Id.OnValueChanged += (oldVal, newVal) => RotateTableLocally();
        p3_Id.OnValueChanged += (oldVal, newVal) => RotateTableLocally();

        RotateTableLocally();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
        }

        if (IsServer)
        {
            StartServerSetupAsync();
        }
    }

    private async void OnPlayerDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        if (IsServer && !isGameOver.Value)
        {
            await LeaveRoutineAsync();
        }
    }

    public async void LeaveMultiplayerGame()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        await LeaveRoutineAsync();
    }

    private async System.Threading.Tasks.Task LeaveRoutineAsync()
    {
        if (NetworkManager.Singleton != null)
        {
            if (IsServer && NetworkManager.Singleton.IsListening)
            {
                ForceClientsToMenuRpc();

                var clients = NetworkManager.Singleton.ConnectedClientsIds.ToList();
                foreach (var id in clients)
                {
                    if (id != NetworkManager.Singleton.LocalClientId)
                        NetworkManager.Singleton.DisconnectClient(id);
                }

                await System.Threading.Tasks.Task.Delay(500);
            }

            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            Destroy(NetworkManager.Singleton.gameObject);
        }

        SceneManager.LoadScene("MainMenu");
    }

    [Rpc(SendTo.NotServer)]
    private void ForceClientsToMenuRpc()
    {
        ForceLeaveLocal();
    }

    private void ForceLeaveLocal()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }
        SceneManager.LoadScene("MainMenu");
    }

    private async void StartServerSetupAsync()
    {
        try
        {
            await System.Threading.Tasks.Task.Delay(1500);
            if (this == null || !IsSpawned) return;

            var clients = NetworkManager.Singleton.ConnectedClientsList;
            playersInGame.Value = clients.Count;

            p0_Id.Value = 9999; p1_Id.Value = 9999; p2_Id.Value = 9999; p3_Id.Value = 9999;

            if (clients.Count > 0) p0_Id.Value = clients[0].ClientId;
            if (clients.Count > 1) p1_Id.Value = clients[1].ClientId;
            if (clients.Count > 2) p2_Id.Value = clients[2].ClientId;
            if (clients.Count > 3) p3_Id.Value = clients[3].ClientId;

            CreateAndShuffleDeck();

            await System.Threading.Tasks.Task.Delay(500);
            if (this == null || !IsSpawned) return;

            SetTrumpCard();
            DealCards();

            await System.Threading.Tasks.Task.Delay(1500);
            if (this == null || !IsSpawned) return;

            DetermineFirstTurn();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManagerMP] ПОМИЛКА РОЗДАЧІ: {e.Message}");
        }
    }

    private void CreateAndShuffleDeck()
    {
        int deckSize = PlayerPrefs.GetInt("DeckSize", 36);
        int startValue = (deckSize == 24) ? 9 : (deckSize == 36 ? 6 : 2);
        List<Vector2Int> virtualDeck = new List<Vector2Int>();

        for (int s = 0; s < 4; s++)
        {
            for (int v = startValue; v <= 14; v++) virtualDeck.Add(new Vector2Int(s, v));
        }

        if (deckSize == 54)
        {
            virtualDeck.Add(new Vector2Int(0, 15));
            virtualDeck.Add(new Vector2Int(2, 15));
        }

        for (int i = 0; i < virtualDeck.Count; i++)
        {
            Vector2Int temp = virtualDeck[i];
            int rand = Random.Range(i, virtualDeck.Count);
            virtualDeck[i] = virtualDeck[rand];
            virtualDeck[rand] = temp;
        }

        serverDeck.Clear();
        foreach (var cardData in virtualDeck)
        {
            GameObject newCard = Instantiate(cardPrefabMP, deckArea);
            newCard.GetComponent<NetworkObject>().Spawn();
            CardMP cardScript = newCard.GetComponent<CardMP>();
            cardScript.syncSuit.Value = cardData.x;
            cardScript.syncValue.Value = cardData.y;
            serverDeck.Add(newCard);
        }
    }

    private void SetTrumpCard()
    {
        if (serverDeck.Count > 0)
        {
            int validTrumpIndex = serverDeck.Count - 1;
            while (validTrumpIndex >= 0 && serverDeck[validTrumpIndex].GetComponent<CardMP>().syncValue.Value == 15) validTrumpIndex--;

            if (validTrumpIndex >= 0 && validTrumpIndex != serverDeck.Count - 1)
            {
                GameObject temp = serverDeck[serverDeck.Count - 1];
                serverDeck[serverDeck.Count - 1] = serverDeck[validTrumpIndex];
                serverDeck[validTrumpIndex] = temp;
            }

            CardMP ts = serverDeck[serverDeck.Count - 1].GetComponent<CardMP>();
            trumpSuit = (CardMP.CardSuit)ts.syncSuit.Value;
            SyncTrumpCardClientRpc(ts.syncSuit.Value, ts.syncValue.Value);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SyncTrumpCardClientRpc(int suit, int cardValue)
    {
        trumpSuit = (CardMP.CardSuit)suit;
        int idx = cardValue - 2;
        if (suit == 0 && clubsSprites.Length > idx) trumpCardUI.sprite = clubsSprites[idx];
        else if (suit == 1 && diamondsSprites.Length > idx) trumpCardUI.sprite = diamondsSprites[idx];
        else if (suit == 2 && heartsSprites.Length > idx) trumpCardUI.sprite = heartsSprites[idx];
        else if (suit == 3 && spadesSprites.Length > idx) trumpCardUI.sprite = spadesSprites[idx];
        trumpCardUI.transform.SetAsFirstSibling();
    }

    public void DealCards()
    {
        if (!IsServer) return;
        StartCoroutine(DealCardsWithAnimationCoroutine());
    }

    IEnumerator DealCardsWithAnimationCoroutine()
    {
        bool cardsDealt = false;
        bool dealing = true;

        while (dealing && serverDeck.Count > 0)
        {
            dealing = false;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                int seatIndex = GetSeatIndex(client.ClientId);
                if (seats[seatIndex].childCount < 6 && serverDeck.Count > 0)
                {
                    dealing = true;
                    cardsDealt = true;

                    GameObject card = serverDeck[0];
                    serverDeck.RemoveAt(0);

                    NetworkObject netObj = card.GetComponent<NetworkObject>();
                    netObj.ChangeOwnership(client.ClientId);
                    netObj.TrySetParent(seats[seatIndex], true);
                    card.GetComponent<CardMP>().ownerId.Value = client.ClientId;

                    DealCardClientRpc(netObj.NetworkObjectId, client.ClientId);
                    yield return new WaitForSeconds(0.15f);
                }
            }
        }

        CheckDeckUIClientRpc(serverDeck.Count);
        if (cardsDealt) SortAllHandsClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void DealCardClientRpc(ulong cardId, ulong clientId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardId, out NetworkObject cardObj))
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayDeal();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SortAllHandsClientRpc() { SortPlayerHand(); }

    [Rpc(SendTo.Everyone)]
    private void CheckDeckUIClientRpc(int cardsLeft)
    {
        if (cardsLeft <= 1 && mainDeckUI != null) mainDeckUI.gameObject.SetActive(false);
        if (cardsLeft == 0 && trumpCardUI != null)
        {
            Color c = trumpCardUI.color; c.a = 0.35f; trumpCardUI.color = c;
        }
    }

    private void DetermineFirstTurn()
    {
        ulong firstPlayerId = NetworkManager.Singleton.LocalClientId;
        int minTrump = 100;

        CardMP[] allCards = FindObjectsByType<CardMP>(FindObjectsSortMode.None);

        foreach (CardMP card in allCards)
        {
            if (card.ownerId.Value == 9999 || card.ownerId.Value == ulong.MaxValue) continue;

            if (card.syncSuit.Value == (int)trumpSuit && card.syncValue.Value < minTrump)
            {
                minTrump = card.syncValue.Value;
                firstPlayerId = card.ownerId.Value;
            }
        }

        currentAttackerId.Value = firstPlayerId;
        currentDefenderId.Value = GetNextActivePlayer(firstPlayerId);
        firstAttackWave.Value = true;
        ResetBitoLocallyRpc();
    }

    private ulong GetNextActivePlayer(ulong currentPlayerId)
    {
        List<ulong> activePlayers = new List<ulong>();
        if (p0_Id.Value != 9999) activePlayers.Add(p0_Id.Value);
        if (p1_Id.Value != 9999) activePlayers.Add(p1_Id.Value);
        if (p2_Id.Value != 9999) activePlayers.Add(p2_Id.Value);
        if (p3_Id.Value != 9999) activePlayers.Add(p3_Id.Value);

        int currentIndex = activePlayers.IndexOf(currentPlayerId);
        if (currentIndex == -1) return activePlayers.Count > 0 ? activePlayers[0] : 0;

        for (int i = 1; i <= activePlayers.Count; i++)
        {
            int checkIndex = (currentIndex + i) % activePlayers.Count;
            ulong checkId = activePlayers[checkIndex];
            if (seats[GetSeatIndex(checkId)].childCount > 0 || serverDeck.Count > 0)
            {
                return checkId;
            }
        }
        return currentPlayerId;
    }

    public int GetSeatIndex(ulong clientId)
    {
        int index = 0;
        if (clientId == p0_Id.Value) index = 0;
        else if (clientId == p1_Id.Value) index = 1;
        else if (clientId == p2_Id.Value) index = 2;
        else if (clientId == p3_Id.Value) index = 3;

        int pCount = playersInGame.Value <= 0 ? 2 : playersInGame.Value;
        int step = seats.Length / pCount;
        return (index * step) % seats.Length;
    }

    private void RotateTableLocally()
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        int totalSeats = seats.Length;
        int mySeat = GetSeatIndex(myId);

        playerHand = seats[mySeat];
        int pCount = playersInGame.Value <= 0 ? 2 : playersInGame.Value;
        int step = totalSeats / pCount;
        enemyHand = seats[(mySeat + step) % totalSeats];

        int shift = mySeat;
        for (int i = 0; i < totalSeats; i++)
        {
            if (seats[i] == null) continue;

            int configIndex = (i - shift + totalSeats) % totalSeats;
            RectTransform rt = seats[i].GetComponent<RectTransform>();

            rt.anchoredPosition3D = defaultSeatPos[configIndex];
            rt.localRotation = defaultSeatRot[configIndex];

            HorizontalLayoutGroup hlg = seats[i].GetComponent<HorizontalLayoutGroup>();

            if (seats[i] == playerHand)
            {
                if (hlg != null) hlg.spacing = heroSpacing;
                rt.sizeDelta = heroSize;
            }
            else
            {
                if (hlg != null) hlg.spacing = villainSpacing;
                rt.sizeDelta = villainSize;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    public void SortPlayerHand()
    {
        int sortType = PlayerPrefs.GetInt("SortMethod", 0);
        if (sortType == 0 || playerHand == null || playerHand.childCount == 0) return;

        List<CardMP> cardsInHand = new List<CardMP>();
        foreach (Transform t in playerHand)
        {
            CardMP c = t.GetComponent<CardMP>();
            if (c != null) cardsInHand.Add(c);
        }

        if (sortType == 1) cardsInHand = cardsInHand.OrderBy(c => (int)c.value).ThenBy(c => c.suit).ToList();
        else if (sortType == 2) cardsInHand = cardsInHand.OrderBy(c => c.suit == trumpSuit ? 1 : 0).ThenBy(c => c.suit).ThenBy(c => (int)c.value).ToList();

        for (int i = 0; i < cardsInHand.Count; i++) cardsInHand[i].transform.SetSiblingIndex(i);

        LayoutRebuilder.ForceRebuildLayoutImmediate(playerHand.GetComponent<RectTransform>());
    }

    private void Update()
    {
        if (NetworkManager.Singleton != null && !IsServer && !NetworkManager.Singleton.IsConnectedClient)
        {
            ForceLeaveLocal();
            return;
        }

        if (isGameOver.Value) return;

        ulong myId = NetworkManager.Singleton.LocalClientId;
        bool amIAttacker = (myId == currentAttackerId.Value);
        bool amIDefender = (myId == currentDefenderId.Value);

        int cardsOnTable = tableArea.GetComponentsInChildren<CardMP>().Length;

        if (!attackerPassed.Value) localHasVotedBito = false;

        if (cardsOnTable > 0)
        {
            bool allDefended = AreAllCardsDefended();

            if (amIDefender)
            {
                takeButton.SetActive(!allDefended);
                bitoButton.SetActive(false);

                bool canTransfer = false;
                if (isTransferMode && !allDefended)
                {
                    bool allUndefended = true;
                    int firstCardValue = -1;
                    int rootCardCount = 0;

                    foreach (Transform t in tableArea)
                    {
                        CardMP rootCard = t.GetComponent<CardMP>();
                        if (rootCard != null)
                        {
                            rootCardCount++;
                            if (t.childCount > 0)
                            {
                                allUndefended = false;
                                break;
                            }

                            if (firstCardValue == -1) firstCardValue = rootCard.syncValue.Value;
                            else if (firstCardValue != rootCard.syncValue.Value)
                            {
                                allUndefended = false;
                                break;
                            }
                        }
                    }


                    if (rootCardCount > 0 && allUndefended)
                    {
                        ulong nextPlayer = GetNextActivePlayer(myId);
                        int nextPlayerCards = seats[GetSeatIndex(nextPlayer)].childCount;

                        if (nextPlayerCards >= rootCardCount + 1)
                        {
                            canTransfer = true;
                        }
                    }
                }

                if (transferZone != null) transferZone.SetActive(canTransfer);

            }
            else
            {
                takeButton.SetActive(false);
                if (transferZone != null) transferZone.SetActive(false); 

                if (firstAttackWave.Value)
                {
                    if (amIAttacker) bitoButton.SetActive(allDefended && !localHasVotedBito);
                    else bitoButton.SetActive(false);
                }
                else
                {
                    if (playerHand.childCount > 0) bitoButton.SetActive(allDefended && !localHasVotedBito);
                    else bitoButton.SetActive(false);
                }
            }
        }
        else
        {
            bitoButton.SetActive(false);
            takeButton.SetActive(false);
            if (transferZone != null) transferZone.SetActive(false);
        }
    }

    public bool AreAllCardsDefended()
    {
        int attackCardsCount = 0;
        int defendCardsCount = 0;

        foreach (Transform t in tableArea)
        {
            if (t.GetComponent<CardMP>() != null)
            {
                attackCardsCount++;
                if (t.childCount > 0 && t.GetChild(0).GetComponent<CardMP>() != null) defendCardsCount++;
            }
        }

        if (attackCardsCount == 0) return false;
        return attackCardsCount == defendCardsCount;
    }

    public void CheckWinCondition()
    {
        if (!IsServer) return;

        if (serverDeck.Count == 0)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                int seatIndex = GetSeatIndex(client.ClientId);
                if (seats[seatIndex].childCount == 0)
                {
                    isGameOver.Value = true;
                    EndGameClientRpc(client.ClientId);
                    break;
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void EndGameClientRpc(ulong winnerId)
    {
        int lang = PlayerPrefs.GetInt("GameLanguage", 0);
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
        bitoButton.SetActive(false);
        takeButton.SetActive(false);

        if (NetworkManager.Singleton.LocalClientId == winnerId)
        {
            if (gameOverText != null) gameOverText.text = lang == 0 ? "Виграш!" : "You Win!";
        }
        else
        {
            if (gameOverText != null) gameOverText.text = lang == 0 ? "Програш!" : "You Lose!";
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ResetBitoLocallyRpc()
    {
        localHasVotedBito = false;
    }

    public void OnBitoButtonClicked()
    {
        localHasVotedBito = true;
        VoteBitoServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void VoteBitoServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!bitoVotes.Contains(clientId))
            bitoVotes.Add(clientId);

        if (clientId == currentAttackerId.Value)
        {
            attackerPassed.Value = true;
            firstAttackWave.Value = false;
        }

        int activeThrowers = 0;

        foreach (var c in NetworkManager.Singleton.ConnectedClientsIds)
        {
            int seatIndex = GetSeatIndex(c);

            if (c != currentDefenderId.Value && seats[seatIndex].childCount > 0)
                activeThrowers++;
        }

        if (activeThrowers == 0)
            activeThrowers = 1;

        if (bitoVotes.Count >= activeThrowers)
        {
            ExecuteBito();
        }
    }

    IEnumerator DelayedDespawn(List<NetworkObject> cards)
    {
        yield return new WaitForSeconds(0.4f);

        foreach (NetworkObject net in cards)
        {
            if (net != null && net.IsSpawned)
                net.Despawn();
        }
    }

    private void ExecuteBito()
    {
        List<NetworkObject> cards = new List<NetworkObject>();

        foreach (Transform t in tableArea)
        {
            NetworkObject net = t.GetComponent<NetworkObject>();
            if (net != null && net.IsSpawned)
                cards.Add(net);

            if (t.childCount > 0)
            {
                NetworkObject child = t.GetChild(0).GetComponent<NetworkObject>();
                if (child != null && child.IsSpawned)
                    cards.Add(child);
            }
        }

        foreach (NetworkObject net in cards)
        {
            net.TrySetParent(discardPile, false);
        }

        StartCoroutine(DelayedDespawn(cards));

        ulong nextAttacker = currentDefenderId.Value;
        currentAttackerId.Value = nextAttacker;
        currentDefenderId.Value = GetNextActivePlayer(nextAttacker);

        attackerPassed.Value = false;
        bitoVotes.Clear();
        firstAttackWave.Value = true;

        ResetBitoLocallyRpc();
        DealCards();
        PlayBitoVisualClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayBitoVisualClientRpc()
    {
        if (bitoVisual != null) bitoVisual.SetActive(true);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayBito();
    }

    public void OnTakeButtonClicked()
    {
        if (NetworkManager.Singleton.LocalClientId == currentDefenderId.Value) TakeCardsServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TakeCardsServerRpc()
    {
        ulong takerId = currentDefenderId.Value;
        int takerSeatIndex = GetSeatIndex(takerId);
        Transform takerHand = seats[takerSeatIndex];

        List<CardMP> rootCards = new List<CardMP>();
        foreach (Transform t in tableArea)
        {
            CardMP rootCard = t.GetComponent<CardMP>();
            if (rootCard != null) rootCards.Add(rootCard);
        }

        foreach (CardMP c in rootCards)
        {
            NetworkObject netObj = c.GetComponent<NetworkObject>();
            netObj.TrySetParent(takerHand, false);
            netObj.ChangeOwnership(takerId);
            c.ownerId.Value = takerId;

            if (c.transform.childCount > 0)
            {
                CardMP childCard = c.transform.GetChild(0).GetComponent<CardMP>();
                if (childCard != null)
                {
                    NetworkObject childNetObj = childCard.GetComponent<NetworkObject>();
                    childNetObj.TrySetParent(takerHand, false);
                    childNetObj.ChangeOwnership(takerId);
                    childCard.ownerId.Value = takerId;
                }
            }
        }

        ulong nextAttacker = GetNextActivePlayer(takerId);
        currentAttackerId.Value = nextAttacker;
        currentDefenderId.Value = GetNextActivePlayer(nextAttacker);

        attackerPassed.Value = false;
        bitoVotes.Clear();
        firstAttackWave.Value = true;

        ResetBitoLocallyRpc();
        PlayTakeVisualClientRpc();
        DealCards();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayTakeVisualClientRpc()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayTake();
        SortPlayerHand();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayCardServerRpc(ulong cardNetworkId, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        if (sender != currentAttackerId.Value && sender != currentDefenderId.Value)
            return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardNetworkId, out NetworkObject netObj))
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayCardToTable();
            netObj.TrySetParent(tableArea, false);

            attackerPassed.Value = false;
            bitoVotes.Clear();
            ResetBitoLocallyRpc();

            CheckWinCondition();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void DefendCardServerRpc(ulong defCardId, ulong targetCardId, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        if (sender != currentDefenderId.Value)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(defCardId, out NetworkObject defObj)) return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetCardId, out NetworkObject targetObj)) return;

        if (targetObj.transform.parent != tableArea) return;
        if (targetObj.transform.childCount > 0) return;

        CardMP defCard = defObj.GetComponent<CardMP>();
        CardMP targetCard = targetObj.GetComponent<CardMP>();

        if (defCard == null || targetCard == null) return;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayCardToTable();

        defObj.TrySetParent(targetObj.transform, true);

        CheckWinCondition();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TransferAttackServerRpc(ulong cardNetworkId, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        if (sender != currentDefenderId.Value)
            return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardNetworkId, out NetworkObject netObj))
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayCardToTable();
            netObj.TrySetParent(tableArea, false);

            ulong oldDefender = currentDefenderId.Value;
            currentAttackerId.Value = oldDefender;
            currentDefenderId.Value = GetNextActivePlayer(oldDefender);

            attackerPassed.Value = false;
            bitoVotes.Clear();
            firstAttackWave.Value = true;

            ResetBitoLocallyRpc();
            CheckWinCondition();
        }
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
        }

        base.OnDestroy();
    }
}