using System;
using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.Navigation;
using Klrohias.NFast.PhiGamePlay;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIControllers
{
    public class MainMenuUIController : MonoBehaviour
    {
        public Button TestButton;
        [Serializable]
        public class TabItem
        {
            public Button Button;
            public GameObject View;
        }
        public List<TabItem> TabItems;
        public Color TabSelectedColor;
        public Color TabColor;
        private int selectedItem;
        void Start()
        {
            TestButton.onClick.AddListener(() =>
            {
                NavigationService.Get().ExtraData = new PhiGamePlayer.GameStartInfo()
                {
                    Path = "H:/testchart.pez",
                    UseLargeChart = false
                };
                NavigationService.Get().LoadScene("Scenes/PhiPlayScene");
            });
            RefreshTabItems();
            for (var i = 0; i < TabItems.Count; i++)
            {
                var curr = i;
                TabItems[i].Button.onClick.AddListener(() =>
                {
                    selectedItem = curr;
                    RefreshTabItems();
                });
            }
        }

        private void RefreshTabItems()
        {
            for (var i = 0; i < TabItems.Count; i++)
            {
                var button = TabItems[i].Button;
                var text = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                var image = button.transform.Find("Image").GetComponentInChildren<Image>();

                var color = i == selectedItem ? TabSelectedColor : TabColor;
                text.color = color;
                image.color = color;

                if (TabItems[i].View != null)
                    TabItems[i].View.SetActive(i == selectedItem);
            }
        }
    }
}