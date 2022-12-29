using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIControllers
{
    public class PlayUIController : MonoBehaviour
    {
        // Start is called before the first frame update
        public Button BackButton;

        void Start()
        {
            BackButton.onClick.AddListener(() => { NavigationService.Get().Back(); });
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}