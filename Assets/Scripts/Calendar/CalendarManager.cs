using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CalendarManager : MonoBehaviour
{
    [SerializeField]
    List<CalendarPopUpUI> CalendarList = new List<CalendarPopUpUI>();

    public GameObject CalendarPanel, CalendarListPanel, PopupMessage;

    public InputField MessageInputField, TimeInputField;
    void Start()
    {
        CalendarPanel.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            UpLoadSchedule();
            //PopupMessageToCalendar("문자 테스트 중!!! / text testing!!!", "1234-05-13 오전 1:23:45");
        }
    }
    public void UpLoadSchedule()
    {
        if (MessageInputField.text == "" || TimeInputField.text == "") return;
        PopupMessageToCalendar(MessageInputField.text, TimeInputField.text);
        MessageInputField.text = ""; TimeInputField.text = "";
    }
    public void PopupMessageToCalendar(string text, string time)
    {
        GameObject newMesh = Instantiate(PopupMessage, CalendarListPanel.transform);
        CalendarPopUpUI newCalendarPopUpUI = newMesh.GetComponent<CalendarPopUpUI>();
        newCalendarPopUpUI.SetUpPopUI(text, time);
        newCalendarPopUpUI.OnDeleteRequested += HandlePopUpDelete;
        CalendarList.Add(newCalendarPopUpUI);

    }

    public void ToggleWindow()
    {
        CalendarPanel.SetActive(!CalendarPanel.activeSelf);
    }

    private void HandlePopUpDelete(CalendarPopUpUI slotToDelete)
    {
        CalendarList.Remove(slotToDelete);

        Destroy(slotToDelete.gameObject);
    }
}