using UnityEngine;
using Unity.Netcode; // Required for Networking

public class NetworkUI : MonoBehaviour
{
    void OnGUI()
    {
        // This draws a box in the top-left corner
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        
        // Only show the buttons if we haven't connected yet
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host (Player X)", GUILayout.Width(200), GUILayout.Height(50)))
            {
                NetworkManager.Singleton.StartHost();
            }
                
            if (GUILayout.Button("Start Client (Player Y)", GUILayout.Width(200), GUILayout.Height(50)))
            {
                NetworkManager.Singleton.StartClient();
            }
        }

        GUILayout.EndArea();
    }
}