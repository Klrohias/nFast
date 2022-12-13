using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIControllers
{
    public class MainMenuUIController : MonoBehaviour
    {
        public Button TestButton;

        void Start()
        {
            TestButton.onClick.AddListener(() =>
            {
                NavigationService.Get().ExtraData = new GamePlayer.GameStartInfo()
                {
                    Path = "H:/Tan90_Full.pez",
                    UseLargeChart = true
                };
                NavigationService.Get().LoadScene("Scenes/PlayScene");
            });
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}