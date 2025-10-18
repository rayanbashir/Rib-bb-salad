using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMainMenuScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Loads the scene named exactly "Main Menu". Call this from UI button OnClick.
    public void ReturnToMainMenu()
    {
        const string sceneName = "Main Menu";
        if (!IsSceneInBuildSettings(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' is not in Build Settings. Add it there or change the scene name.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // Async variant that optionally reports progress via a coroutine. Useful for showing a loading bar.
    public void ReturnToMainMenuAsync()
    {
        const string sceneName = "Main Menu";
        if (!IsSceneInBuildSettings(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' is not in Build Settings. Add it there or change the scene name.");
            return;
        }

        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            // op.progress is 0..0.9 while loading, then becomes 1.0 when activation completes
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            // You can hook this progress value to a UI progress bar.
            // Debug.Log($"Loading progress: {progress:0.00}");
            yield return null;
        }
    }

    bool IsSceneInBuildSettings(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }
}
