using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public class ErrorResponse
    {
        [MapKey(Key.Error)]
        public string Message { get; set; }
    }
}
