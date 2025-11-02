using UnityEngine;

public class CancelOption : MonoBehaviour
{
    public void PressCancelButton()
    {
        Debug.Log("게임 종료.");
        Quit();
    }
    public void Quit()
    {
        Application.Quit();
    }
}
