using System;
using System.IO;
using UnityEngine;

// The data + disk layer of the profile system. The UI (ProfileSlot) calls this,
// and later on your actual game will call this too to save/load player progress.
//
// Where data lives:
//   Documents/Undeved/Class8/Profiles/Profile-1/save.json
//   Documents/Undeved/Class8/Profiles/Profile-2/save.json
//   Documents/Undeved/Class8/Profiles/Profile-3/save.json
//
// Saving over an existing profile simply overwrites its save.json.
// A profile "exists" only if its folder AND save.json both exist, so you can
// reset a slot by deleting the folder on disk.
public static class ProfileSystem
{
    public const string AppFolderName = "Undeved";
    public const string ClassFolderName = "Class8";
    public const string ProfilesFolderName = "Profiles";
    public const string ProfileFolderPrefix = "Profile-";
    public const string SaveFileName = "save.json";

    // The user's real Documents folder (C:\Users\<name>\Documents on Windows)
    private static string DocumentsPath =>
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    // Full path to .../Documents/Undeved/Class8/Profiles
    public static string ProfilesRootPath =>
        Path.Combine(DocumentsPath, AppFolderName, ClassFolderName, ProfilesFolderName);

    // .../Profiles/Profile-3
    public static string GetProfileFolderPath(int profileNumber) =>
        Path.Combine(ProfilesRootPath, ProfileFolderPrefix + profileNumber);

    // .../Profiles/Profile-3/save.json
    public static string GetSaveFilePath(int profileNumber) =>
        Path.Combine(GetProfileFolderPath(profileNumber), SaveFileName);

    // A profile counts as existing only when its folder AND save file are both there
    public static bool ProfileExists(int profileNumber)
    {
        return Directory.Exists(GetProfileFolderPath(profileNumber))
               && File.Exists(GetSaveFilePath(profileNumber));
    }

    // Creates the folder + a fresh default save for a brand new game.
    // Safe to call even if the folder already exists. Returns false on failure.
    public static bool CreateProfile(int profileNumber)
    {
        return SaveProfileData(profileNumber, ProfileData.CreateNew(profileNumber));
    }

    // Writes (or overwrites) this profile's save file on disk.
    // This is the method your game will call later with real progress data.
    public static bool SaveProfileData(int profileNumber, ProfileData data)
    {
        try
        {
            if (data == null)
            {
                Debug.LogError("[ProfileSystem] Tried to save null data.");
                return false;
            }

            // Creates Documents/Undeved/Class8/Profiles (all levels at once).
            // No error if they already exist.
            Directory.CreateDirectory(GetProfileFolderPath(profileNumber));

            // Touch the timestamp every save, then overwrite the file
            data.lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(GetSaveFilePath(profileNumber), json);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[ProfileSystem] Failed to save profile " + profileNumber + ": " + e.Message);
            return false;
        }
    }

    // Reads a profile back from disk, or null if it doesn't exist / is corrupt
    public static ProfileData LoadProfileData(int profileNumber)
    {
        try
        {
            string path = GetSaveFilePath(profileNumber);
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            ProfileData data = JsonUtility.FromJson<ProfileData>(json);

            if (data == null)
            {
                Debug.LogWarning("[ProfileSystem] save.json for profile " + profileNumber + " was empty or corrupt.");
            }
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError("[ProfileSystem] Failed to load profile " + profileNumber + ": " + e.Message);
            return null;
        }
    }

    // Deletes the whole profile folder (used by the "Delete Profile" context menu,
    // and later by an optional in-game "delete save" option).
    public static bool DeleteProfile(int profileNumber)
    {
        try
        {
            string folder = GetProfileFolderPath(profileNumber);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
                Debug.Log("[ProfileSystem] Deleted profile " + profileNumber + " at " + folder);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[ProfileSystem] Failed to delete profile " + profileNumber + ": " + e.Message);
            return false;
        }
    }
}

// The actual save data. Add more fields here later (health, area, items, ...)
// and it will automatically be serialized next time the game saves.
[Serializable]
public class ProfileData
{
    public string profileId;      // "Profile-1"
    public string displayName;    // "Profile 1"
    public string createdAt;      // when the save was first made
    public string lastPlayed;     // updated on every save
    public string currentScene;   // scene name to continue from

    [Tooltip("Reserved: your game can stuff any extra progress JSON into this without changing ProfileData.")]
    public string customDataJson;

    public static ProfileData CreateNew(int profileNumber)
    {
        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return new ProfileData
        {
            profileId = ProfileSystem.ProfileFolderPrefix + profileNumber,
            displayName = "Profile " + profileNumber,
            createdAt = now,
            lastPlayed = now,
            currentScene = "",
            customDataJson = ""
        };
    }
}