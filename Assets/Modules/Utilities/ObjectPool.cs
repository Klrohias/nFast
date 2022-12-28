using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Klrohias.NFast.Utilities
{
    public class ObjectPool
    {
        private List<GameObject> objects = new();
        public Func<GameObject> OnRequestNewObject;
        private byte[] lendObjectBitmap = new byte[4];
        public ObjectPool(Func<GameObject> onRequestNewObject)
        {
            OnRequestNewObject = onRequestNewObject;
        }

        public ObjectPool(Func<GameObject> onRequestNewObject, int c)
        {
            OnRequestNewObject = onRequestNewObject;

            for (int i = 0; i < c; i++)
            {
                RequestNewObject();
            }
        }

        private void RequestNewObject()
        {
            var obj = OnRequestNewObject();
            obj.SetActive(false);
            objects.Add(obj);
            if (objects.Count / 8 >= lendObjectBitmap.Length)
            {
                var newLendObjectBitmap = new byte[lendObjectBitmap.Length + 2];
                lendObjectBitmap.CopyTo(newLendObjectBitmap, 0);
                lendObjectBitmap = newLendObjectBitmap;
                GC.Collect();
            }
        }

        public void WarmUp(int target)
        {
            while (target > objects.Count)
            {
                RequestNewObject();
            }
        }
        public GameObject RequestObject()
        {
            // find object
            int i = 0;
            for (; i < lendObjectBitmap.Length; i++)
            {
                if (lendObjectBitmap[i] != 0xFF) break;
            }

            if (i == lendObjectBitmap.Length)
            {
                RequestNewObject();
            }

            int k = 0;
            for (; k < 8; k++)
            {
                if ((lendObjectBitmap[i] >> k & (byte) 0b1) == 0) break;
            }

            var index = i * 8 + k;
            if (index >= objects.Count)
            {
                RequestNewObject();
            }
            var result = objects[index];
            lendObjectBitmap[i] |= (byte) (0b1 << k);
            return result;
        }

        public void ReturnObject(GameObject gameObject)
        {
            var index = objects.IndexOf(gameObject);
            var i = index / 8;
            var k = index % 8;
            gameObject.SetActive(false);
            lendObjectBitmap[i] &= (byte) ~(0b1 << k);
        }
    }
}