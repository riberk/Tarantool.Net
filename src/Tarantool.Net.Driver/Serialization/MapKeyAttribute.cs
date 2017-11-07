using System;

namespace Tarantool.Net.Driver.Serialization
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class MapKeyAttribute : Attribute
    {
        public Key Key { get; }

        public MapKeyAttribute(Key key)
        {
            Key = key;
        }
    }
}