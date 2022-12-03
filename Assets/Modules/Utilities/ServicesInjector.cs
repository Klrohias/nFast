using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

public class ServicesInjector : MonoBehaviour
{
    private Type[] serviceTypes = new Type[]
    {
        typeof(NavigationService),
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
