using System;

namespace Tarantool.Net.Driver
{
    public struct AsyncResult<T>
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
        public AsyncResult(T value) : this()
        {
            HasValue = true;
            Value = value;
        }

        public bool HasValue { get; }

        public T Value { get; }
    }
}
