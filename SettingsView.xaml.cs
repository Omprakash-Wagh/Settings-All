using System;
using System.Windows.Controls;

namespace SettingsAll;

public partial class SettingsView : UserControl
{
    private readonly PluginSettings _settings;
    private readonly Action _onSave;

    public SettingsView(PluginSettings settings, Action onSave)
    {
        InitializeComponent();
        _settings = settings;
        _onSave = onSave;
        
        DataContext = _settings;
        DllPathTextBox.LostFocus += (s, e) => _onSave?.Invoke();
    }
}
