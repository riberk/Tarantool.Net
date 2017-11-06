using System;

namespace Tarantool.Net.Driver
{
    public class TarantoolException : Exception
    {
        public ErrorCode ErrorCode { get; }

        public string TarantoolMessage { get; }

        public TarantoolException(ErrorCode errorCode, string tarantoolMessage)
            : base($"Tarantool response error \nCode: {errorCode}\nMessage: {tarantoolMessage}")
        {
            ErrorCode = errorCode;
            TarantoolMessage = tarantoolMessage;
        }

    }
}
