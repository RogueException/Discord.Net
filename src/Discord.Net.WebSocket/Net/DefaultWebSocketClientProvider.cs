using System;
using System.Net;

namespace Discord.Net.WebSockets
{
    public static class DefaultWebSocketProvider
    {
        public static readonly WebSocketProvider Instance = Create();

        public static WebSocketProvider Create(IWebProxy proxy = null) => () =>
        {
            try
            {
                return new DefaultWebSocketClient(proxy);
            }
            catch (PlatformNotSupportedException ex)
            {
                throw new PlatformNotSupportedException(
                    "The default WebSocketProvider is not supported on this platform.", ex);
            }
        };
    }
}
