using System.Diagnostics;

namespace XREngine
{
    public class TraceListener : ConsoleTraceListener
    {
        private bool _isActive;
        private readonly object _lock = new();

        //TODO: output to session file using stream
        public override void WriteLine(string? message)
            => Write(message + Environment.NewLine);
        public override void Write(string? message)
        {
            //Avoid possibility of stack overflow
            //Use lock to prevent multiple threads from writing to the same file
            lock (_lock)
            {
                if (_isActive)
                    return;

                try
                {
                    _isActive = true;
                    base.Write(message);
                    //TODO: output to session file using stream
                }
                finally
                {
                    _isActive = false;
                }
            }
        }
    }
}
