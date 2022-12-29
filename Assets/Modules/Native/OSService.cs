using System.Collections;
using System.Collections.Generic;
using System.IO;
using Klrohias.NFast.Utilities;
using UnityEngine;

namespace Klrohias.NFast.Native
{
    public class OSService : Service<OSService>
    {
        public string CachePath { get; private set; }
        public string DataPath { get; private set; }
        public string ChartPath { get; private set; }
        private const string DATA_DIR_NAME = "nFast";
        void Awake()
        {
            CachePath = Application.temporaryCachePath;
#if UNITY_EDITOR
            DataPath = Path.Combine(Application.persistentDataPath, DATA_DIR_NAME);
#elif UNITY_ANDROID
            DataPath = Path.Combine("/storage/emulated/0", DATA_DIR_NAME);
#else
            DataPath = Path.Combine(Application.persistentDataPath, DATA_DIR_NAME);
#endif
            if (!Directory.Exists(DataPath)) Directory.CreateDirectory(DataPath);
            ChartPath = Path.Combine(DataPath, "Charts");
            if (!Directory.Exists(ChartPath)) Directory.CreateDirectory(ChartPath);
        }
    }
}
