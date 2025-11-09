using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class CalendarPopUpUI : MonoBehaviour
{
    public InputField TodoInputField;
    public InputField TimeInputField;

    private const string timeFormat = "yyyy-MM-dd HH:mm:ss";

    public Button DeleteButton;

    public event Action<CalendarPopUpUI> OnDeleteRequested;

    void Start()
    {
        if (DeleteButton != null)
        {
            DeleteButton.onClick.AddListener(HandleDeleteClick);
        }

        if (TimeInputField.text == "")
            TimeInputField.text = DateTime.Now.ToString();
    }

    private void HandleDeleteClick()
    {
        OnDeleteRequested?.Invoke(this);
    }

    public void SetUpPopUI(string message, string time)
    {
        SetText(message);
        SetTime(time);
    }
    private void SetText(string message)
    {
        if (TodoInputField != null)
        {
            TodoInputField.text = message;
        }
        else
        {
            Debug.LogError("CalendarPopUpUI에 TodoInputField 컴포넌트가 연결되지 않았습니다!");
        }
    }

    private void SetTime(string newTimeString)
    {
        TimeInputField.text = newTimeString;

        if (TimeInputField.text == "")
            TimeInputField.text = DateTime.Now.ToString();
    }
    private string FormatFullText(DateTime time, string message)
    {
        return $"[{time.ToString(timeFormat)}] {message}";
    }

}