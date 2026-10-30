using System.Globalization;
namespace MatrixWps300s.Services;
public static class MatrixScpi
{
    public const double MaxVoltage = 150.0;
    public const double MaxCurrent = 5.0;
    public const double MaxPower = 300.0;
    public const string Identify = "*IDN?";
    public const string Remote = "SYST:REM";
    public const string Local = "SYST:LOC";
    public const string MeasureVoltage = "MEAS:VOLT?";
    public const string MeasureCurrent = "MEAS:CURR?";
    public const string MeasurePower = "MEAS:POW?";
    public const string QueryOutput = "OUTP?";
    public const string OutputOn = "OUTP ON";
    public const string OutputOff = "OUTP OFF";
    public static string SetVoltage(double value) => "VOLT " + value.ToString("0.000", CultureInfo.InvariantCulture);
    public static string SetCurrent(double value) => "CURR " + value.ToString("0.0000", CultureInfo.InvariantCulture);
    public static string Apply(double voltage, double current) => "APPL " + voltage.ToString("0.000", CultureInfo.InvariantCulture) + "," + current.ToString("0.0000", CultureInfo.InvariantCulture);
    public static double ParseNumber(string response) => double.Parse(response.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
}
