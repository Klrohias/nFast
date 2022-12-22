using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIControllers
{
    public class EntryUIController : MonoBehaviour
    {
        public Button EnterButton;
        public GameObject ServicesRoot;

        private void Awake()
        {
            DontDestroyOnLoad(ServicesRoot);
        }
        void Start()
        {
            NavigationService.Get().JumpScene("Scenes/EntryScene");
            EnterButton.onClick.AddListener(() => { NavigationService.Get().JumpScene("Scenes/MainMenuScene"); });
        }
    }
}