using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Tools > Events > Clean All UnityEvents (Scenes + Prefabs)
// - Removes only corrupt entries (missing required sub-properties) or listeners with a method name but null target.
// - Keeps empty WIP rows (no method selected yet) so it won't fight you while editing.
public static class UnityEventsCleaner
{
    [MenuItem("Tools/Events/Clean All UnityEvents (Scenes + Prefabs)")]
    public static void CleanAllUnityEvents()
    {
        int totalRemoved = 0;

        // Clean open scenes
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            totalRemoved += CleanScene(scene);
        }

        // Clean all scenes in Assets
        var sceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (var guid in sceneGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsSceneOpen(path)) continue; // already cleaned
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            int removed = CleanScene(scene);
            totalRemoved += removed;
            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.CloseScene(scene, true);
        }

        // Clean all prefabs
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            int removed = 0;
            foreach (var comp in root.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue; // missing scripts; skip
                removed += CleanObjectUnityEvents(comp);
            }

            if (removed > 0)
            {
                totalRemoved += removed;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("UnityEvents Cleaner",
            $"Cleanup finished. Removed {totalRemoved} corrupt/broken persistent listener(s).",
            "OK");
    }

    private static bool IsSceneOpen(string assetPath)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.path == assetPath) return true;
        }
        return false;
    }

    private static int CleanScene(Scene scene)
    {
        int removed = 0;
        if (!scene.isLoaded) return 0;
        foreach (var go in scene.GetRootGameObjects())
        {
            foreach (var comp in go.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                removed += CleanObjectUnityEvents(comp);
            }
        }
        return removed;
    }

    private static int CleanObjectUnityEvents(UnityEngine.Object obj)
    {
        int removed = 0;
        var so = new SerializedObject(obj);
        var it = so.GetIterator();
        bool enterChildren = true;
        while (it.Next(enterChildren))
        {
            enterChildren = true;
            var calls = it.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls == null || !calls.isArray) continue;

            bool changed = false;
            for (int i = calls.arraySize - 1; i >= 0; i--)
            {
                var call = calls.GetArrayElementAtIndex(i);
                if (call == null)
                {
                    calls.DeleteArrayElementAtIndex(i);
                    removed++; changed = true; continue;
                }

                var targetProp = call.FindPropertyRelative("m_Target");
                var methodProp = call.FindPropertyRelative("m_MethodName");
                var argsProp = call.FindPropertyRelative("m_Arguments");
                var modeProp = call.FindPropertyRelative("m_Mode");

                // Remove if required sub-properties missing (corrupt entry)
                if (targetProp == null || methodProp == null || argsProp == null || modeProp == null)
                {
                    calls.DeleteArrayElementAtIndex(i);
                    removed++; changed = true; continue;
                }

                var target = targetProp.objectReferenceValue;
                var method = methodProp.stringValue;

                // Remove if a specific method is set but the target is gone (clearly broken)
                if (!string.IsNullOrEmpty(method) && target == null)
                {
                    calls.DeleteArrayElementAtIndex(i);
                    removed++; changed = true; continue;
                }

                // Keep empty WIP rows (both method and target empty) so we don't fight user edits.
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(obj);
            }
        }
        return removed;
    }
}
