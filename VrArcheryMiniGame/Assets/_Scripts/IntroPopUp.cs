using UnityEngine;

public class IntroPopup : MonoBehaviour
{
    public GameObject introCanvas;

    void Start()
    {
        Invoke("ShowPopup", 5f);
    }

    void ShowPopup()
    {
        introCanvas.SetActive(true);
    }
}