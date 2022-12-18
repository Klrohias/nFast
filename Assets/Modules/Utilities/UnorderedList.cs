using System;
using System.Collections.Generic;

namespace Klrohias.NFast.Utilities
{
    public class UnorderedList<T>
    {
        public T[] Items = new T[16];
        private int length;
        public int Length => length;
        public T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }
        private void Realloc(int size)
        {
            var newItems = new T[size];
            Array.Copy(Items, 0, newItems, 0, length);
            Items = newItems;
        }
        public void AddRange(IList<T> itemList)
        {
            if (Items.Length - length < itemList.Count)
            {
                Realloc(Math.Max((int) (Items.Length * 1.5f), length + itemList.Count));
            }

            itemList.CopyTo(Items, length);
            length += itemList.Count;
        }

        public void RemoveAt(int index)
        {
            length--;
            Items[index] = Items[length];
            Items[length] = default;
        }
    }
}