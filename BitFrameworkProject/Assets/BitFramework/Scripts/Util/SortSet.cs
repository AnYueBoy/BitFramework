using System;
using System.Collections;
using System.Collections.Generic;

namespace BitFramework.Util
{
    public sealed class SortSet<TElement, TScore> : IEnumerable<TElement>
        where TScore : IComparable<TScore>
    {
        private readonly int maxLevel;

        /// <summary>
        /// 头结点
        /// </summary>
        private readonly SkipNode header;

        private readonly double probability;
        private readonly Random random = new Random();
        private readonly IComparer<TScore> comparer;
        private readonly Dictionary<TElement, TScore> elementMapping = new Dictionary<TElement, TScore>();
        private int level;

        /// <summary>
        /// 尾结点
        /// </summary>
        private SkipNode tail;

        public SortSet(int maxLevel = 32, double probable = 0.25)
        {
            Guard.Requires<ArgumentOutOfRangeException>(probable < 1);
            Guard.Requires<ArgumentOutOfRangeException>(probable > 0);

            maxLevel = Math.Max(1, maxLevel);
            level = 1;
            this.maxLevel = maxLevel;

            probability = probable * 0xFFFF;

            header = new SkipNode()
            {
                Level = new SkipNode.SkipNodeLevel[maxLevel]
            };
        }

        public SortSet(int maxLevel, double probability, IComparer<TScore> comparer) : this(maxLevel, probability)
        {
            Guard.Requires<ArgumentOutOfRangeException>(comparer != null);
            this.comparer = comparer;
        }

        public int Count { get; private set; }

        public TElement this[int rank] => GetElementByRank(rank);

        public void Clear()
        {
            for (int i = 0; i < header.Level.Length; i++)
            {
                header.Level[i].Span = 0;
                header.Level[i].Forward = null;
            }

            tail = null;
            level = 1;
            Count = 0;
            elementMapping.Clear();
        }

        public IEnumerable<TElement> GetIterator(bool forward = true)
        {
            return new Iterator(this, forward);
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return new Iterator(this, true);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TElement[] ToArray()
        {
            var elements = new TElement[Count];
            var node = header.Level[0];
            var i = 0;
            while (node.Forward != null)
            {
                elements[i++] = node.Forward.Element;
                node = node.Forward.Level[0];
            }

            return elements;
        }

        public TElement GetElementByRank(int rank)
        {
            rank = Math.Max(0, rank);
            rank += 1;

            // 已访问的节点
            var traversed = 0;
            var cursor = header;
            for (int i = level - 1; i >= 0; i--)
            {
                while (cursor.Level[i].Forward != null && (traversed + cursor.Level[i].Span) <= rank)
                {
                    traversed += cursor.Level[i].Span;
                    cursor = cursor.Level[i].Forward;
                }

                if (traversed == rank)
                {
                    return cursor.Element;
                }
            }

            if (Count > 0)
            {
                throw new ArgumentOutOfRangeException($"Rank is out of range [{rank}]");
            }

            throw new InvalidOperationException("SortSet is Null.");
        }

        public struct Iterator : IEnumerator<TElement>, IEnumerable<TElement>
        {
            private readonly SortSet<TElement, TScore> collection;
            private readonly bool forward;
            private SkipNode current;

            internal Iterator(SortSet<TElement, TScore> collection, bool forward)
            {
                this.collection = collection;
                this.forward = forward;
                current = forward ? collection.header : null;
            }

            public bool MoveNext()
            {
                if (forward)
                {
                    do
                    {
                        current = current.Level[0].Forward;
                    } while (current != null && current.IsDeleted);

                    return current != null;
                }

                if (current == null)
                {
                    do
                    {
                        current = collection.tail;
                    } while (current != null && current.IsDeleted);

                    return current != null;
                }

                do
                {
                    current = current.Backward;
                } while (current != null && current.IsDeleted);

                return current != null;
            }

            public void Reset()
            {
                current = forward ? collection.header : null;
            }

            object IEnumerator.Current => Current;

            public TElement Current => current.Element;


            public void Dispose()
            {
                // ignore.
            }

            public IEnumerator<TElement> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class SkipNode
        {
            public TElement Element { get; set; }
            public TScore Score { get; set; }
            public bool IsDeleted { get; set; }

            /// <summary>
            /// 前继节点
            /// </summary>
            public SkipNode Backward { get; set; }

            public SkipNodeLevel[] Level { get; set; }

            internal struct SkipNodeLevel
            {
                /// <summary>
                /// 后继节点
                /// </summary>
                internal SkipNode Forward;

                /// <summary>
                /// 代表有多少节点与下个节点相交
                /// </summary>
                internal int Span;
            }
        }
    }
}