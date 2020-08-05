using System.Collections.Generic;

namespace Envis10n.AsyncTCP.Lib
{
    class ConcurrentList<T> : AutoMutex<List<T>>
    {
        public ConcurrentList() : base(new List<T>()) { }
    }
    class ConcurrentDictionary<TKey, TVal> : AutoMutex<Dictionary<TKey, TVal>>
    {
        public ConcurrentDictionary() : base(new Dictionary<TKey, TVal>()) { }
    }
}