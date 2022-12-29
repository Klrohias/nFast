using System;
using System.IO;
using Klrohias.NFast.Native;
using Klrohias.NFast.Navigation;
using Klrohias.NFast.PhiGamePlay;
using Klrohias.NFast.Utilities;
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

        private async void ImportFile()
        {
            var path = LocalImport.PathInput.text;
            if (!File.Exists(path))
            {
                ToastService.Get().Show(ToastService.ToastType.Failure,"路径可能有误\n文件不存在");
                return;
            }

            LocalImport.ImportButton.interactable = false;

            var fileName = Path.GetFileName(path);
            var ext = Path.GetExtension(path);
            if (ext == ".nfp")
            {
                await Async.RunOnThread(() =>
                {
                    File.Copy(path, 
                        Path.Combine(OSService.Get().ChartPath, fileName));
                });
            }
            else
            {
                fileName = Path.GetFileNameWithoutExtension(path) + ".nfp";
                await PhiChartLoader.ChartLoader.ToNFastChart(path, OSService.Get().CachePath,
                    Path.Combine(OSService.Get().ChartPath, fileName));
            }
            ToastService.Get().Show(ToastService.ToastType.Success, "导入成功");

            LocalImport.ImportButton.interactable = true;
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