using Klrohias.NFast.Navigation;
using Klrohias.NFast.PhiGamePlay;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIControllers
{
    public class MainMenuUIController : MonoBehaviour
    {
        public Button TestButton;
        public ToggleGroup tabGroup;
        public GameObject[] views;

        void Start()
        {
            TestButton.onClick.AddListener(() =>
            {
                NavigationService.Get().ExtraData = new PhiGamePlayer.GameStartInfo()
                {
                    Path = "H:/testchart.pez",
                };
                NavigationService.Get().LoadScene("Scenes/PhiPlayScene");
            });
            var activation = tabGroup.GetFirstActiveToggle();
            var index = activation.transform.GetSiblingIndex();
            for (int i = 0; i < views.Length; i++)
                views[i].SetActive(i == index);
        }
    }
}