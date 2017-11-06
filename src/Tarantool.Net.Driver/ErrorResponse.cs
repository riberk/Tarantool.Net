namespace Tarantool.Net.Driver
{
    public struct ErrorResponse
    {
        public ErrorResponse(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}