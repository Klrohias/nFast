using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.GamePlay;
using Klrohias.NFast.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIControllers
{
    public class EntryUIController : MonoBehaviour
    {
        public Button EnterButton;

        void Start()
        {
            NavigationService.Get().JumpScene("Scenes/EntryScene");
            EnterButton.onClick.AddListener(() => { NavigationService.Get().JumpScene("Scenes/MainMenuScene"); });
            TouchService.Get().enabled = false;
        }
    }
}