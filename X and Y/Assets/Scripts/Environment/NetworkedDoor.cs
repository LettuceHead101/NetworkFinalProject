using Unity.Netcode;
using UnityEngine;

public class NetworkedDoor : NetworkBehaviour
{
    // A synced variable. The Server writes to it, everyone reads it.
    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);

    private SpriteRenderer sr;
    private Collider2D col;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        // If isOpen is true, enabled is false (door vanishes)
        // If isOpen is false, enabled is true (door is solid)
        sr.enabled = !isOpen.Value;
        col.enabled = !isOpen.Value;
    }
}