using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.Utilities;
using UnityEngine;

namespace Klrohias.NFast.Native
{
    public class OSService : Service<OSService>
    {
        public string CachePath { get; private set; }
        void Awake()
        {
            CachePath = Application.temporaryCachePath;
        }
    }
}
