using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class IntroPopup : MonoBehaviour
{
    public GameObject introCanvas;
    public InputActionProperty aButton;

    void Start()
    {
        Invoke("ShowPopup", 3f);
    }

    void ShowPopup()
    {
        introCanvas.SetActive(true);
    }

    void Update()
    {
        if (aButton.action.IsPressed())
        {
            SceneManager.LoadScene("General");
        }
    }
}