using System;
using System.IO.Ports;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HaengSungAOI_WPF.Machine;

namespace HaengSungAOI_WPF.Services.Machine
{
    public class ScanOutService : IScanOutService
    {
        private readonly ILogger<ScanOutService> _logger;
        private readonly IErrorService _errorService;
        private SerialPort _serialPort;

        public bool IsOpen => _serialPort?.IsOpen ?? false;

        public event EventHandler<ScanOutReceivedEventArgs> DataReceived;

        public ScanOutService(ILogger<ScanOutService> logger, IErrorService errorService)
        {
            _logger = logger;
            _errorService = errorService;
        }

        public void Open(string portName, int baudRate = 115200)
        {
            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen) _serialPort.Close();
                    _serialPort.Dispose();
                }

                _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
                _serialPort.DataReceived += (s, e) =>
                {
                    try
                    {
                        string response = _serialPort.ReadLine().Trim();
                        if (!string.IsNullOrEmpty(response))
                        {
                            _logger.LogInformation($"ScanOut received: {response}");
                            DataReceived?.Invoke(this, new ScanOutReceivedEventArgs(response));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading from ScanOut serial port");
                    }
                };

                _serialPort.Open();
                _logger.LogInformation($"ScanOut serial port opened: {portName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to open ScanOut serial port: {portName}");
                _errorService.ReportError("ScanOut", $"ScanOut serial port {portName} failed to open", ex);
                // Do not re-throw to prevent application crash. The port remains closed.
            }
        }

        public void Close()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                _logger.LogInformation("ScanOut serial port closed");
            }
        }

        public Task<ScanOutResult> PerformScanOutAsync(string pid, int slot)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    _logger.LogWarning("Cannot perform scan-out: Serial port not open");
                    return Task.FromResult(ScanOutResult.NG);
                }

                string dataToSend = $"{pid}|{slot}\r";
                _serialPort.WriteLine(dataToSend);
                _logger.LogInformation($"ScanOut sent: {pid}, Slot: {slot}");
                
                return Task.FromResult(ScanOutResult.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending scan-out trigger");
                return Task.FromResult(ScanOutResult.NG);
            }
        }

        public void Dispose()
        {
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen) _serialPort.Close();
                _serialPort.Dispose();
            }
        }
    }
}
