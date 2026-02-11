using RestoranOtomasyon.Models;

namespace RestoranOtomasyon.Services;

/// <summary>
/// Oturum yönetimi için current user bilgisi
/// </summary>
public interface ISessionService
{
    Kullanici? CurrentUser { get; }
    bool IsLoggedIn { get; }
    void Login(Kullanici user);
    void Logout();
    bool HasPermission(string permission);
    event EventHandler<Kullanici?>? UserChanged;
}

/// <summary>
/// Session servisi implementasyonu
/// </summary>
public class SessionService : ISessionService
{
    private Kullanici? _currentUser;

    public Kullanici? CurrentUser => _currentUser;
    public bool IsLoggedIn => _currentUser != null;

    public event EventHandler<Kullanici?>? UserChanged;

    public void Login(Kullanici user)
    {
        _currentUser = user;
        UserChanged?.Invoke(this, user);
    }

    public void Logout()
    {
        _currentUser = null;
        UserChanged?.Invoke(this, null);
    }

    public bool HasPermission(string permission)
    {
        return _currentUser?.HasPermission(permission) ?? false;
    }
}
