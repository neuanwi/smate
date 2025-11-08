using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

public class AlarmManager : MonoBehaviour
{
    [Serializable]
    public class AlarmData
    {
        public string time;     // "2025-11-08 15:00"
        public string text;     // "도서관 가기"
        public string createdAt;
    }

    [Serializable]
    private class AlarmWrapper
    {
        public List<AlarmData> items = new List<AlarmData>();
    }

    private AlarmWrapper alarmList = new AlarmWrapper();
    private string jsonPath;

    void Awake()
    {
        jsonPath = Path.Combine(Application.persistentDataPath, "alarms.json");
        LoadAlarms();
    }

    public void SaveAlarm(string time, string text)
    {
        if (alarmList.items == null) alarmList.items = new List<AlarmData>();
        bool dup = alarmList.items.Exists(a => a.time == time && a.text == text);
        if (dup) return;

        var data = new AlarmData
        {
            time = time,
            text = text,
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };

        alarmList.items.Add(data);
        File.WriteAllText(jsonPath, JsonUtility.ToJson(alarmList, true), Encoding.UTF8);
        Debug.Log($"[AlarmManager] 저장 완료: {jsonPath}");
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
