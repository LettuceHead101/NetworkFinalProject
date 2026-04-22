using System.Threading.Tasks;
using System.Collections.Generic; // NEW: Needed for lists
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro; 
using UnityEngine.UI; 

public class RelayManager : MonoBehaviour 
{
    [Header("UI Panels")]
    public GameObject connectPanel;
    public GameObject lobbyPanel;

    [Header("Connect Panel Elements")]
    public TMP_InputField joinInput;

    [Header("Lobby Panel Elements")]
    public TextMeshProUGUI codeDisplay;
    public GameObject startGameButton; 

    [Header("Game Prefabs")]
    public GameObject playerPrefab; // NEW: We hold the blueprint here now!

    private string joinCode = "";

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public void OnHostButtonClicked() { _ = CreateRelay(); }

    public void OnJoinButtonClicked()
    {
        if (joinInput.text.Length == 6) { _ = JoinRelay(joinInput.text); }
    }

    // --- NEW SPAWN LOGIC ---
    public void OnStartGameButtonClicked()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // 1. Tell the server: "When you finish loading Level1, run the SpawnPlayers method"
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SpawnPlayers;

            // 2. Start loading the scene
            NetworkManager.Singleton.SceneManager.LoadScene("Level1", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void SpawnPlayers(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        // Unsubscribe from the event so it doesn't accidentally run again later
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SpawnPlayers;

        if (sceneName == "Level1")
        {
            // Loop through every player ID that successfully loaded the level
            foreach (ulong clientId in clientsCompleted)
            {
                // 1. Determine spawn position based on who is connecting
                Vector3 spawnPosition;
                
                if (clientId == 0) // The Host is always ID 0 (Player X)
                {
                    spawnPosition = new Vector3(0f, -2f, 0f);
                }
                else // The Client is everyone else (Player Y)
                {
                    spawnPosition = new Vector3(-2f, 0f, 0f);
                }

                // 2. Spawn them at their specific coordinate
                GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                
                // Tell the network to assign this object to this specific player
                playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            }
        }
    }
    // -----------------------

    private async Task CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

            NetworkManager.Singleton.StartHost();

            connectPanel.SetActive(false);
            lobbyPanel.SetActive(true);
            codeDisplay.text = "YOUR JOIN CODE: " + joinCode;
            startGameButton.SetActive(true); 
        }
        catch (RelayServiceException e) { Debug.LogError(e); }
    }

    private async Task JoinRelay(string codeToJoin)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(codeToJoin);
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes, joinAllocation.Key,
                joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();

            connectPanel.SetActive(false);
            lobbyPanel.SetActive(true);
            codeDisplay.text = "Connected! Waiting for Host...";
            startGameButton.SetActive(false); 
        }
        catch (RelayServiceException e) { Debug.LogError(e); }
    }
}