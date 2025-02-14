namespace XREngine.Networking
{
    internal class OutputLogListener(VirtualizedConsoleUIComponent outputLogComp) : System.Diagnostics.TraceListener
    {
        public VirtualizedConsoleUIComponent OutputLogComp { get; } = outputLogComp;

        public override void Write(string? message)
        {
            var newLine = Environment.NewLine;
            if (message is not null && message.Contains(newLine))
            {
                var lines = message.Split(newLine);
                OutputLogComp.AddToLastItem(lines[0]);
                for (int i = 1; i < lines.Length; i++)
                    OutputLogComp.AddItem(lines[i]);
            }
            else
                OutputLogComp.AddToLastItem(message);
        }

        public override void WriteLine(string? message)
        {
            OutputLogComp.AddItem(message);
        }
    }
}