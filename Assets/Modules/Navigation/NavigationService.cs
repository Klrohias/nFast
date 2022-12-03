using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NavigationService : Service<NavigationService>
{
    private class NavigationItem
    {
        public string SceneName = "";
        public object ExtraData = null;
    }

    private Stack<NavigationItem> navStack = new();

    public void JumpScene(string name)
    {
        if (navStack.Count == 0)
        {
            navStack.Push(new NavigationItem
            {
                SceneName = name
            });
            return;
        }

        var curr = navStack.Peek();
        curr.SceneName = name;

        SceneManager.LoadSceneAsync(name);
    }

    public void LoadScene(string name)
    {
        if (navStack.Count == 0) throw new InvalidOperationException("nav stack is empty");
        navStack.Push(new NavigationItem()
        {
            SceneName = name
        });

        SceneManager.LoadSceneAsync(name);
    }

    public void Back()
    {
        if (navStack.Count == 1) throw new InvalidOperationException("nav stack is empty");
        navStack.Pop();
        var curr = navStack.Peek();
        
        SceneManager.LoadSceneAsync(name);
    }
}
