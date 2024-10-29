namespace XREngine.Networking
{
    internal class OutputLogListener(VirtualizedListUIComponent outputLogComp) : System.Diagnostics.TraceListener
    {
        public VirtualizedListUIComponent OutputLogComp { get; } = outputLogComp;

        public override void Write(string? message)
        {
            OutputLogComp.AddToLastItem(message);
        }

        public override void WriteLine(string? message)
        {
            OutputLogComp.AddItem(message);
        }
    }
}