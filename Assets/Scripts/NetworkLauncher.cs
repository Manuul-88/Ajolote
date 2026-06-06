using Unity.Netcode;
using UnityEngine;

public class NetworkLauncher : MonoBehaviour
{
    private void OnGUI()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsServer)
        {
            if (GUI.Button(new Rect(10, 10, 120, 40), "Host"))
            {
                NetworkManager.Singleton.StartHost();
            }

            if (GUI.Button(new Rect(10, 60, 120, 40), "Client"))
            {
                NetworkManager.Singleton.StartClient();
            }
        }
    }
}