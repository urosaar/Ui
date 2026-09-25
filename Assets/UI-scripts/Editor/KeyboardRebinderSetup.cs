using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-click tool: copy the exact look of inventory-rebinder (label text, button box,
// and Default-Key text) into every other *-rebinder child of the Keyboard-panel.
//
// Menu: UI Tools > Copy inventory-rebinder to all keyboard rebinders
//
// What it copies per rebinder (keeping each row's OWN label text / key text):
//   - Row button box sizeDelta          -> inventory's (keeps each row's position)
//   - "Text (TMP)" rect + TMP style     -> inventory's label
//   - "Default-Key" rect + TMP style    -> inventory's key text
//   - child order (label first, key second)
//   - UIKeyBindTester wiring (rebindButton -> own Button, buttonText -> Default-Key TMP)
public static class KeyboardRebinderSetup
{
    private const string MenuRoot = "UI Tools/";

    [MenuItem(MenuRoot + "Copy inventory-rebinder to all keyboard rebinders", false, 1)]
    public static void CopyToAllRebinders()
    {
        Transform panel = FindObjectByName("Keyboard-panel");
        if (panel == null)
        {
            EditorUtility.DisplayDialog("Keyboard UI",
                "Could not find a GameObject named 'Keyboard-panel' in the open scene.",
                "OK");
            return;
        }

        Transform template = FindChild(panel, "inventory-rebinder");
        TextMeshProUGUI tplLabel = ChildTmp(template, "Text (TMP)");
        TextMeshProUGUI tplKey = ChildTmp(template, "Default-Key");
        if (template == null || tplLabel == null || tplKey == null)
        {
            EditorUtility.DisplayDialog("Keyboard UI",
                "Could not find inventory-rebinder with 'Text (TMP)' and 'Default-Key' (TMP) children.",
                "OK");
            return;
        }

        RectTransform tplRowRt = template.GetComponent<RectTransform>();
        List<string> done = new List<string>();
        List<string> skipped = new List<string>();

        foreach (Transform child in panel)
        {
            if (!child.name.EndsWith("-rebinder") || child.name == "inventory-rebinder") continue;

            RectTransform rowRt = child.GetComponent<RectTransform>();
            RectTransform labelRt = child.Find("Text (TMP)") as RectTransform;
            RectTransform keyRt = child.Find("Default-Key") as RectTransform;
            TextMeshProUGUI labelTmp = labelRt != null ? labelRt.GetComponent<TextMeshProUGUI>() : null;
            TextMeshProUGUI keyTmp = keyRt != null ? keyRt.GetComponent<TextMeshProUGUI>() : null;

            if (rowRt == null || labelRt == null || keyRt == null || labelTmp == null || keyTmp == null)
            {
                skipped.Add(child.name);
                continue;
            }

            // 1. Button box: same size as inventory (position untouched)
            Undo.RecordObject(rowRt, "Match rebinder box");
            rowRt.sizeDelta = tplRowRt.sizeDelta;

            // 2. Label text
            CopyRect(tplLabel.rectTransform, labelRt);
            CopyTmpProps(tplLabel, labelTmp);

            // 3. Key text
            CopyRect(tplKey.rectTransform, keyRt);
            CopyTmpProps(tplKey, keyTmp);

            // 4. Child order like inventory: label first, key second
            Undo.RecordObject(rowRt, "Match rebinder child order");
            labelRt.SetAsFirstSibling();
            keyRt.SetAsLastSibling();

            // 5. Wiring safety net (rebindButton -> own Button, buttonText -> Default-Key TMP)
            WireRebinder(child, keyTmp);

            done.Add(child.name);
        }

        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        EditorSceneManager.SaveScene(panel.gameObject.scene);

        string message = "Matched " + done.Count + " rebinder(s) to inventory-rebinder:\n  " +
                         string.Join("\n  ", done);
        if (skipped.Count > 0)
        {
            message += "\n\nSkipped (missing children/TMP):\n  " + string.Join("\n  ", skipped);
        }
        message += "\n\nEverything is undoable (Ctrl+Z).";
        EditorUtility.DisplayDialog("Keyboard UI", message, "OK");
    }

    // ------------------------------------------------------------------ helpers

    private static void WireRebinder(Transform row, TextMeshProUGUI keyTmp)
    {
        UIKeyBindTester rebinder = row.GetComponent<UIKeyBindTester>();
        if (rebinder == null)
        {
            rebinder = Undo.AddComponent<UIKeyBindTester>(row.gameObject);
        }

        SerializedObject so = new SerializedObject(rebinder);
        so.FindProperty("rebindButton").objectReferenceValue = row.GetComponent<Button>();
        so.FindProperty("buttonText").objectReferenceValue = keyTmp;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyRect(RectTransform from, RectTransform to)
    {
        Undo.RecordObject(to, "Match text rect");
        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.anchoredPosition = from.anchoredPosition;
        to.sizeDelta = from.sizeDelta;
        to.pivot = from.pivot;
        to.localScale = from.localScale;
        to.localRotation = from.localRotation;
    }

    // Copies every TMP property except the text itself (so each row keeps its own
    // label like "Zoom" and its own key like "Right mouse"), plus identity fields.
    private static void CopyTmpProps(TextMeshProUGUI from, TextMeshProUGUI to)
    {
        Undo.RecordObject(to, "Match TMP style");
        SerializedObject src = new SerializedObject(from);
        SerializedObject dst = new SerializedObject(to);
        HashSet<string> skip = new HashSet<string>
        {
            "m_ObjectHideFlags",
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset",
            "m_GameObject",
            "m_Enabled",
            "m_EditorHideFlags",
            "m_Script",
            "m_text",
        };

        SerializedProperty p = src.GetIterator();
        while (p.NextVisible(true))
        {
            if (skip.Contains(p.name)) continue;
            if (dst.FindProperty(p.name) != null)
            {
                dst.CopyFromSerializedProperty(p);
            }
        }
        dst.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TextMeshProUGUI ChildTmp(Transform parent, string childName)
    {
        if (parent == null) return null;
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
        }
        return null;
    }

    // Finds a GameObject by name anywhere in the scene, including inactive ones.
    private static Transform FindObjectByName(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == name) return t;
        }
        return null;
    }
}