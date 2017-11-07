using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MsgPack;
using MsgPack.Serialization;
using Tarantool.Net.Driver;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.MessagePackCli
{
    public class SerializerDeserializer<T> : ISerializer<T>, IDeserializer<T>
    {
        [NotNull] private readonly MessagePackSerializer<T> _messagePackSerializer;

        public SerializerDeserializer([NotNull] MessagePackSerializer<T> messagePackSerializer)
        {
            _messagePackSerializer = messagePackSerializer ?? throw new ArgumentNullException(nameof(messagePackSerializer));
        }
        public Task SerializeAsync(Stream s, T value, CancellationToken ct)
        {
            return _messagePackSerializer.PackAsync(s, value, ct);
        }

        public Task<T> DeserializeAsync(Stream s, CancellationToken ct)
        {
            return _messagePackSerializer.UnpackAsync(s, ct);
        }
    }

    public class MapSerializerDeserializer<T> : IDeserializer<T>, ISerializer<T>
    {
        [NotNull]
        public SerializationContext OwnerContext { get; }

        public MapSerializerDeserializer([NotNull] SerializationContext ownerContext)
        {
            OwnerContext = ownerContext ?? throw new ArgumentNullException(nameof(ownerContext));
        }

        public async Task<T> DeserializeAsync(Stream s, CancellationToken ct)
        {
            var unpacker = Unpacker.Create(s,
                new PackerUnpackerStreamOptions() {OwnsStream = false, WithBuffering = false},
                new UnpackerOptions {ValidationLevel = UnpackerValidationLevel.None});
            if (!(await unpacker.ReadAsync(ct)))
            {
                throw new InvalidOperationException();
            }
            if (unpacker.LastReadData.IsNil)
            {
                throw new InvalidOperationException("null");
            }
            return await UnpackFromAsyncCore(unpacker, ct);
        }

        private async Task<T> UnpackFromAsyncCore(Unpacker unpacker, CancellationToken cancellationToken)
        {
            var valueTuples = typeof(T).GetProperties().Select(x => (x, x.GetCustomAttribute<MapKeyAttribute>())).Where(x => x.Item2 != null).ToList();
            long mapLength;
            mapLength = unpacker.IsMapHeader ? unpacker.LastReadData.AsInt64() : (await unpacker.ReadMapLengthAsync(cancellationToken)).ThrowIfUnsuccess();

            var res = Activator.CreateInstance<T>();
            var keySerializer = OwnerContext.GetSerializer<Key>();
            for (var i = 0; i < mapLength; i++)
            {
                if (!await unpacker.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException();
                }
                var key = await keySerializer.UnpackFromAsync(unpacker, cancellationToken);
                var t = valueTuples.Single(x => x.Item2.Key == key);
                if (!await unpacker.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException();
                }
                var val = await OwnerContext.GetSerializer(t.Item1.PropertyType).UnpackFromAsync(unpacker, cancellationToken);
                t.Item1.SetValue(res, val);
            }
            return res;
        }

        private async Task PackToAsyncCore(Packer packer, T objectTree, CancellationToken cancellationToken)
        {
            var valueTuples = objectTree
                    .GetType()
                    .GetProperties()
                    .Select(x => (x, x.GetCustomAttribute<MapKeyAttribute>()))
                    .Where(x => x.Item2 != null)
                    .Select(x => new
                    {
                        Prop = x.Item1,
                        Attr = x.Item2,
                        Val = x.Item1.GetValue(objectTree)
                    })
                    .Where(x => x.Val != null)
                    .ToList();
            await packer.PackMapHeaderAsync(valueTuples.Count, cancellationToken);
            var keySerializer = OwnerContext.GetSerializer<Key>();
            foreach (var valueTuple in valueTuples)
            {
                await keySerializer.PackToAsync(packer, valueTuple.Attr.Key, cancellationToken);
                await OwnerContext.GetSerializer(valueTuple.Prop.PropertyType).PackToAsync(packer, valueTuple.Val, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task SerializeAsync(Stream s, T value, CancellationToken ct)
        {
            
            var packer = Packer.Create(s, PackerCompatibilityOptions.None, false);
            if (value == null)
            {
                // ReSharper disable once PossibleNullReferenceException
                await packer.PackNullAsync(ct).ConfigureAwait(false);
                return;
            }
            await PackToAsyncCore(packer, value, ct).ConfigureAwait(false);
            if (s is MemoryStream ms)
            { 
                var arr = ms.ToArray();
                var pos = ms.Position;
                ms.Seek(5, SeekOrigin.Begin);
                var up = Unpacker.Create(ms, false);
                var h = up.ReadItemData();
                var b = up.ReadItemData();
                ms.Position = pos;
            }
        }
    }

    public class MessagePackResolver : ISerializerResolver, IDeserializerResolver
    {
        [NotNull] private readonly SerializationContext _ctx;
        [NotNull] private readonly Dictionary<Type, object> _internalMapSerializers = new Dictionary<Type, object>();

        public MessagePackResolver()
        {
            _ctx = new SerializationContext();
            _ctx.EnumSerializationOptions.SerializationMethod = EnumSerializationMethod.ByUnderlyingValue;
            Add<Header>();
            Add<ErrorResponse>();
            Add<AuthenticationInfo>();
        }

        ISerializer<T> ISerializerResolver.Resolve<T>()
        {
            if (_internalMapSerializers.TryGetValue(typeof(T), out var res))
            {
                return (ISerializer<T>)res;
            }
            var messagePackSerializer = _ctx.GetSerializer<T>();
            return new SerializerDeserializer<T>(messagePackSerializer);
        }

        IDeserializer<T> IDeserializerResolver.Resolve<T>()
        {
            if (_internalMapSerializers.TryGetValue(typeof(T), out var res))
            {
                return (IDeserializer<T>)res;
            }
            var messagePackSerializer = _ctx.GetSerializer<T>();
            return new SerializerDeserializer<T>(messagePackSerializer);
        }

        private void Add<T>()
        {
            _internalMapSerializers.Add(typeof(T), new MapSerializerDeserializer<T>(_ctx));
        }
    }

    public static class AsyncReadResultExtensions
    {
        public static T ThrowIfUnsuccess<T>(this AsyncReadResult<T> readResult)
        {
            if (!readResult.Success)
            {
                throw new InvalidOperationException();
            }
            return readResult.Value;
        }
    }
}
