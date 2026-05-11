using UnityEngine;
using Unity.Netcode;

public class WinScreenManager : MonoBehaviour
{
    void Start()
    {
        // Despawn the player bodies
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude);
            foreach (PlayerController p in players)
            {
                p.GetComponent<NetworkObject>().Despawn(true);
            }
        }
    }

    // --- NEW: The method the button will call ---
    public void QuitGame()
    {
        // 1. Safely disconnect from the network
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // 2. Close the actual built game
        Application.Quit();

        // 3. Stop playing in the Unity Editor (so you don't have to hit the play button manually)
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}