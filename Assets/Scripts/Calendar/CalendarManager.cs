using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CalendarManager : MonoBehaviour
{
    [SerializeField]
    private List<CalendarPopUpUI> calendarList = new List<CalendarPopUpUI>();

    public GameObject CalendarPanel, CalendarListPanel, PopupMessage;
    public InputField MessageInputField, TimeInputField;

    private AlarmManager alarmManager;

    void Start()
    {
        CalendarPanel.SetActive(false);

        // AlarmManager 찾아서 구독
        alarmManager = AlarmManager.Instance ?? FindObjectOfType<AlarmManager>();

        if (alarmManager != null)
        {
            // 1) 처음 켰을 때 이미 있는 알람들 UI로 뿌리기
            foreach (var alarm in alarmManager.GetAllAlarms())
            {
                CreateUIForAlarm(alarm);
            }

            // 2) 이후로 추가/삭제되는 것도 듣기
            alarmManager.OnAlarmAdded += HandleAlarmAdded;
            alarmManager.OnAlarmDeleted += HandleAlarmDeleted;
        }
        else
        {
            Debug.LogWarning("[CalendarManager] AlarmManager를 찾지 못했습니다.");
        }
    }

    void OnDestroy()
    {
        if (alarmManager != null)
        {
            alarmManager.OnAlarmAdded -= HandleAlarmAdded;
            alarmManager.OnAlarmDeleted -= HandleAlarmDeleted;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            UploadSchedule();
    }

    public void UploadSchedule()
    {
        if (string.IsNullOrWhiteSpace(MessageInputField.text) ||
            string.IsNullOrWhiteSpace(TimeInputField.text))
            return;

        // 여기서는 AlarmManager만 호출하면 나머지는 이벤트로 UI가 생김
        alarmManager?.SaveAlarm(TimeInputField.text, MessageInputField.text);

        MessageInputField.text = "";
        TimeInputField.text = "";
    }

    public void ToggleWindow()
    {
        CalendarPanel.SetActive(!CalendarPanel.activeSelf);
    }

    // ======= 이벤트 핸들러 =======

    private void HandleAlarmAdded(AlarmManager.AlarmData alarm)
    {
        CreateUIForAlarm(alarm);
    }

    private void HandleAlarmDeleted(AlarmManager.AlarmData alarm)
    {
        // UI에서도 찾아서 없애기
        var ui = calendarList.Find(c => c.boundAlarm == alarm);
        if (ui != null)
        {
            calendarList.Remove(ui);
            Destroy(ui.gameObject);
        }
    }

    private void CreateUIForAlarm(AlarmManager.AlarmData alarm)
    {
        GameObject obj = Instantiate(PopupMessage, CalendarListPanel.transform);
        var ui = obj.GetComponent<CalendarPopUpUI>();
        ui.SetUpPopUI(alarm);                 // 알람 데이터 그대로 넣기
        ui.OnDeleteRequested += HandlePopUpDelete;
        calendarList.Add(ui);
    }

    private void HandlePopUpDelete(CalendarPopUpUI slot)
    {
        // 사용자가 빨간 버튼 눌렀을 때 → AlarmManager에 삭제 요청
        if (alarmManager != null && slot.boundAlarm != null)
            alarmManager.DeleteAlarm(slot.boundAlarm);
        else
        {
            // 혹시 boundAlarm이 비어 있으면 UI만 삭제
            calendarList.Remove(slot);
            Destroy(slot.gameObject);
        }
    }
}
