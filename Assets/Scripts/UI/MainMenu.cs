using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public GameObject gameStartButton;
    public GameObject exitButton;
    public GameObject backToMainButton;
    public GameObject mapScrollView;
    public GameObject mapExplainView;

    void Start()
    {
        BackToMain();
    }

    public void GameStart()
    {
        gameStartButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);
        backToMainButton.gameObject.SetActive(true);
        mapScrollView.gameObject.SetActive(true);
        mapExplainView.gameObject.SetActive(true);
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void BackToMain()
    {
        gameStartButton.gameObject.SetActive(true);
        exitButton.gameObject.SetActive(true);
        backToMainButton.gameObject.SetActive(false);
        mapScrollView.gameObject.SetActive(false);
        mapExplainView.gameObject.SetActive(false);
    }
}
