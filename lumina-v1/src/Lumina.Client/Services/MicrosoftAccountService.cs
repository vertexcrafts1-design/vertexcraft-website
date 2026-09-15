using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;

namespace Lumina.Client.Services;

public sealed class MicrosoftAccountService
{
    private readonly JELoginHandler _handler = JELoginHandlerBuilder.BuildDefault();

    public MSession? Session { get; private set; }
    public bool IsSignedIn => Session is not null && Session.CheckIsValid();
    public string PlayerName => Session?.Username ?? "Nicht angemeldet";
    public string PlayerId => Session?.UUID ?? "";

    public async Task<bool> TrySilentAsync()
    {
        try
        {
            Session = await _handler.AuthenticateSilently();
            return IsSignedIn;
        }
        catch
        {
            Session = null;
            return false;
        }
    }

    public async Task<MSession> LoginInteractiveAsync()
    {
        Session = await _handler.AuthenticateInteractively();
        return Session;
    }

    public async Task SignOutAsync()
    {
        await _handler.Signout();
        Session = null;
    }
}
