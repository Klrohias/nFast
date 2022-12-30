using System;
using System.IO;
using Klrohias.NFast.Native;
using Klrohias.NFast.Navigation;
using Klrohias.NFast.PhiGamePlay;
using Klrohias.NFast.UIComponent;
using Klrohias.NFast.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIControllers
{
    public class MainMenuUIController : MonoBehaviour
    {
        [Serializable]
        public struct HomeProperties
        {
            public Transform Viewport;
            public Button RefreshChartButton;
        }
        [Serializable]
        public struct LocalImportProperties
        {
            public Button BrowseButton;
            public Button ImportButton;
            public TMP_InputField PathInput;
        }
        public LocalImportProperties LocalImport;
        public HomeProperties Home;
        public GameObject CommandButtonPrefab;
        private ObjectPool _homeListItemPool;

        private void Awake()
        {
            _homeListItemPool = new ObjectPool(() =>
            {
                var item = Instantiate(CommandButtonPrefab);
                var transform = item.transform;
                transform.SetParent(Home.Viewport, false);
                transform.localScale = Vector3.one;
                transform.GetComponent<CommandButton>().OnClick += OnHomeListItemSelected;
                return item;
            }, 10);
        }

        private void OnHomeListItemSelected(object obj)
        {
            var typedParam = obj as string;
            if (typedParam == null) return;
            var navSrv = NavigationService.Get();
            navSrv.ExtraData = typedParam;
            navSrv.LoadScene("Scenes/PhiPlayScene");
        }

        private void Start()
        {
            LocalImport.BrowseButton.onClick.AddListener(BrowseImportFile);
            LocalImport.ImportButton.onClick.AddListener(ImportFile);
            Home.RefreshChartButton.onClick.AddListener(UpdateCharts);
            UpdateCharts();
        }

        private void UpdateCharts()
        {
            _homeListItemPool.ReturnAll();

            var chartPath = OSService.Get().ChartPath;
            var files = Directory.GetFiles(chartPath, "*.nfp");
            foreach (var file in files)
            {
                var item = _homeListItemPool.RequestObject();
                var commandButton = item.GetComponent<CommandButton>();
                commandButton.CommandValue = file;
                commandButton.Title = Path.GetFileName(file);
                item.SetActive(true);
            }
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
            UpdateCharts();
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