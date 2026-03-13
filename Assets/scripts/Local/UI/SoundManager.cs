using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    private AudioSource source;

    [Header("Card Sounds")]
    public AudioClip shuffle;
    public AudioClip deal;
    public AudioClip cardToTable;
    public AudioClip flip;
    public AudioClip takeCards;

    [Header("UI & Actions")]
    public AudioClip click;
    public AudioClip bito;
    public AudioClip sceneTransition;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float cardsVolume = 0.3f;
    [Range(0f, 1f)] public float uiVolume = 0.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        AudioListener.volume = PlayerPrefs.GetFloat("GameVolume", 0.5f);
    }

    public void PlayShuffle() => source.PlayOneShot(shuffle, cardsVolume);
    public void PlayDeal() => source.PlayOneShot(deal, cardsVolume);
    public void PlayCardToTable() => source.PlayOneShot(cardToTable, cardsVolume);
    public void PlayFlip() => source.PlayOneShot(flip, cardsVolume);
    public void PlayTake() => source.PlayOneShot(takeCards, cardsVolume);

    public void PlayClick() => source.PlayOneShot(click, uiVolume);
    public void PlayBito() => source.PlayOneShot(bito, uiVolume);
    public void PlayTransition() => source.PlayOneShot(sceneTransition, uiVolume);
}