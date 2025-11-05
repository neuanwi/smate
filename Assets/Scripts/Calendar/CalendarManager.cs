using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CalendarManager : MonoBehaviour
{
    [SerializeField]
    List<Message> CalendarList = new List<Message>();

    public GameObject CalendarPanel,CalendarListPanel, PopupMessage;

    void Start()
    {
        CalendarPanel.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PopupMessageToCalendar("문자 테스트 중!!! / text testing!!!");
        }
    }

    public void UpLoadCalendar(List<string> texts)
    {

    }

    public void PopupMessageToCalendar(string text)
    {
        Message newMessage = new Message();
        newMessage.text = text;

        GameObject newText = Instantiate(PopupMessage, CalendarListPanel.transform);

        CalendarList.Add(newMessage);
    }

    public void ToggleWindow()
    {
        CalendarPanel.SetActive(!CalendarPanel.activeSelf);
    }
}

[System.Serializable]
public class Message
{
    public string text;
}