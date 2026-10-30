using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using MatrixWps300s.Services;
namespace MatrixWps300s;
public partial class MainWindow : Window
{
    private readonly UsbSerialScpiClient _device = new();
    private readonly DispatcherTimer _poll;
    private bool _busy, _pollBusy, _closing;
    private double _voltage, _current;
    public MainWindow()
    {
        InitializeComponent();
        RefreshPorts();
        _poll = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _poll.Tick += Poll_Tick;
        _poll.Start();
        AddLog("Ready. Connect the Matrix USB serial interface. Output is not enabled automatically.");
    }
    private void AddLog(string line)
    {
        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
        if (LogBox.Text.Length > 18000) LogBox.Text = LogBox.Text[^14000..];
        LogBox.ScrollToEnd();
    }
    private void RefreshPorts()
    {
        string? prior = PortCombo.SelectedItem as string;
        PortCombo.ItemsSource = UsbSerialScpiClient.Ports();
        if (prior is not null && PortCombo.Items.Contains(prior)) PortCombo.SelectedItem = prior;
        else if (PortCombo.Items.Count > 0) PortCombo.SelectedIndex = 0;
    }
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshPorts();
    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            if (_device.Connected)
            {
                await _device.DisconnectAsync(OffOnDisconnectCheck.IsChecked == true);
                ConnectButton.Content = "Connect";
                ConnectionText.Text = "Disconnected";
                OutputStateText.Text = "OUTPUT UNKNOWN";
                VoltageDisplay.Text = "---.---"; CurrentDisplay.Text = "-.----"; PowerDisplay.Text = "---.---";
                AddLog("Disconnected. OFF request sent if selected; physical output state not verified.");
            }
            else
            {
                if (PortCombo.SelectedItem is not string port) { AddLog("Select a USB COM port."); return; }
                _device.Connect(port);
                ConnectButton.Content = "Disconnect";
                ConnectionText.Text = "Connected: " + port;
                AddLog("USB serial port opened: " + port);
                try { AddLog("IDN: " + await _device.QueryAsync(MatrixScpi.Identify)); }
                catch (Exception ex) { AddLog("Identification unavailable: " + ex.Message); }
                try { await _device.SendAsync(MatrixScpi.Remote); AddLog("Remote mode requested."); }
                catch (Exception ex) { AddLog("Remote mode failed: " + ex.Message); }
                await ReadOutputStateAsync();
            }
        }
        catch (Exception ex) { AddLog("Connection error: " + ex.Message); }
        finally { _busy = false; }
    }
    private async Task ExecuteAsync(string command)
    {
        if (!_device.Connected) { AddLog("Not connected."); return; }
        try { await _device.SendAsync(command); AddLog("TX " + command); }
        catch (Exception ex) { AddLog("Command error: " + ex.Message); }
    }
    private async void Remote_Click(object sender, RoutedEventArgs e) => await ExecuteAsync(MatrixScpi.Remote);
    private async void Local_Click(object sender, RoutedEventArgs e) => await ExecuteAsync(MatrixScpi.Local);
    private static bool TryValue(string text, double max, out double value) =>
        (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
         double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) &&
        double.IsFinite(value) && value >= 0 && value <= max;
    private async void SetCurrent_Click(object sender, RoutedEventArgs e)
    {
        if (!TryValue(CurrentBox.Text, MatrixScpi.MaxCurrent, out double i)) { AddLog("Current must be 0–5 A."); return; }
        await ExecuteAsync(MatrixScpi.SetCurrent(i));
    }
    private async void SetVoltage_Click(object sender, RoutedEventArgs e)
    {
        if (!TryValue(VoltageBox.Text, MatrixScpi.MaxVoltage, out double v)) { AddLog("Voltage must be 0–150 V."); return; }
        await ExecuteAsync(MatrixScpi.SetVoltage(v));
    }
    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!TryValue(CurrentBox.Text, MatrixScpi.MaxCurrent, out double i) ||
            !TryValue(VoltageBox.Text, MatrixScpi.MaxVoltage, out double v)) { AddLog("Enter valid current AND voltage setpoints."); return; }
        if (v * i > MatrixScpi.MaxPower) { AddLog("Requested V × I exceeds 300 W nameplate limit."); return; }
        await ExecuteAsync(MatrixScpi.Apply(v, i));
    }
    private async void OutputOn_Click(object sender, RoutedEventArgs e)
    {
        if (!_device.Connected) { AddLog("Not connected."); return; }
        if (MessageBox.Show("Enable power supply output? Confirm your DUT and limits are safe.", "Enable output", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await ExecuteAsync(MatrixScpi.OutputOn);
        await ReadOutputStateAsync();
    }
    private async void OutputOff_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync(MatrixScpi.OutputOff);
        await ReadOutputStateAsync();
    }
    private async Task ReadOutputStateAsync()
    {
        if (!_device.Connected) return;
        try
        {
            var s = await _device.QueryAsync(MatrixScpi.QueryOutput);
            OutputStateText.Text = s == "1" || s.Equals("ON", StringComparison.OrdinalIgnoreCase) ? "OUTPUT ON" :
                                   s == "0" || s.Equals("OFF", StringComparison.OrdinalIgnoreCase) ? "OUTPUT OFF" : "OUTPUT: " + s;
        }
        catch (Exception ex) { OutputStateText.Text = "OUTPUT UNKNOWN"; AddLog("Output state query: " + ex.Message); }
    }
    private async void Poll_Tick(object? sender, EventArgs e)
    {
        if (_pollBusy || _busy || !_device.Connected || PollCheck.IsChecked != true) return;
        _pollBusy = true;
        try
        {
            _voltage = MatrixScpi.ParseNumber(await _device.QueryAsync(MatrixScpi.MeasureVoltage));
            _current = MatrixScpi.ParseNumber(await _device.QueryAsync(MatrixScpi.MeasureCurrent));
            VoltageDisplay.Text = _voltage.ToString("000.000", CultureInfo.InvariantCulture);
            CurrentDisplay.Text = _current.ToString("0.0000", CultureInfo.InvariantCulture);
            PowerDisplay.Text = (_voltage * _current).ToString("000.000", CultureInfo.InvariantCulture);
        }
        catch (Exception ex) { AddLog("Readback error: " + ex.Message); }
        finally { _pollBusy = false; }
    }
    private void Clear_Click(object sender, RoutedEventArgs e) => LogBox.Clear();
    private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_closing) return;
        if (_device.Connected)
        {
            e.Cancel = true;
            _closing = true;
            _poll.Stop();
            try { await _device.DisconnectAsync(OffOnDisconnectCheck.IsChecked == true); }
            catch (Exception ex) { AddLog("Shutdown warning: " + ex.Message); }
            Close();
        }
        else _poll.Stop();
    }
}
