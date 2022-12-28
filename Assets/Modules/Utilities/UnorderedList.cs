using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Klrohias.NFast.Utilities
{
    public class UnorderedList<T> : IEnumerable<T>
    {
        public T[] Items = new T[16];
        private long _length = 0;
        public int Length => Convert.ToInt32(_length);
        public long LongLength => _length;
        public T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public T this[long index]
        {
            get => Items[index];
            set => Items[index] = value;
        }
        private void Reallocate(long size)
        {
            var newItems = new T[size];
            Array.Copy(Items, 0, newItems, 0, _length);
            Items = newItems;
        }

        public void Add(T item)
        {
            if (_length >= Items.Length)
            {
                Reallocate((int) (Items.Length * 1.5f));
            }

            Items[_length] = item;
            _length++;
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
            if (Items.Length - _length < itemList.Count)
            {
                Reallocate(Math.Max((long) (Items.Length * 1.5f), _length + itemList.Count));
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
            if (_length == 0) throw new IndexOutOfRangeException();
            _length--;
            Items[index] = Items[_length];
            Items[_length] = default;
        }

        public void Remove(T obj)
        {
            for (int i = 0; i < _length; i++)
            {
                var item = Items[i];

                if (!object.ReferenceEquals(item, obj)) continue;
                RemoveAt(i);
                break;
            }
        }

        public void Clear()
        {
            _length = 0;
        }

        private IEnumerator<T> LongEnumerator()
        {
            for (long i = 0; i < _length; i++)
            {
                yield return Items[i];
            }
        }
        public IEnumerator<T> GetEnumerator()
        {
            if (LongLength >= Length)
            {
                return LongEnumerator();
            }
            return Items.Take(Length).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}