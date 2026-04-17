using System.Collections.Generic;
using UnityEngine;

public class PlayerRegistry : MonoBehaviour
{
    public static PlayerRegistry Instance { get; private set; }

    private static readonly Dictionary<int, PlayerController> _players = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static void Register(PlayerController p)   => _players[p.playerIndex] = p;
    public static void Unregister(PlayerController p) => _players.Remove(p.playerIndex);

    public static PlayerController Get(int index)
    {
        _players.TryGetValue(index, out var p);
        return p;
    }
}