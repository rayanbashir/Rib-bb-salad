using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    public string SceneName;
    public Animator animator;
    public Movement playerMovement;

    // Start is called before the first frame update
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetTrigger("FadeOut");
            playerMovement.canMove = false;
            Invoke("SceneSwitch", 1.5f);
        }
    }
    public void SceneSwitch ()
    {
        SceneManager.LoadScene(SceneName);
    }
  
}
