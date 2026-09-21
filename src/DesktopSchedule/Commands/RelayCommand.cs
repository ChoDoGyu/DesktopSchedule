using System.Windows.Input;

namespace DesktopSchedule.Commands;

/// <summary>
/// View의 사용자 입력을 ViewModel의 메서드와 연결하는 기본 Command 구현입니다.
/// </summary>
public class RelayCommand : ICommand
{
    // Command가 실행될 때 실제로 수행할 동작을 저장합니다.
    private readonly Action<object?> _execute;

    // Command를 현재 실행할 수 있는지 판단하는 조건을 저장합니다.
    // 조건이 필요하지 않은 Command도 있으므로 null을 허용합니다.
    private readonly Predicate<object?>? _canExecute;

    /// <summary>
    /// 실행할 동작과 선택적인 실행 가능 조건을 받아 RelayCommand를 생성합니다.
    /// </summary>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        // 실행할 동작이 전달되지 않으면 Command 자체가 의미가 없으므로 예외를 발생시킵니다.
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        // 실행 가능 조건은 없어도 되므로 그대로 저장합니다.
        _canExecute = canExecute;
    }

    /// <summary>
    /// 현재 Command를 실행할 수 있는지 여부를 반환합니다.
    /// </summary>
    public bool CanExecute(object? parameter)
    {
        // 실행 조건이 따로 없으면 항상 실행 가능하도록 true를 반환합니다.
        // 조건이 있다면 해당 조건의 결과를 반환합니다.
        return _canExecute?.Invoke(parameter) ?? true;
    }

    /// <summary>
    /// Command에 연결된 실제 동작을 실행합니다.
    /// </summary>
    public void Execute(object? parameter)
    {
        // 생성자에서 전달받은 실행 동작을 호출합니다.
        _execute(parameter);
    }

    /// <summary>
    /// Command의 실행 가능 상태가 변경되었음을 WPF에 알립니다.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        // WPF의 CommandManager가 다시 실행 가능 여부를 검사할 때 이벤트를 연결합니다.
        add => CommandManager.RequerySuggested += value;

        // 더 이상 이벤트를 사용하지 않을 때 연결을 제거합니다.
        remove => CommandManager.RequerySuggested -= value;
    }
}