using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MapList : MonoBehaviour
{
    ScrollRect scrollRect;
    public GameObject mapListPrefab;
    public Map[] mapList;
    GameObject[] mapListButton;
    public GameObject scrollViewContent;
    public Text explain;
    public Button startButton;

    // Start is called before the first frame update
    void Start()
    {
        scrollRect = GetComponent<ScrollRect>();
        mapListButton = new GameObject[mapList.Length];
        
        for(int i = 0; i < mapListButton.Length; i++)
        {
            mapListButton[i] = Instantiate(mapListPrefab) as GameObject;
            mapListButton[i].transform.SetParent(scrollViewContent.transform);
            mapListButton[i].transform.Find("Text").GetComponent<Text>().text = mapList[i].mapName;

            RectTransform buttonTransform = mapListButton[i].GetComponent<RectTransform>();
            buttonTransform.anchoredPosition = new Vector2(0, -15 - i * 30);
            buttonTransform.localScale = new Vector3(1, 1, 1);

            Button button = mapListButton[i].GetComponent<Button>();
            string goals = mapList[i].goals;
            string sceneName = mapList[i].sceneName;
            button.onClick.AddListener(() => MapInfo(goals, sceneName));
        }
    }

    // Update is called once per frame
    void Update()
    {
        scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, mapList.Length * 30);
    }

    public void MapInfo(string goals, string sceneName)
    {
        explain.text = goals;
        startButton.gameObject.SetActive(true);
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() => {
            SceneManager.LoadScene(sceneName);
        });
    }

    void OnDisable()
    {
        explain.text = "";
        startButton.gameObject.SetActive(false);
        startButton.onClick.RemoveAllListeners();
    }
}

[System.Serializable]
public struct Map
{
    public string sceneName;
    public string mapName;
    public Texture thumbnail;
    public string goals;
}