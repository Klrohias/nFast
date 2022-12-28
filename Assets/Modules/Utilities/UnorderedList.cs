using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Klrohias.NFast.Utilities
{
    public class UnorderedList<T> : IEnumerable<T>
    {
        public T[] Items = new T[16];
        private int length;
        public int Length => length;
        public T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        private void Reallocate(int size)
        {
            var newItems = new T[size];
            Array.Copy(Items, 0, newItems, 0, length);
            Items = newItems;
        }

        public void Add(T item)
        {
            if (length + 1 >= Items.Length)
            {
                Reallocate((int) (Items.Length * 1.5f));
            }
            Items[length] = item;
            length++;
        }

        public void AddIfNotExists(T item)
        {
            for (int i = 0; i < Items.Length; i++)
            {
                if (object.ReferenceEquals(Items[i], item)) return;
            }
            Add(item);
        }

        public void AddRange(IList<T> itemList)
        {
            if (Items.Length - length < itemList.Count)
            {
                Reallocate(Math.Max((int) (Items.Length * 1.5f), length + itemList.Count));
            }

            foreach (var item in itemList)
            {
                Add(item);
            }
        }
        
        /// <summary>
        /// note: when use RemoveAt in a for-loop, make index minus one, or it will cause bugs
        /// </summary>
        /// <param name="index">the index of element which will be removed</param>
        public void RemoveAt(int index)
        {
            length--;
            Items[index] = Items[length];
            Items[length] = default;
        }

        public void Remove(T obj)
        {
            for (int i = 0; i < length; i++)
            {
                var item = Items[i];

                if (!object.ReferenceEquals(item, obj)) continue;
                RemoveAt(i);
                break;
            }
        }

        public void Clear()
        {
            length = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Items.Take(length).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}