using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameMenu : MonoBehaviour
{
    bool isPause = false;
    public Text pauseResumeText;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Continue()
    {
        gameObject.SetActive(false);
    }

    public void PauseResume()
    {
        if(isPause)
        {
            // Resume
            Time.timeScale = 1;
            isPause = false;
            pauseResumeText.text = "일시정지";
        } else
        {
            // Pause
            Time.timeScale = 0;
            isPause = true;
            pauseResumeText.text = "게임 재개";
        }
    }

    public void ReturnToMain()
    {
        SceneManager.LoadScene("Menu");
    }

    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
