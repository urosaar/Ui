using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-click scene setup for the Hollow Knight-style profile rows.
// Menu: Profile System > Setup Profile Rows in Scene
//
// Creates (or repairs) one ProfileRow-N per profile slot:
//   - New Game slot  = the existing profile-bg (+ profile-bg-hover)
//   - Continue slot  = a clone using Continue.png / Continue-hovered.png, hidden until a save exists
//   - ProfileSlot    = attached to the row, drives New Game <-> Continue switching
//
// IMPORTANT transform note: the Continue clone is made to EXACTLY overlap the
// New Game slot by copying the slot's LOCAL RectTransform values (same parent =>
// same screen position). Reparenting UI with worldPositionStays=true double-applies
// scale when parents use non-1 scale and scrambles anchored values - avoid it.
public static class ProfileSceneSetup
{
    private const string MenuRoot = "Profile System/";

    [MenuItem(MenuRoot + "Setup Profile Rows in Scene", false, 1)]
    public static void SetupScene()
    {
        Transform holder = FindHolder();
        if (holder == null)
        {
            EditorUtility.DisplayDialog("Profile System",
                "Could not find the profile rows container.\n\nExpected: Game-panel > holder (holding your profile-bg buttons).",
                "OK");
            return;
        }

        Sprite continueSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Class-8/Continue.png");
        Sprite continueHoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Class-8/Continue-hovered.png");
        if (continueSprite == null || continueHoverSprite == null)
        {
            EditorUtility.DisplayDialog("Profile System",
                "Could not load Assets/Class-8/Continue.png or Continue-hovered.png.\nMake sure those files exist and are imported as Sprites.",
                "OK");
            return;
        }

        // ---------- Case A: rows already exist -> repair their transforms ----------
        List<Transform> existingRows = new List<Transform>();
        foreach (Transform child in holder)
        {
            if (child.name.StartsWith("ProfileRow-"))
            {
                existingRows.Add(child);
            }
        }

        if (existingRows.Count > 0)
        {
            int repaired = 0;
            foreach (Transform row in existingRows)
            {
                RowParts parts = FindRowParts(row);
                if (parts.newGame == null || parts.continueBtn == null)
                {
                    Debug.LogWarning("[ProfileSceneSetup] " + row.name + " is incomplete - skipped. Delete it and re-run setup.");
                    continue;
                }

                // Make Continue overlap the New Game slot / hover exactly
                CopyLocalRect(parts.newGame.GetComponent<RectTransform>(), parts.continueBtn.GetComponent<RectTransform>());
                if (parts.continueHover != null && parts.hover != null)
                {
                    CopyLocalRect(parts.hover.GetComponent<RectTransform>(), parts.continueHover.GetComponent<RectTransform>());
                }

                EnsureSprite(parts.continueBtn, continueSprite);
                if (parts.continueHover != null) EnsureSprite(parts.continueHover, continueHoverSprite);

                EditorUtility.SetDirty(row.gameObject);
                repaired++;
            }

            EditorSceneManager.MarkSceneDirty(holder.gameObject.scene);
            EditorSceneManager.SaveScene(holder.gameObject.scene);
            EditorUtility.DisplayDialog("Profile System",
                "Repaired " + repaired + " existing row(s).\n\nThe Continue buttons now sit exactly on top of their New Game slots.\nEverything is undoable (Ctrl+Z).",
                "OK");
            return;
        }

        // ---------- Case B: first run -> create the rows ----------
        List<GameObject> slots = new List<GameObject>();
        foreach (Transform child in holder)
        {
            if (child.name == "profile-bg" || child.name.StartsWith("profile-bg ("))
            {
                slots.Add(child.gameObject);
            }
        }

        if (slots.Count == 0)
        {
            EditorUtility.DisplayDialog("Profile System",
                "No 'profile-bg' buttons found under 'holder' (Game-panel). Add them or check the hierarchy.",
                "OK");
            return;
        }

        // Sort top-to-bottom, then left-to-right. Top slot = Profile 1.
        slots.Sort((a, b) =>
        {
            int byY = b.transform.position.y.CompareTo(a.transform.position.y);
            return byY != 0 ? byY : a.transform.position.x.CompareTo(b.transform.position.x);
        });

        int wired = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            GameObject bg = slots[i];
            GameObject hover = FindChildByName(holder, HoverNameFor(bg.name));
            if (hover == null)
            {
                Debug.LogWarning("[ProfileSceneSetup] No matching profile-bg-hover for '" + bg.name + "' - skipped.");
                continue;
            }

            CreateRow(holder, bg, hover, i + 1, continueSprite, continueHoverSprite);
            wired++;
        }

