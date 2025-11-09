using System;
using UnityEngine;
using UnityEngine.UI;

public class CalendarPopUpUI : MonoBehaviour
{
    public InputField TodoInputField;
    public InputField TimeInputField;
    public Button DeleteButton;

    // 이 UI가 표시하고 있는 실제 알람 데이터
    public AlarmManager.AlarmData boundAlarm;

    public event Action<CalendarPopUpUI> OnDeleteRequested;

    void Start()
    {
        if (DeleteButton != null)
            DeleteButton.onClick.AddListener(HandleDeleteClick);

        if (string.IsNullOrEmpty(TimeInputField.text))
            TimeInputField.text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    }

    private void HandleDeleteClick()
    {
        OnDeleteRequested?.Invoke(this);
    }

    // AlarmData로부터 세팅
    public void SetUpPopUI(AlarmManager.AlarmData data)
    {
        boundAlarm = data;
        SetText(data.text);
        SetTime(data.time);
    }

    // 채팅에서 바로 text, time만 넘어오는 경우용
    public void SetUpPopUI(string message, string time)
    {
        SetText(message);
        SetTime(time);
    }

    private void SetText(string message)
    {
        if (TodoInputField != null)
            TodoInputField.text = message;
    }

    private void SetTime(string newTimeString)
    {
        if (TimeInputField != null)
            TimeInputField.text = newTimeString;
    }
}
