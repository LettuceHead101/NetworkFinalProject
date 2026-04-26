using Unity.Netcode;
using UnityEngine;

public class PressurePlate : NetworkBehaviour
{
    [Header("Link the Door Here")]
    public NetworkedDoor linkedDoor;

    // We count players just in case BOTH players manage to stand on it at once
    private int playersOnPlate = 0;

    // This runs when something enters the trigger zone
    void OnTriggerEnter2D(Collider2D other)
    {
        // SECURITY: Only the Host computer is allowed to calculate button presses
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            playersOnPlate++;
            linkedDoor.isOpen.Value = true; // Open the door across the network!
        }
    }

    // This runs when something leaves the trigger zone
    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            playersOnPlate--;
            if (playersOnPlate <= 0)
            {
                playersOnPlate = 0;
                linkedDoor.isOpen.Value = false; // Close the door!
            }
        }
    }
}