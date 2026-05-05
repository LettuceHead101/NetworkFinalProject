using Unity.Netcode;
using UnityEngine;

public class PressurePlate : NetworkBehaviour
{
    [Header("Link the Door Here")]
    public NetworkedDoor linkedDoor;

    private int playersOnPlate = 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            playersOnPlate++;
            linkedDoor.isOpen.Value = true; 
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            playersOnPlate--;
            if (playersOnPlate <= 0)
            {
                playersOnPlate = 0;
                linkedDoor.isOpen.Value = false; 
            }
        }
    }
}