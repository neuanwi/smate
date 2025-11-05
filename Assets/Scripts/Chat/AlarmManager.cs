using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class AlarmManager : MonoBehaviour
{
    // ✅ 알람 데이터 구조
    [Serializable]
    public class AlarmData
    {
        public string time;       // "2025-11-06 15:00"
        public string task;       // "도서관 가기"
        public string createdAt;  // 저장된 날짜
    }

    [Serializable]
    private class AlarmWrapper
    {
        public List<AlarmData> items;
    }

    private List<AlarmData> alarmList = new List<AlarmData>();
    private string jsonPath;

    void Awake()
    {
        jsonPath = Path.Combine(Application.persistentDataPath, "alarms.json");
        LoadAlarms();
    }

    // ✅ ChatManager에서 호출할 함수
    public void TryCreateAlarmFromMessage(string message)
    {
        string time = ParseTime(message);
        if (string.IsNullOrEmpty(time))
        {
            Debug.Log("[AlarmManager] 시간 파싱 실패");
            return;
        }

        string task = ParseTask(message);
        SaveAlarm(time, task);
    }

    // ✅ "3시에 도서관 가는 알람 만들어줘" → "2025-11-07 15:00"
    private string ParseTime(string message)
    {
        Match m = Regex.Match(message, @"(\d{1,2})시");
        if (!m.Success) return null;

        int hour = int.Parse(m.Groups[1].Value);
        DateTime now = DateTime.Now;
        DateTime alarmTime = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0);
        return alarmTime.ToString("yyyy-MM-dd HH:mm");
    }

    // ✅ "알람", "설정", "해줘" 같은 단어 제거 → “도서관 가기”
    private string ParseTask(string message)
    {
        string task = message;
        task = task.Replace("알람", "")
                   .Replace("설정", "")
                   .Replace("해줘", "")
                   .Replace("만들어줘", "")
                   .Replace("켜줘", "")
                   .Trim();
        return string.IsNullOrEmpty(task) ? "할 일 없음" : task;
    }

    // ✅ 실제 저장
    private void SaveAlarm(string time, string task)
    {
        AlarmData data = new AlarmData
        {
            time = time,
            task = task,
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };

        alarmList.Add(data);
        WriteToJson();
        Debug.Log($"[Alarm 저장] {data.time} / {data.task}");
        PrintAll();   // 저장 후 전체 출력
    }

    private void WriteToJson()
    {
        AlarmWrapper wrapper = new AlarmWrapper { items = alarmList };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(jsonPath, json);
    }

    private void LoadAlarms()
    {
        if (!File.Exists(jsonPath)) return;
        string json = File.ReadAllText(jsonPath);
        AlarmWrapper wrapper = JsonUtility.FromJson<AlarmWrapper>(json);
        if (wrapper != null && wrapper.items != null)
            alarmList = wrapper.items;
    }

    public void PrintAll()
    {
        if (alarmList.Count == 0)
        {
            Debug.Log("[Alarm] 저장된 알람 없음");
            return;
        }
        Debug.Log($"[Alarm 전체 리스트] ({alarmList.Count}개)");
        foreach (var a in alarmList)
        {
            Debug.Log($" - {a.time} / {a.task} (저장: {a.createdAt})");
        }
    }
}
