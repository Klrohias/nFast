using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EntryUIController : MonoBehaviour
{
    public Button EnterButton;

    void Start()
    {
        NavigationService.Get().JumpScene("Scenes/EntryScene");
        EnterButton.onClick.AddListener(() =>
        {
            NavigationService.Get().JumpScene("Scenes/MainMenuScene");
        });
    }
}
