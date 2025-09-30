using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string savePath = Application.persistentDataPath + "/playerSave.json";

    public static void SavePlayer(PlayerSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    public static PlayerSaveData LoadPlayer()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            return JsonUtility.FromJson<PlayerSaveData>(json);
        }
        return null;
    }
}