        EditorSceneManager.MarkSceneDirty(holder.gameObject.scene);
        EditorSceneManager.SaveScene(holder.gameObject.scene);

        EditorUtility.DisplayDialog("Profile System",
            "Done! Wired " + wired + " profile row(s).\n\n- Profile 1.." + slots.Count + " assigned top-to-bottom by slot position.\n- Continue buttons start hidden; click New Game in Play mode to create a profile.\n- Everything is undoable (Ctrl+Z).",
            "OK");
    }

    [MenuItem(MenuRoot + "Delete ALL Profile Data On Disk", false, 2)]
    public static void DeleteAllProfiles()
    {
        if (!EditorUtility.DisplayDialog("Profile System",
            "This deletes " + ProfileSystem.ProfilesRootPath + " entirely.\n\nContinue?", "Delete", "Cancel"))
        {
            return;
        }

        int deleted = 0;
        for (int n = 1; n <= 3; n++)
        {
            if (ProfileSystem.DeleteProfile(n)) deleted++;
        }
        Debug.Log("[ProfileSystem] Deleted " + deleted + " profile folder(s).");
    }

    [MenuItem(MenuRoot + "Reveal Profiles Folder", false, 3)]
    public static void RevealProfilesFolder()
    {
        EditorUtility.RevealInFinder(ProfileSystem.ProfilesRootPath);
    }

    // ------------------------------------------------------------------ helpers

    private class RowParts
    {
        public Transform newGame;
        public Transform hover;
        public Transform continueBtn;
        public Transform continueHover;
    }

    private static RowParts FindRowParts(Transform row)
    {
        RowParts parts = new RowParts();
        foreach (Transform child in row)
        {
            if (child.name.StartsWith("profile-bg") && !child.name.StartsWith("profile-bg-hover"))
            {
                parts.newGame = child;
            }
            else if (child.name.StartsWith("profile-bg-hover"))
            {
                parts.hover = child;
            }
            else if (child.name.StartsWith("Continue-") && !child.name.EndsWith("-Hover"))
            {
                parts.continueBtn = child;
            }
            else if (child.name.StartsWith("Continue-") && child.name.EndsWith("-Hover"))
            {
                parts.continueHover = child;
            }
        }
        return parts;
    }

    private static void EnsureSprite(Transform target, Sprite sprite)
    {
        Image img = target.GetComponent<Image>();
        if (img != null && img.sprite != sprite)
        {
            img.sprite = sprite;
        }
    }

    private static void CreateRow(Transform holder, GameObject bg, GameObject hover,
        int number, Sprite continueSprite, Sprite continueHoverSprite)
    {
        string rowName = "ProfileRow-" + number;

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        RectTransform hoverRect = hover.GetComponent<RectTransform>();

        // 1. Row container with the exact same rect as the slot (in holder space)
        GameObject rowGo = new GameObject(rowName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(rowGo, "Create " + rowName);
        RectTransform rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.SetParent(holder, false);
        CopyRect(bgRect, rowRect);

        // 2. Clone the slot + hover BEFORE moving anything (clean local values)
        GameObject continueGo = Object.Instantiate(bg);
        continueGo.name = "Continue-" + number;
        Undo.RegisterCreatedObjectUndo(continueGo, "Create Continue-" + number);

        GameObject continueHoverGo = Object.Instantiate(hover);
        continueHoverGo.name = "Continue-" + number + "-Hover";
        Undo.RegisterCreatedObjectUndo(continueHoverGo, "Create " + continueHoverGo.name);

        // 3. Local-preserving reparent: keep local values, then force them to match.
        //    worldPositionStays:true would recompute through canvas scales and scramble UI.
        bg.transform.SetParent(rowRect, false);
        hover.transform.SetParent(rowRect, false);
        continueGo.transform.SetParent(rowRect, false);
        continueHoverGo.transform.SetParent(rowRect, false);

        CopyRect(bgRect, continueGo.GetComponent<RectTransform>());
        CopyRect(hoverRect, continueHoverGo.GetComponent<RectTransform>());

        // 4. Drop cloned HoverReveal (it points at the original slot objects)
        HoverReveal oldReveal = continueGo.GetComponent<HoverReveal>();
        if (oldReveal != null)
        {
            Undo.DestroyObjectImmediate(oldReveal);
        }

        Image continueImage = continueGo.GetComponent<Image>();
        if (continueImage != null) continueImage.sprite = continueSprite;

        Image continueHoverImage = continueHoverGo.GetComponent<Image>();
        if (continueHoverImage != null) continueHoverImage.sprite = continueHoverSprite;

        // 5. Wire hover reveal on the Continue button like the original slot
        HoverReveal sourceReveal = bg.GetComponent<HoverReveal>();
        if (sourceReveal != null)
        {
            HoverReveal newReveal = Undo.AddComponent<HoverReveal>(continueGo);
            SerializedObject hrSo = new SerializedObject(newReveal);
            SerializedObject srcSo = new SerializedObject(sourceReveal);
            hrSo.FindProperty("elementToActivate").objectReferenceValue = continueHoverGo;
            hrSo.FindProperty("imageToHide").objectReferenceValue = continueImage;
            hrSo.FindProperty("hoverScale").floatValue = srcSo.FindProperty("hoverScale").floatValue;
            hrSo.FindProperty("hoverOffsetUp").floatValue = srcSo.FindProperty("hoverOffsetUp").floatValue;
            hrSo.FindProperty("transitionDuration").floatValue = srcSo.FindProperty("transitionDuration").floatValue;
            hrSo.FindProperty("revealedElementBlocksRaycasts").boolValue = srcSo.FindProperty("revealedElementBlocksRaycasts").boolValue;
            hrSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // 6. Continue starts hidden - ProfileSlot shows it only once a save exists
        continueGo.SetActive(false);
        continueHoverGo.SetActive(false);

        // 7. Wire ProfileSlot
        ProfileSlot slot = Undo.AddComponent<ProfileSlot>(rowGo);
        SerializedObject slotSo = new SerializedObject(slot);
        slotSo.FindProperty("profileNumber").intValue = number;
        slotSo.FindProperty("newGameButton").objectReferenceValue = bg.GetComponent<Button>();
        slotSo.FindProperty("continueButton").objectReferenceValue = continueGo.GetComponent<Button>();
        slotSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(rowGo);
        Debug.Log("[ProfileSceneSetup] Wired " + rowName + " (profile #" + number + "): " +
                  "New Game = " + bg.name + ", Continue = " + continueGo.name);
    }

    // Finds "holder" even if it (or Game-panel) is inactive in the editor.
    private static Transform FindHolder()
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "holder" && t.parent != null && t.parent.name == "Game-panel")
            {
                return t;
            }
        }
        return null;
    }

    private static string HoverNameFor(string bgName)
    {
        if (bgName == "profile-bg") return "profile-bg-hover";
        int idx = bgName.IndexOf('(');
        if (idx >= 0) return "profile-bg-hover " + bgName.Substring(idx);
        return "profile-bg-hover";
    }

    private static GameObject FindChildByName(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.gameObject : null;
    }

    // Copies the full layout rect: anchors, position, size, pivot + transform.
    private static void CopyRect(RectTransform from, RectTransform to)
    {
        CopyLocalRect(from, to);
    }

    private static void CopyLocalRect(RectTransform from, RectTransform to)
    {
        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.anchoredPosition = from.anchoredPosition;
        to.sizeDelta = from.sizeDelta;
        to.pivot = from.pivot;
        to.localScale = from.localScale;
        to.localRotation = from.localRotation;
    }
}