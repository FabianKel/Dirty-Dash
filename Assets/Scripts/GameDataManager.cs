using UnityEngine;

[System.Serializable]
public class PlayerStats
{
    public int lives = 3;
    public int deathCounter = 0;
    public ItemData itemInHand;
    public int itemsUsedCounter = 0;
    public int itemsReceivedCounter = 0;
    public bool reachedGoal = false;

    public void Reset()
    {
        lives = 3;
        deathCounter = 0;
        itemInHand = null; 
        itemsUsedCounter = 0;
        itemsReceivedCounter = 0;
        reachedGoal = false;
    }
}

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;

    [Header("Selección de Personajes")]
    public CharacterData p1Selected;
    public CharacterData p2Selected;

    [Header("Estadísticas de Partida")]
    public PlayerStats p1Stats = new PlayerStats();
    public PlayerStats p2Stats = new PlayerStats();

    [Header("Debug")]
    public CharacterData characterPorDefecto;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (p1Selected == null) p1Selected = characterPorDefecto;
            if (p2Selected == null) p2Selected = characterPorDefecto;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetAllStats()
    {
        p1Stats.Reset();
        p2Stats.Reset();
    }
}