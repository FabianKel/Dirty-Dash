using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EffectItem : MonoBehaviour
{
    public enum EffectType
    {
        Slow,           // Rival slows down
        Blind,          // Rival's screen goes dark
        InvertControls, // Rival's left/right flip
        Boost,          // You get a speed boost
    }

    [Header("Effect")]
    public EffectType effectType = EffectType.Slow;
    public float duration = 4f;

    [Header("Optional VFX")]
    public GameObject collectVFX; 

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController collector = other.GetComponent<PlayerController>();
        if (collector == null) return;

        int rivalIndex = collector.playerIndex == 1 ? 2 : 1;
        PlayerController rival = PlayerRegistry.Get(rivalIndex);

        switch (effectType)
        {
            case EffectType.Slow:
                rival?.ApplySlow(duration);
                break;
            case EffectType.Blind:
                rival?.ApplyBlind(duration);
                break;
            case EffectType.InvertControls:
                rival?.ApplyInvertControls(duration);
                break;
            case EffectType.Boost:
                collector.ApplyBoost(duration);
                break;
        }

        if (collectVFX)
            Instantiate(collectVFX, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}