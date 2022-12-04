using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Klrohias.NFast.GamePlay;
using Klrohias.NFast.Native;
using Klrohias.NFast.Navigation;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace Klrohias.NFast.Utilities
{
    public class ServicesInjector : MonoBehaviour
    {
        private Type[] serviceTypes = new Type[]
        {
            // add services here...
            typeof(NavigationService),
            typeof(TouchService),
            typeof(OSService)
        };

        public Transform ServicesRoot;

        private void injectServices()
        {
            var rootGameObject = ServicesRoot.gameObject;
            UObject.DontDestroyOnLoad(rootGameObject);
            foreach (var serviceType in serviceTypes)
            {
                rootGameObject.AddComponent(serviceType);
            }
        }

        void Awake()
        {
            injectServices();
        }
    }
}