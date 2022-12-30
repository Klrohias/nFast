using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Klrohias.NFast.Utilities
{
    public class UnorderedList<T> : IEnumerable<T>
    {
        private T[] _items;
        private long _length = 0;
        public int Length => Convert.ToInt32(_length);
        public long LongLength => _length;

        public UnorderedList()
        {
            _items = new T[16];
        }
        public UnorderedList(int c)
        {
            _items = new T[c];
        }
        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        public T this[long index]
        {
            get => _items[index];
            set => _items[index] = value;
        }
        private void Reallocate(long size)
        {
            var newItems = new T[size];
            Array.Copy(_items, 0, newItems, 0, _length);
            _items = newItems;
        }

        public void Add(T item)
        {
            if (_length >= _items.Length)
            {
                Reallocate((int) (_items.Length * 1.5f));
            }

            _items[_length] = item;
            _length++;
        }

        public void AddIfNotExists(T item)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                if (object.ReferenceEquals(_items[i], item)) return;
            }
            Add(item);
        }

        public void AddRange(IList<T> itemList)
        {
            if (_items.Length - _length < itemList.Count)
            {
                Reallocate(Math.Max((long) (_items.Length * 1.5f), _length + itemList.Count));
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
            _items[index] = _items[_length];
            _items[_length] = default;
        }

        public void Remove(T obj)
        {
            for (int i = 0; i < _length; i++)
            {
                var item = _items[i];

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
                yield return _items[i];
            }
        }
        public IEnumerator<T> GetEnumerator()
        {
            return LongEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T[] AsArray()
        {
            var result = new T[_length];
            Array.Copy(_items, 0, result, 0, _length);
            return result;
        }
    }
}