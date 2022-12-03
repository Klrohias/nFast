using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Service<T> : MonoBehaviour
    where T : MonoBehaviour
{
    public static T Get() => service;
    private static T service = null;

    public Service()
    {
        service = this as T;
    }
}
