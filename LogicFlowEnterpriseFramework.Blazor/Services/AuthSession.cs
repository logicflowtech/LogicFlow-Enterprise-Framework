using LogicFlowEnterpriseFramework.Blazor.Models;

namespace LogicFlowEnterpriseFramework.Blazor.Services;

public sealed class AuthSession
{
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public UserProfile? User { get; private set; }
    public bool IsReady { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken) && ExpiresAt > DateTimeOffset.UtcNow;

    public event Action? Changed;

    public void SignIn(AuthResponse authResponse)
    {
        AccessToken = authResponse.AccessToken;
        RefreshToken = authResponse.RefreshToken;
        ExpiresAt = authResponse.ExpiresAt;
        User = authResponse.User;
        IsReady = true;
        Changed?.Invoke();
    }

    public void Restore(AuthSessionState state)
    {
        AccessToken = state.AccessToken;
        RefreshToken = state.RefreshToken;
        ExpiresAt = state.ExpiresAt;
        User = state.User;
        IsReady = true;
        Changed?.Invoke();
    }

    public void MarkReady()
    {
        IsReady = true;
        Changed?.Invoke();
    }

    public void UpdateProfile(UserProfile profile)
    {
        User = profile;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        AccessToken = null;
        RefreshToken = null;
        ExpiresAt = null;
        User = null;
        IsReady = true;
        Changed?.Invoke();
    }

    public AuthSessionState? Export()
    {
        if (string.IsNullOrWhiteSpace(AccessToken) || string.IsNullOrWhiteSpace(RefreshToken) || ExpiresAt is null || User is null)
        {
            return null;
        }

        return new AuthSessionState
        {
            AccessToken = AccessToken,
            RefreshToken = RefreshToken,
            ExpiresAt = ExpiresAt.Value,
            User = User
        };
    }
}

public sealed class AuthSessionState
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required UserProfile User { get; init; }
}
