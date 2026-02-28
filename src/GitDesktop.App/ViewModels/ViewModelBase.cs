using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// Base class for all ViewModels. Implements <see cref="INotifyPropertyChanged"/>
/// to support Avalonia data binding.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raises <see cref="PropertyChanged"/> for the given property.</summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Sets a backing field and raises <see cref="PropertyChanged"/> if the value changed.</summary>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
