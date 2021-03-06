using Microsoft.Extensions.Logging;

namespace MiniCover.IO
{
    public interface IOutput
    {
        LogLevel MinimumLevel { get; set; }

        int Identation { get; set; }

        void WriteLine(string message, LogLevel level);
    }
}