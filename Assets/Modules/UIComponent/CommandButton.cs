using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.Assets.Modules.UIComponent
{
    [RequireComponent(typeof(Button))]
    public class CommandButton : MonoBehaviour
    {
        public object CommandValue { get; set; }
        public event Action<object> OnClick;
        public TMP_Text Label;
        public string Title
        {
            get => Label.text;
            set => Label.text = value;
        }

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                OnClick?.Invoke(CommandValue);
            });
        }
    }
}