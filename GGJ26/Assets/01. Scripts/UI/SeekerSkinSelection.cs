using UnityEngine;

public static class SeekerSkinSelection
{
    private const string Key = "GGJ26.SeekerSkinIndex";

    public static int LoadSelectedSkinIndex(int fallback = 0)
    {
        return PlayerPrefs.GetInt(Key, fallback);
    }

    public static void SaveSelectedSkinIndex(int skinIndex)
    {
        PlayerPrefs.SetInt(Key, Mathf.Max(0, skinIndex));
        PlayerPrefs.Save();
    }
}
