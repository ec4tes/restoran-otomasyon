using CommunityToolkit.Mvvm.ComponentModel;

namespace RestoranOtomasyon.ViewModels;

/// <summary>
/// Tüm ViewModel'ler için temel sınıf
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    /// <summary>
    /// Busy durumunu ayarla
    /// </summary>
    protected void SetBusy(bool isBusy, string? message = null)
    {
        IsBusy = isBusy;
        BusyMessage = message;
    }

    /// <summary>
    /// Async işlem için busy wrapper
    /// </summary>
    protected async Task ExecuteBusyAsync(Func<Task> action, string? message = null)
    {
        try
        {
            SetBusy(true, message);
            await action();
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Async işlem için busy wrapper (with result)
    /// </summary>
    protected async Task<T?> ExecuteBusyAsync<T>(Func<Task<T>> action, string? message = null)
    {
        try
        {
            SetBusy(true, message);
            return await action();
        }
        finally
        {
            SetBusy(false);
        }
    }
}
