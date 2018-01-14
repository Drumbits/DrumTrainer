using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drumz.Common.Utils.Collections
{
    public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, int> keyIndices;
        private readonly List<TKey> keys;
        private readonly List<TValue> values;

        public int IndexOfKey(TKey key)
        {
            int keyIndex;
            if (!keyIndices.TryGetValue(key, out keyIndex))
                return -1;
            return keyIndex;
        }
        public TKey Key(int index)
        {
            return keys[index];
        }
        public TValue Value(int index)
        {
            return values[index];
        }
        public TValue this[TKey key]
        {
            get
            {
                int keyIndex = IndexOfKey(key);
                if (keyIndex == -1)
                    throw new KeyNotFoundException("Key not found: " + key);
                return values[keyIndex];
            }

            set
            {
                int keyIndex = IndexOfKey(key);
                if (keyIndex == -1)
                    throw new KeyNotFoundException("Key not found: " + key);
                values[keyIndex] = value;
            }
        }

        public int Count
        {
            get
            {
                return values.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return values;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Add(TKey key, TValue value)
        {
            int index = keyIndices.Count;
            keyIndices.Add(key, index);
            keys.Add(key);
            values.Add(value);
        }

        public void Clear()
        {
            keyIndices.Clear();
            keys.Clear();
            values.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            int index = IndexOfKey(item.Key);
            return index > -1 && Equals(values[index], item.Value);
        }

        public bool ContainsKey(TKey key)
        {
            return keyIndices.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return keys.Select(k => new KeyValuePair<TKey, TValue>(k, values[keyIndices[k]])).GetEnumerator();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public bool Remove(TKey key)
        {
            int index = IndexOfKey(key);
            if (index == -1) return false;
            keys.RemoveAt(index);
            values.RemoveAt(index);
            keyIndices.Remove(key);
            foreach (var alteredKey in keys.Skip(index))
                keyIndices[alteredKey]--;
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            int index;
            if (!keyIndices.TryGetValue(key, out index)) return false;
            value = values[index];
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
