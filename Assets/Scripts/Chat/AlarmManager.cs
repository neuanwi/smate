using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class AlarmManager : MonoBehaviour
{
    public static AlarmManager Instance { get; private set; }

    [Serializable]
    public class AlarmData
    {
        public string time;
        public string text;
        public string createdAt;
    }

    [Serializable]
    private class AlarmWrapper
    {
        public List<AlarmData> items = new List<AlarmData>();
    }

    // 캘린더가 듣는 이벤트
    public event Action<AlarmData> OnAlarmAdded;
    public event Action<AlarmData> OnAlarmDeleted;

    private AlarmWrapper alarmList = new AlarmWrapper();
    private string jsonPath;

    void Awake()
    {
        // 싱글톤
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        jsonPath = Path.Combine(Application.persistentDataPath, "alarms.json");
        LoadAlarms();
    }

    // 시작할 때 캘린더가 읽어가라고 주는 리스트
    public IReadOnlyList<AlarmData> GetAllAlarms()
    {
        return alarmList.items;
    }

    public void SaveAlarm(string time, string text)
    {
        if (string.IsNullOrWhiteSpace(time) || string.IsNullOrWhiteSpace(text))
            return;

        if (alarmList.items == null)
            alarmList.items = new List<AlarmData>();

        // 같은 거 여러 번 안 넣게
        bool dup = alarmList.items.Exists(a => a.time == time && a.text == text);
        if (dup) return;

        var data = new AlarmData
        {
            time = time,
            text = text,
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };

        alarmList.items.Add(data);
        SaveFile();

        Debug.Log($"[AlarmManager] 저장 완료: {jsonPath}");

        // ✅ UI 들으라고 알림
        OnAlarmAdded?.Invoke(data);
    }

    // 캘린더에서 삭제 눌렀을 때 호출할 함수
    public void DeleteAlarm(AlarmData alarm)
    {
        if (alarm == null) return;
        if (alarmList.items.Remove(alarm))
        {
            SaveFile();
            OnAlarmDeleted?.Invoke(alarm);
        }
    }

    private void SaveFile()
    {
        File.WriteAllText(jsonPath, JsonUtility.ToJson(alarmList, true), Encoding.UTF8);
    }

    private void LoadAlarms()
    {
        if (!File.Exists(jsonPath))
        {
            alarmList = new AlarmWrapper();
            return;
        }

        var json = File.ReadAllText(jsonPath);
        var loaded = JsonUtility.FromJson<AlarmWrapper>(json);
        alarmList = loaded ?? new AlarmWrapper();
        if (alarmList.items == null) alarmList.items = new List<AlarmData>();
    }
}
