using System;
using Klrohias.NFast.Native;
using Klrohias.NFast.Navigation;
using Klrohias.NFast.PhiGamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIControllers
{
    public class MainMenuUIController : MonoBehaviour
    {
        public Button TestButton;
        [Serializable]
        public struct LocalImportProperties
        {
            public Button BrowseButton;
            public Button ImportButton;
            public TMP_InputField PathInput;
        }
        public LocalImportProperties LocalImport;
        private void Start()
        {
            LocalImport.BrowseButton.onClick.AddListener(BrowseImportFile);
            LocalImport.ImportButton.onClick.AddListener(ImportFile);
        }

        private void ImportFile()
        {
            throw new NotImplementedException();
        }

        private async void BrowseImportFile()
        {
            var fps = FilePickerService.Get();
            fps.Filter = ".nfp.pez.zip.pgm";
            fps.CurrentDirectory = OSService.Get().DataPath;
            fps.Open();
            LocalImport.PathInput.text = await fps.GetSelectedFile() ?? "";
        }
    }
}