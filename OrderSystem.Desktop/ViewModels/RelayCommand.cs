using System.Windows.Input;

namespace OrderSystem.Desktop.ViewModels;

public sealed class RelayCommand(Action execute) : ICommand
{
    private readonly Action _execute = execute;

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        _execute();
    }
}
