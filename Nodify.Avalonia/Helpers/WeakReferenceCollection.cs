using System;
using System.Collections;
using System.Collections.Generic;

namespace Nodify.Avalonia.Helpers
{
    internal class WeakReferenceCollection<T> : IEnumerable<T> where T : class
    {
        private readonly int _cleanupThreshold;
        private readonly List<WeakReference<T>> _references;

        private int _cleanupCounter = 0;

        public WeakReferenceCollection(int initialCapacity, int cleanupThreshold = 32)
        {
            _cleanupThreshold = cleanupThreshold;
            _references = new List<WeakReference<T>>(initialCapacity);
        }

        public void Add(T item)
        {
            _references.Add(new WeakReference<T>(item));
            _cleanupCounter++;
            if (_cleanupCounter >= _cleanupThreshold)
            {
                _cleanupCounter = 0;
                Cleanup();
            }
        }

        public void Clear()
            => _references.Clear();

        public IEnumerator<T> GetEnumerator() => new Enumerator(_references);

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private void Cleanup()
        {
            int writeIndex = 0;
            for (int readIndex = 0; readIndex < _references.Count; readIndex++)
            {
                var reference = _references[readIndex];
                if (reference.TryGetTarget(out _))
                {
                    if (writeIndex != readIndex)
                        _references[writeIndex] = reference;
                    writeIndex++;
                }
            }

            if (writeIndex < _references.Count)
            {
                _references.RemoveRange(writeIndex, _references.Count - writeIndex);
            }
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly List<WeakReference<T>> _references;
            private int _index;
            private T? _current;

            public Enumerator(List<WeakReference<T>> references)
            {
                _references = references;
                _index = -1;
                _current = default;
            }

            public T Current => _current!;
            object IEnumerator.Current => _current!;

            public bool MoveNext()
            {
                while (++_index < _references.Count)
                {
                    if (_references[_index].TryGetTarget(out var target))
                    {
                        _current = target;
                        return true;
                    }
                }

                _current = null;
                return false;
            }

            public void Reset()
            {
                _index = -1;
                _current = null;
            }

            public readonly void Dispose() { }
        }
    }
}
