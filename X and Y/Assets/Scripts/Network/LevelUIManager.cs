using UnityEngine;
using Unity.Netcode;
using TMPro;

public class LevelUIManager : NetworkBehaviour
{
    [Header("UI Text Elements")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI playerXDeathText;
    public TextMeshProUGUI playerYDeathText;

    // The Server owns this timer and syncs it to the clients
    private NetworkVariable<float> levelTimer = new NetworkVariable<float>(0f);

    void Update()
    {
        // 1. Only the Server advances the clock
        if (IsServer)
        {
            levelTimer.Value += Time.deltaTime;
        }

        // 2. Everyone updates their UI to match the Server's clock
        if (timerText != null)
        {
            timerText.text = "Time: " + levelTimer.Value.ToString("F1") + "s";
        }

        // 3. UPDATED: Removed the obsolete FindObjectsSortMode to keep Unity 6 happy!
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude);
        
        foreach (PlayerController p in players)
        {
            if (p.currentRole.Value == PlayerRole.PlayerX && playerXDeathText != null)
            {
                playerXDeathText.text = "Blue Deaths: " + p.deathCount.Value;
            }
            else if (p.currentRole.Value == PlayerRole.PlayerY && playerYDeathText != null)
            {
                playerYDeathText.text = "Red Deaths: " + p.deathCount.Value;
            }
        }
    }

    // --- GRACEFUL QUIT METHOD ---
    public void QuitGame()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}