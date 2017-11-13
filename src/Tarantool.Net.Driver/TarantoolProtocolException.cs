using System;

namespace Tarantool.Net.Driver
{
    public class TarantoolProtocolException : Exception
    {
        public TarantoolProtocolException(string message) : base(message)
        {
        }
    }
}