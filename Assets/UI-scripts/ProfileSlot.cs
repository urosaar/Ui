using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One of these per profile row (Hollow Knight style).
// - No save on disk yet  -> shows the NEW GAME button
// - Save exists on disk   -> hides it and shows the CONTINUE button instead
//
// Clicking NEW GAME creates the folder + save.json via ProfileSystem, then flips
// this row to CONTINUE and loads the assigned scene (if any).
//
// IMPORTANT: attach this to the ROW (an empty parent GameObject holding both
// buttons), NOT to one of the buttons themselves. Otherwise deactivating a
// button would deactivate this script and the row would break.
public class ProfileSlot : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Profile number for this row: 1, 2 or 3. This decides which Profile-N folder is used on disk.")]
    [SerializeField] private int profileNumber = 1;

    [Header("Buttons")]
    [Tooltip("Drag the New Game button of this row here. The script adds its own click listener.")]
    [SerializeField] private Button newGameButton;
    [Tooltip("Drag the Continue button of this row here. Keep it ACTIVE in the scene, the script hides/shows it.")]
    [SerializeField] private Button continueButton;

    [Header("Optional Display")]
    [Tooltip("Optional TMP text on the Continue button that shows the last played time. Can be left empty.")]
    [SerializeField] private TMP_Text continueInfoText;
    [Tooltip("Optional TMP text used to show errors (e.g. 'Could not create profile'). Can be left empty.")]
    [SerializeField] private TMP_Text statusText;

    [Header("Scene To Load")]
    [Tooltip("Exact scene name to load (as shown in File > Build Settings). Leave EMPTY while you have no scene yet.")]
    [SerializeField] private string sceneToLoad;

    private void Awake()
    {
        // Attach here, the script so it keeps working. If not attached to the row
        // this silently lands on whichever button holds this component.
        if (newGameButton == null) newGameButton = GetComponent<Button>();

        // Wire clicks in code so nothing needs to be dragged in the inspector's OnClick
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void Start()
    {
        // Decide New Game vs Continue based on what actually exists on disk
        RefreshState();
    }

    // Shows the correct button for the current on-disk state. Also public so you
    // can call it from elsewhere (e.g. after a rename/delete from another screen).
    public void RefreshState()
    {
        bool exists = ProfileSystem.ProfileExists(profileNumber);

        if (newGameButton != null) newGameButton.gameObject.SetActive(!exists);
        if (continueButton != null) continueButton.gameObject.SetActive(exists);

        if (continueInfoText != null)
        {
            if (exists)
            {
                ProfileData data = ProfileSystem.LoadProfileData(profileNumber);
                continueInfoText.text = data != null ? "Last played: " + data.lastPlayed : "Continue";
            }
            else
            {
                continueInfoText.text = "";
            }
        }
    }

    // Wired to the New Game button. Creates the profile on disk, flips to
    // Continue, then loads the assigned scene (no-op until a scene is set).
    public void OnNewGameClicked()
    {
        bool created = ProfileSystem.CreateProfile(profileNumber);

        if (created)
        {
            if (statusText != null) statusText.text = "Profile " + profileNumber + " created.";
            RefreshState();
            LoadAssignedScene();
        }
        else
        {
            if (statusText != null) statusText.text = "Could not create profile. Check the console.";
            Debug.LogError("[ProfileSlot] Failed to create profile " + profileNumber + " (see ProfileSystem errors above).");
        }
    }

    // Wired to the Continue button. Just loads the scene; the save already exists.
    public void OnContinueClicked()
    {
        // Guard: save got deleted on disk while running -> fall back to New Game
        if (!ProfileSystem.ProfileExists(profileNumber))
        {
            Debug.Log("[ProfileSlot] Profile " + profileNumber + " no longer exists on disk, showing New Game.");
            RefreshState();
            return;
        }

        LoadAssignedScene();
    }

    // Editor helpers: right-click the component header to use these.

    [ContextMenu("Refresh State")]
    public void RefreshSlotFromInspector()
    {
        RefreshState();
    }

    [ContextMenu("Delete Profile (disk)")]
    public void DeleteProfileFromInspector()
    {
        if (ProfileSystem.DeleteProfile(profileNumber))
        {
            if (statusText != null) statusText.text = "Profile " + profileNumber + " deleted.";
            RefreshState();
        }
    }

    private void LoadAssignedScene()
    {
        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.Log("[ProfileSlot] Profile " + profileNumber + " ready. Assign a scene name in the inspector + add it to Build Settings to actually start the game.");
            return;
        }

        // Warn instead of silently failing if the scene isn't in Build Settings
#pragma warning disable 0618 // CanStreamedLevelBeLoaded is the name-based check that matches LoadScene(name)
        bool inBuildSettings = Application.CanStreamedLevelBeLoaded(sceneToLoad);
#pragma warning restore 0618

        if (!inBuildSettings)
        {
            Debug.LogWarning("[ProfileSlot] Scene '" + sceneToLoad + "' is not in Build Settings. Add it via File > Build Settings > Add Open Scenes.");
            return;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}