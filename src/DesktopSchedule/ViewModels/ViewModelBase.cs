using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 모든 ViewModel이 공통으로 사용하는 속성 변경 알림 기능을 제공합니다.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// 바인딩된 속성 값이 변경되었을 때 발생합니다.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 필드 값이 변경된 경우 값을 갱신하고 속성 변경 알림을 발생시킵니다.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// 지정한 속성의 변경 알림을 발생시킵니다.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}