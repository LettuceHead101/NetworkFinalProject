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

        if (remainingCoins.Length <= 1) 
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Level2", LoadSceneMode.Single);
        }
        
        GetComponent<NetworkObject>().Despawn();
    }
}