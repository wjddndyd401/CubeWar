using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public Image backGround;
    Color originalColor;

    void Start()
    {
        originalColor = backGround.color;
        StartCoroutine(FadeOut(1));
        GameManager.isGameOver = true;
    }

    IEnumerator FadeOut(float time)
    {
        float percent = 0;
        while(percent < 1)
        {
            percent += Time.deltaTime / time;
            backGround.color = Color.Lerp(Color.clear, originalColor, percent);
            yield return null;
        }
    }

    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Menu()
    {
        SceneManager.LoadScene("Menu");
    }
}
