using System.IO.Ports;

namespace DrumDuino.Core.Serial;

public static class SerialPortHelper
{
    public static IEnumerable<string> GetOrderedPortNames() =>
        SerialPort.GetPortNames()
            .OrderBy(GetPortNumber)
            .ThenBy(p => p, StringComparer.OrdinalIgnoreCase);

    public static int GetPortNumber(string portName)
    {
        var name = NormalizeDisplayName(portName);
        if (name.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(name.AsSpan(3), out var number))
        {
            return number;
        }

        return int.MaxValue;
    }

    public static string NormalizeDisplayName(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            return portName;
        }

        return portName.StartsWith(@"\\.\", StringComparison.OrdinalIgnoreCase)
            ? portName[4..]
            : portName;
    }

    public static SerialPort OpenPort(string portName, int baudRate)
    {
        var displayName = NormalizeDisplayName(portName);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new IOException("Nenhuma porta COM selecionada.");
        }

        var available = SerialPort.GetPortNames();
        if (!available.Contains(displayName, StringComparer.OrdinalIgnoreCase))
        {
            throw new IOException(
                $"Porta {displayName} não encontrada. Clique em ↻ para atualizar a lista de portas COM.");
        }

        var port = CreatePort(displayName, baudRate);
        try
        {
            port.Open();
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
            return port;
        }
        catch (Exception ex)
        {
            SafeDispose(port);
            throw new IOException(DescribeOpenFailure(displayName, ex), ex);
        }
    }

    public static string DescribeOpenFailure(string portName, Exception ex)
    {
        var displayName = NormalizeDisplayName(portName);
        var root = ex.GetBaseException();

        if (root is UnauthorizedAccessException
            || (root is IOException io && io.Message.Contains("denied", StringComparison.OrdinalIgnoreCase)))
        {
            return $"Porta {displayName} em uso ou bloqueada. Feche o Monitor Serial / Arduino IDE, " +
                   "outra instância do DrumDuino, ou desconecte e reconecte o cabo USB.";
        }

        if (root is ArgumentException)
        {
            return $"Porta {displayName} inválida. Atualize a lista de portas COM e tente novamente.";
        }

        return $"Falha ao abrir {displayName}: {root.Message}";
    }

    private static SerialPort CreatePort(string portName, int baudRate) =>
        new(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 500,
            WriteTimeout = 500,
            DtrEnable = true,
            RtsEnable = true
        };

    private static void SafeDispose(SerialPort port)
    {
        try
        {
            if (port.IsOpen)
            {
                port.Close();
            }
        }
        catch
        {
            // Ignore cleanup errors.
        }

        port.Dispose();
    }
}
