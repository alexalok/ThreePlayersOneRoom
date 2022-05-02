using Lib.AspNetCore.ServerSentEvents;

namespace ThreePlayersOneRoom.Services.SSE;

class CookieBasedServerSentEventsClientIdProvider : IServerSentEventsClientIdProvider
{
    const string CookieName = ".ServerSentEvents.Guid";

    public Guid AcquireClientId(HttpContext context)
    {
        string? cookieValue = context.Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(cookieValue) || !Guid.TryParse(cookieValue, out var clientId))
        {
            clientId = Guid.NewGuid();

            context.Response.Cookies.Append(CookieName, clientId.ToString());
        }

        return clientId;
    }

    public void ReleaseClientId(Guid clientId, HttpContext context)
    {
        try
        {
            context.Response.Cookies.Delete(CookieName);
        }
        catch
        {
        }
    }
}