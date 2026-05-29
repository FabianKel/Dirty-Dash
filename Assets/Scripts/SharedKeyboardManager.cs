using UnityEngine;
using UnityEngine.InputSystem;

public class SharedKeyboardManager : MonoBehaviour
{
    [Header("Asigna aqui a tus jugadores desde la jerarquia")]
    public PlayerInput player1Input;
    public PlayerInput player2Input;

    [Header("Nombres exactos de tus Control Schemes")]
    public string p1Scheme = "Keyboard1";
    public string p2Scheme = "Keyboard2";

    void Start()
    {
        if (Keyboard.current == null)
        {
            Debug.LogWarning("No se detecto ningun teclado.");
            return;
        }

        if (player1Input != null)
        {
            player1Input.SwitchCurrentControlScheme(p1Scheme, Keyboard.current);
        }

        if (player2Input != null)
        {
            player2Input.SwitchCurrentControlScheme(p2Scheme, Keyboard.current);
        }

        Debug.Log("Teclado compartido asignado a ambos jugadores exitosamente por codigo.");
    }
}