using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Upright.App;

public partial class SettingsWindow : Window
{
    public SettingsWindow(double deadZone, double warningDelaySeconds)
    {
        InitializeComponent();
        DeadZoneTextBox.Text =
            deadZone.ToString("0.##", CultureInfo.InvariantCulture);
        WarningDelayTextBox.Text =
            warningDelaySeconds.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public MonitorSettingsValues? SavedValues { get; private set; }

    private void OnWindowDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left &&
            e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        if (!MonitorSettingsInput.TryParse(
                DeadZoneTextBox.Text,
                WarningDelayTextBox.Text,
                out MonitorSettingsValues? values,
                out string error))
        {
            ValidationText.Text = error;
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        SavedValues = values;
        DialogResult = true;
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
