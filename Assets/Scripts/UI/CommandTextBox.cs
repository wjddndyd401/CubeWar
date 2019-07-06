using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CommandTextBox : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RawImage commandTextBox;
    RectTransform rect;
    Text text;
    string message = "";

    void Start()
    {
        rect = GetComponent<RectTransform>();
        text = commandTextBox.transform.GetChild(0).GetComponent<Text>();
    }

    public void SetText(string _message)
    {
        message = _message;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (message.Equals(""))
        {
            commandTextBox.gameObject.SetActive(false);
        } else
        {
            commandTextBox.gameObject.SetActive(true);

            commandTextBox.rectTransform.anchoredPosition = new Vector2(((RectTransform)transform).anchoredPosition.x, ((RectTransform)transform).anchoredPosition.y + ((RectTransform)transform).sizeDelta.y / 2);
            text.text = message;
            commandTextBox.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            commandTextBox.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)commandTextBox.transform);

            if (commandTextBox.rectTransform.anchoredPosition.x + commandTextBox.rectTransform.sizeDelta.x / 2 > ((RectTransform)transform.parent.transform).sizeDelta.x / 2)
            {
                //commandTextBox.rectTransform.anchoredPosition = new Vector2(((RectTransform)transform.parent.transform).sizeDelta.x / 2 - commandTextBox.rectTransform.sizeDelta.x / 2, commandTextBox.rectTransform.anchoredPosition.y);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        commandTextBox.gameObject.SetActive(false);
    }
}
