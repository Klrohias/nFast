using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Klrohias.NFast.UIComponent;
using Klrohias.NFast.Tween;
using Klrohias.NFast.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIControllers
{
    public class FilePickerService : Service<FilePickerService>
    {
        public CanvasGroup FilePickerBackground;
        public Button CloseButton;
        public GameObject CommandButton;
        public Transform Viewport;
        public TMP_Text PathLabel;
        public Button BackButton;
        public Button SelectButton;
        private ObjectPool _commandButtons;
        private string _currentDirectory = "/";
        private TaskCompletionSource<string> _taskCompletionSource;

        public string CurrentDirectory
        {
            get => _currentDirectory;
            set
            {
                _currentDirectory = value;
                if (_isOpened) UpdateContent();
            }
        }

        private string _filter = null;
        public string Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                if (_isOpened) UpdateContent();
            }
        }

        private string _selectedFile = null;
        private UnorderedList<string> _files = new UnorderedList<string>();
        private bool _isOpened = false;

        private void Awake()
        {
            FilePickerBackground.SetDisplay(false);
        }

        private void Start()
        {
            CloseButton.onClick.AddListener(() =>
            {
                _selectedFile = null;
                Close();
            });
            SelectButton.onClick.AddListener(Close);
            BackButton.onClick.AddListener(() =>
            {
                _currentDirectory = Path.GetDirectoryName(_currentDirectory);

#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
                if (_currentDirectory == null) _currentDirectory = "/";
#endif
                UpdateContent();
            });
            _commandButtons = new ObjectPool(() =>
            {
                var item = Instantiate(CommandButton);
                item.transform.SetParent(Viewport, false);
                item.transform.localScale = Vector3.one;
                item.GetComponent<CommandButton>().OnClick += OnSelect;
                return item;
            }, 10);
        }

        private void OnSelect(object selectResult)
        {
            var typedResult = selectResult as string;
            if (typedResult == null) return;
            if (File.Exists(typedResult))
            {
                _selectedFile = typedResult;
                UpdateSelectState();
            }
            else
            {
                CurrentDirectory = typedResult;
            }
        }

        public async void Open()
        {
            if (_isOpened) return;
            UpdateContent();
            _isOpened = true;
            FilePickerBackground.SetDisplay(true);
            await FilePickerBackground.NTweenAlpha(180f, EasingFunction.SineOut, 0f, 1f);
        }

        public async void Close()
        {
            if (!_isOpened) return;
            FinishSelect();
            _isOpened = false;
            await FilePickerBackground.NTweenAlpha(180f, EasingFunction.SineIn, 1f, 0f);
            FilePickerBackground.SetDisplay(false);
        }

        private string GetDirectoryString(string dir = null)
        {
            if (dir == null) dir = _currentDirectory;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (dir == null) return "Root";
#endif
            if (dir.Length > 36)
            {
                return dir[..10] + "···" + dir[^10..];
            }

            return dir;
        }

        private void UpdateSelectState()
        {
            SelectButton.interactable = _selectedFile != null;
            if (_selectedFile != null)
            {
                PathLabel.text = GetDirectoryString(_selectedFile);
            }
        }

        public Task<string> GetSelectedFile()
        {
            if (_taskCompletionSource != null) throw new Exception();
            _taskCompletionSource = new();
            return _taskCompletionSource.Task;
        }

        private void FinishSelect()
        {
            if (_taskCompletionSource == null) return;
            var source = _taskCompletionSource;
            _taskCompletionSource = null;
            source.TrySetResult(_selectedFile);
        }

        private void UpdateFiles()
        {
            _files.Clear();
            _selectedFile = null;
            UpdateSelectState();

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (_currentDirectory == null)
            {
                foreach (var drive in "ABCDEFGHIJKLNMOPQRSTUVWXYZ")
                {
                    var path = $"{drive}:\\";
                    if (Directory.Exists(path)) _files.Add(path);
                }
                return;
            }
#endif
            _files.AddRange(Directory.GetDirectories(_currentDirectory));
            _files.AddRange(Directory.GetFiles(_currentDirectory)
                .Where(x => _filter?.Contains(Path.GetExtension(x)) ?? true)
                .ToList());
        }

        private void UpdateContent()
        {
            PathLabel.text = GetDirectoryString();
            UpdateFiles();

            _commandButtons.ReturnAll();

            for (int i = 0; i < _files.Length; i++)
            {
                var item = _commandButtons.RequestObject();
                var commandButton = item.GetComponent<CommandButton>();
                commandButton.CommandValue = _files[i];

                item.SetActive(true);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                if (_files[i].EndsWith('\\'))
                {
                    commandButton.Title = _files[i];
                    continue;
                }
#endif

                commandButton.Title = Path.GetFileName(_files[i]);
            }
        }
    }
}