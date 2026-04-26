using UnityEngine;
using Unity.Netcode;

public class KillZone : MonoBehaviour
{
    // We include both Trigger and Collision just in case you set your 
    // spike colliders to be solid objects OR ghostly triggers!
    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckAndRespawn(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckAndRespawn(collision.gameObject);
    }

    private void CheckAndRespawn(GameObject target)
    {
        // 1. Did a Player hit us?
        if (target.CompareTag("Player"))
        {

            Debug.Log("I was killed by the invisible object named: " + this.gameObject.name);

            PlayerController player = target.GetComponent<PlayerController>();

            

            // 2. Are we the computer that controls this player?
            // (If we don't check this, both computers will try to teleport the player at the same time!)
            if (player != null && player.IsOwner)
            {
                player.AddDeathServerRpc();

                // 3. Stop all falling momentum so they don't slide upon respawning
                Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }

                // 4. Teleport back to the specific drop zones!
                if (player.currentRole.Value == PlayerRole.PlayerX)
                {
                    target.transform.position = new Vector3(0f, -2f, 0f);
                }
                else if (player.currentRole.Value == PlayerRole.PlayerY)
                {
                    target.transform.position = new Vector3(-2f, 0f, 0f);
                }
            }
        }
    }
}