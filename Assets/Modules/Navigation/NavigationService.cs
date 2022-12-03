using System;
using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Klrohias.NFast.Navigation
{
    public class NavigationService : Service<NavigationService>
    {
        private class NavigationItem
        {
            public string SceneName = "";
            public object ExtraData = null;
        }

        private Stack<NavigationItem> navStack = new();
        private object extraData = null;

        public object ExtraData
        {
            get => navStack.Peek().ExtraData;
            set => extraData = value;
        }

        public void JumpScene(string name)
        {
            if (navStack.Count == 0)
            {
                navStack.Push(new NavigationItem
                {
                    SceneName = name,
                    ExtraData = extraData
                });
                extraData = null;
                return;
            }

            var curr = navStack.Peek();
            curr.SceneName = name;
            curr.ExtraData = extraData;
            extraData = null;

            SceneManager.LoadSceneAsync(name);
        }

        public void LoadScene(string name)
        {
            if (navStack.Count == 0) throw new InvalidOperationException("nav stack is empty");
            navStack.Push(new NavigationItem()
            {
                SceneName = name,
                ExtraData = extraData
            });
            extraData = null;
            SceneManager.LoadSceneAsync(name);
        }

        public void Back()
        {
            if (navStack.Count == 1) throw new InvalidOperationException("nav stack is empty");
            navStack.Pop();
            var curr = navStack.Peek();
            if (extraData != null) curr.ExtraData = extraData;
            extraData = null;
            SceneManager.LoadSceneAsync(curr.SceneName);
        }
    }
}