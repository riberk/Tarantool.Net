using System;
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
    public class Serializer<T> : ISerializer<T>
    {
        [NotNull] private readonly MessagePackSerializer<T> _messagePackSerializer;

        public Serializer([NotNull] MessagePackSerializer<T> messagePackSerializer)
        {
            _messagePackSerializer = messagePackSerializer ?? throw new ArgumentNullException(nameof(messagePackSerializer));
        }
        public Task SerializeAsync(Stream s, T value, CancellationToken ct)
        {
            return _messagePackSerializer.PackAsync(s, value, ct);
        }
    }

    public class Deserializer<T> : IDeserializer<T>
    {
        [NotNull] private readonly MessagePackSerializer<T> _messagePackSerializer;

        public Deserializer([NotNull] MessagePackSerializer<T> messagePackSerializer)
        {
            _messagePackSerializer = messagePackSerializer ?? throw new ArgumentNullException(nameof(messagePackSerializer));
        }

        public Task<T> DeserializeAsync(Stream s, CancellationToken ct)
        {
            return _messagePackSerializer.UnpackAsync(s, ct);
        }
    }

    public class MessagePackResolver : ISerializerResolver, IDeserializerResolver
    {
        [NotNull] private readonly SerializationContext _ctx;

        public MessagePackResolver()
        {
            _ctx = new SerializationContext();
            _ctx.EnumSerializationOptions.SerializationMethod = EnumSerializationMethod.ByUnderlyingValue;
            _ctx.Serializers.Register(new TarantoolMapSerializer<Header>(_ctx));
            _ctx.Serializers.Register(new TarantoolMapSerializer<ErrorResponse>(_ctx));
            _ctx.Serializers.Register(new TarantoolMapSerializer<AuthenticationInfo>(_ctx));
        }
        ISerializer<T> ISerializerResolver.Resolve<T>()
        {
            return new Serializer<T>(_ctx.GetSerializer<T>());
        }

        IDeserializer<T> IDeserializerResolver.Resolve<T>()
        {
            return new Deserializer<T>(_ctx.GetSerializer<T>());
        }
    }

    public class TarantoolMapSerializer<T> : MessagePackSerializer<T>
    {
        public TarantoolMapSerializer(SerializationContext ownerContext) : base(ownerContext)
        {
            //var valueTuples = typeof(T)
            //    .GetProperties()
            //    .Select(x => (Prop: x, Attr:x.GetCustomAttribute<TarantoolMapPairAttribute>()))
            //    .Where(x => x.Attr != null);
            //var getterParam = Expression.Parameter(typeof(T));
            //var packerParam = Expression.Parameter(typeof(Packer));
            //var ctParam = Expression.Parameter(typeof(CancellationToken));
            //Expression<Func<SerializationContext>> ownerContextLambda = () => OwnerContext;
            //var ownerContextExp = ownerContextLambda.Body;
            //Expression<Func<SerializationContext>> ownerContextLambda = () => OwnerContext;

            //foreach (var valueTuple in valueTuples)
            //{
            //    var me = Expression.Property(getterParam, valueTuple.Prop);

            //    var getter = Expression.Lambda<Func<Packer, T, CancellationToken>>()
            //}

        }

        protected override void PackToCore(Packer packer, T objectTree)
        {
            PackToAsyncCore(packer, objectTree, CancellationToken.None).Wait();
        }

        protected override T UnpackFromCore(Unpacker unpacker)
        {
            return UnpackFromAsyncCore(unpacker, CancellationToken.None).Result;
        }

        protected override async Task PackToAsyncCore(Packer packer, T objectTree, CancellationToken cancellationToken)
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

        protected override async Task<T> UnpackFromAsyncCore(Unpacker unpacker, CancellationToken cancellationToken)
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
