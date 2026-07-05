using UnityEngine;

public class TargetGroupAutoRegister : MonoBehaviour
{
    private MultiplayerTargetGroupManager manager;

    void Start()
    {
        manager = FindFirstObjectByType<MultiplayerTargetGroupManager>();

        if (manager != null)
        {
            manager.RegisterTarget(transform);
        }
    }

    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.UnregisterTarget(transform);
        }
    }
}