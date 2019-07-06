using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOver : MonoBehaviour
{
    public WinType winType;
    public GameObject[] targetForDestroy;
    public float minimumTime;
    public GameObject winScreen;

    public LoseType loseType;
    public GameObject[] targetForProtect;
    public float timeLimit;
    public GameObject loseScreen;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bool isWon = true;
        switch (winType)
        {
            case WinType.Annihilation:
                for(int i = 0; i < GameManager.selectableObjects.Count; i++)
                {
                    if (GameManager.selectableObjects[i].IsStructure() && GameManager.selectableObjects[i].ownerName == Global.enemyPlayer.name)
                        isWon = false;
                }
                break;
            case WinType.TargetDestroy:
                for(int i = 0; i < targetForDestroy.Length; i++)
                {
                    if (targetForDestroy[i] != null) isWon = false;
                }
                break;
            case WinType.Defence:
                if(minimumTime > GameManager.progressTime)
                {
                    isWon = false;
                }
                break;
        }

        bool isLost = true;
        switch (loseType)
        {
            case LoseType.Annihilation:
                for (int i = 0; i < GameManager.selectableObjects.Count; i++)
                {
                    if (GameManager.selectableObjects[i].IsStructure() && GameManager.selectableObjects[i].ownerName == Global.gamePlayer.name)
                        isLost = false;
                }
                break;
            case LoseType.TargetDestroy:
                for (int i = 0; i < targetForProtect.Length; i++)
                {
                    if (targetForProtect[i] != null) isLost = false;
                }
                break;
            case LoseType.TimeLimit:
                if (timeLimit > GameManager.progressTime)
                {
                    isLost = false;
                }
                break;
        }

        // 승리하였을 경우, 패배 조건을 만족한 상태라도 승리
        if (isWon)
        {
            winScreen.SetActive(true);
            GameManager.DeActivateAllObjects();
        } else if(isLost)
        {
            loseScreen.SetActive(true);
            GameManager.DeActivateAllObjects();
        }
    }
}

public enum WinType
{
    Annihilation, TargetDestroy, Defence
}

public enum LoseType
{
    Annihilation, TargetDestroy, TimeLimit
}