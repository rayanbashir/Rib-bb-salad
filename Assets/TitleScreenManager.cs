using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DestroyDontDestroyOnLoadObjects();
    }
    // Destroys all objects in the DontDestroyOnLoad scene
    private void DestroyDontDestroyOnLoadObjects()
    {
        // Create a temporary GameObject to get the DontDestroyOnLoad scene
        GameObject temp = new GameObject();
        Scene dontDestroyOnLoad = temp.scene;
        Object.DontDestroyOnLoad(temp);
        dontDestroyOnLoad = temp.scene;
        // Get all root objects in the DontDestroyOnLoad scene
        List<GameObject> dontDestroyObjects = new List<GameObject>();
        foreach (GameObject obj in dontDestroyOnLoad.GetRootGameObjects())
        {
            if (obj != temp) // Don't destroy the temp object yet
            {
                Destroy(obj);
            }
        }
        // Destroy the temp object itself
        Destroy(temp);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    // Reference to the blackfade UI object's animator
    public Animator blackfadeAnimator;

    // Switches the scene to "Scene1" with fade out and waits for animation
    public void SwitchToScene1()
    {
        StartCoroutine(FadeAndSwitchScene());
    }

    private IEnumerator FadeAndSwitchScene()
    {
        if (blackfadeAnimator != null)
        {
            blackfadeAnimator.SetTrigger("FadeOut");
            // Wait until the fade out animation finishes
            // Assumes the fade animation is 1 second long; adjust as needed
            yield return new WaitForSeconds(1f);
        }
        SceneManager.LoadScene("Scene1");
    }
}
