using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkedCoin : NetworkBehaviour
{
    private bool isCollected = false; 

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            if (IsServer)
            {
                ProcessCoinCollection();
            }
            else 
            {
                // Call the modernized RPC
                RequestCollectRpc();
            }
        }
    }

    // --- THE NEW UNITY 6 RPC SYNTAX ---
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestCollectRpc()
    {
        ProcessCoinCollection();
    }
    // ----------------------------------

    private void ProcessCoinCollection()
    {
        if (isCollected) return; 
        isCollected = true;

        NetworkedCoin[] remainingCoins = FindObjectsByType<NetworkedCoin>(FindObjectsInactive.Exclude);

        // If this is the final coin...
        if (remainingCoins.Length <= 1) 
        {
            // 1. Get the current scene's Build Index number
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            
            // 2. Calculate the next scene's number
            int nextSceneIndex = currentSceneIndex + 1;

            // 3. Safety Check: Ensure the next level exists in your Build Settings
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                // Extract the exact name of the next scene dynamically
                string nextScenePath = SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
                string nextSceneName = System.IO.Path.GetFileNameWithoutExtension(nextScenePath);
                
                // Load it!
                NetworkManager.Singleton.SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("You beat the last level! Add a Win Screen to your Build Settings.");
            }
        }
        
        GetComponent<NetworkObject>().Despawn();
    }
}