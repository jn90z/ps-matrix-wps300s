using System.IO.Ports;
using System.Text;
namespace MatrixWps300s.Services;

/// <summary>SCPI transport over a USB CDC / USB-to-serial COM port. All queries are serialized.</summary>
public sealed class UsbSerialScpiClient : IDisposable
{
    private SerialPort? _port;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    public bool Connected => _port?.IsOpen == true;
    public static string[] Ports() => SerialPort.GetPortNames().OrderBy(x => x).ToArray();
    public void Connect(string portName)
    {
        if (Connected) throw new InvalidOperationException("Already connected.");
        _port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None, NewLine = "\r\n",
            Encoding = Encoding.ASCII, ReadTimeout = 900, WriteTimeout = 900,
            DtrEnable = false, RtsEnable = false
        };
        try { _port.Open(); _port.DiscardInBuffer(); }
        catch { _port.Dispose(); _port = null; throw; }
    }
    public async Task SendAsync(string command)
    {
        await _mutex.WaitAsync();
        try { await Task.Run(() => RequirePort().WriteLine(command)); }
        finally { _mutex.Release(); }
    }
    public async Task<string> QueryAsync(string command)
    {
        await _mutex.WaitAsync();
        try
        {
            return await Task.Run(() =>
            {
                var port = RequirePort();
                port.DiscardInBuffer();
                port.WriteLine(command);
                return port.ReadLine().Trim();
            });
        }
        finally { _mutex.Release(); }
    }
    public async Task DisconnectAsync(bool turnOff)
    {
        await _mutex.WaitAsync();
        try
        {
            if (_port is not null)
            {
                if (_port.IsOpen && turnOff)
                {
                    try { await Task.Run(() => _port.WriteLine(MatrixScpi.OutputOff)); }
                    catch { /* Caller should verify PSU output state after link failures. */ }
                }
                _port.Close(); _port.Dispose(); _port = null;
            }
        }
        finally { _mutex.Release(); }
    }
    private SerialPort RequirePort() => Connected ? _port! : throw new InvalidOperationException("No USB COM port connected.");
    public void Dispose() { _port?.Dispose(); _mutex.Dispose(); }
}
