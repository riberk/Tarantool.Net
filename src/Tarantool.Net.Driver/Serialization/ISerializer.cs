using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Tarantool.Net.Driver.Serialization
{
    public interface ISerializer<in T>
    {
        /// <summary>
        ///     Serialize object as message pack to the stream
        /// </summary>
        /// <param name="s">Destiation stream</param>
        /// <param name="value">Serialized value</param>
        /// <param name="ct">Cancellation token</param>
        [NotNull]
        Task SerializeAsync(Stream s, T value, CancellationToken ct);
    }

    public interface IMessagePack
    {
        Task<bool> ReadAsync(Stream stream);
    }

    public enum MessagePackValueType
    {
        PositiveFixint,
        Fixmap,
        Fixarray,
        Fixstr,
        Nil,
        False,
        True,
        Bin8,
        Bin16,
        Bin32,
        Ext8,
        Ext16,
        Ext32,
        Float32,
        Float64,
        Uint8,
        Uint16,
        Uint32,
        Uint64,
        Int8,
        Int16,
        Int32,
        Int64,
        Fixext1,
        Fixext2,
        Fixext4,
        Fixext8,
        Fixext16,
        Str8,
        Str16,
        Str32,
        Array16,
        Array32,
        Map16,
        Map32,
        NegativeFixint
    }

    public struct MessagePackValue
    {
        private ArraySegment<byte> RawValue { get; }
    }
}
