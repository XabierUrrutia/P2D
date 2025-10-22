using UnityEngine;

public static class PlayerPositionManager
{
    public static Vector3 SavedPosition = Vector3.zero;
    public static bool HasSavedPosition = false;

    public static void SavePosition(Vector3 position)
    {
        SavedPosition = position;
        HasSavedPosition = true;
        Debug.Log($"Posição guardada: {position}");
    }

    public static Vector3 GetPosition()
    {
        return SavedPosition;
    }
}
