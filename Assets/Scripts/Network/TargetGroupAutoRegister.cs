using UnityEngine;
using Unity.Netcode; // <-- Necesario para la red

public class TargetGroupAutoRegister : NetworkBehaviour // <-- Cambiamos MonoBehaviour por NetworkBehaviour
{
    private MultiplayerTargetGroupManager manager;

    // Usamos OnNetworkSpawn en lugar de Start
    public override void OnNetworkSpawn()
    {
        manager = FindFirstObjectByType<MultiplayerTargetGroupManager>();

        if (manager != null)
        {
            manager.RegisterTarget(transform);
        }
    }

    // Usamos OnNetworkDespawn en lugar de OnDestroy
    public override void OnNetworkDespawn()
    {
        if (manager != null)
        {
            manager.UnregisterTarget(transform);
        }
    }
}