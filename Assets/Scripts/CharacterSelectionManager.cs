using UnityEngine;
using UnityEngine.UI;
using TMPro; // Necesario para TextMeshPro

public class CharacterSelectionManager : MonoBehaviour
{
    public CharacterData[] allCharacters;

    [Header("Estado")]
    public int p1Index = 0;
    public int p2Index = 1;
    public bool p1Ready, p2Ready;

    [Header("UI Cursors & Slots")]
    public RectTransform p1Cursor;
    public RectTransform p2Cursor;
    public Transform[] slots;
    public GameObject readyButton;

    [Header("UI Feedback Text")]
    public TextMeshProUGUI p1ReadyText;
    public TextMeshProUGUI p2ReadyText;

    void Start()
    {
        // Inicializamos los textos como invisibles o desactivados
        p1ReadyText.gameObject.SetActive(false);
        p2ReadyText.gameObject.SetActive(false);
        readyButton.SetActive(false);
    }

    void Update()
    {
        // Controles J1: WASD + Espacio
        if (!p1Ready)
        {
            if (Input.GetKeyDown(KeyCode.A)) MoveP1(-1);
            if (Input.GetKeyDown(KeyCode.D)) MoveP1(1);
            if (Input.GetKeyDown(KeyCode.Space)) { p1Ready = true; p1ReadyText.gameObject.SetActive(true); }
        }

        // Controles J2: Flechas + Enter
        if (!p2Ready)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveP2(-1);
            if (Input.GetKeyDown(KeyCode.RightArrow)) MoveP2(1);
            if (Input.GetKeyDown(KeyCode.Return)) { p2Ready = true; p2ReadyText.gameObject.SetActive(true); }
        }

        ActualizarUI();
    }

    void MoveP1(int dir)
    {
        // Calculamos el siguiente movimiento con el carrusel
        int next = (p1Index + dir + slots.Length) % slots.Length;

        // Si el siguiente slot es donde está parado el P2, saltamos uno más en la misma dirección
        if (next == p2Index)
        {
            next = (next + dir + slots.Length) % slots.Length;
        }

        p1Index = next;
    }

    void MoveP2(int dir)
    {
        int next = (p2Index + dir + slots.Length) % slots.Length;

        // Si el siguiente slot es donde está parado el P1, saltamos uno más
        if (next == p1Index)
        {
            next = (next + dir + slots.Length) % slots.Length;
        }

        p2Index = next;
    }

    void ActualizarUI()
    {
        p1Cursor.position = slots[p1Index].position;
        p2Cursor.position = slots[p2Index].position;

        if (p1Ready && p2Ready)
        {
            readyButton.SetActive(true);
            // Solo intentamos guardar si el Instance existe
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.p1Selected = allCharacters[p1Index];
                GameDataManager.Instance.p2Selected = allCharacters[p2Index];
            }
        }
    }
}