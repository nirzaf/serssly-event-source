using System;
using System.Diagnostics.Tracing;

namespace serssly
{
    [EventSource(Name = "Serssly.Diagnostics")]
    internal sealed class SersslyEventSource : EventSource
    {
        private static SersslyEventSource? s_instance;

        private PollingCounter? gen0GCCounter;
        private PollingCounter? gen1GCCounter;
        private PollingCounter? gen2GCCounter;

        public static void Initialize()
        {
            s_instance = new SersslyEventSource();
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                gen0GCCounter ??= new PollingCounter("gen-0-gc-total", this, () => GC.CollectionCount(0)) { DisplayName = "Gen 0 GC Total" };
                gen1GCCounter ??= new PollingCounter("gen-1-gc-total", this, () => GC.CollectionCount(1)) { DisplayName = "Gen 1 GC Total" };
                gen2GCCounter ??= new PollingCounter("gen-2-gc-total", this, () => GC.CollectionCount(2)) { DisplayName = "Gen 2 GC Total" };
            }
        }
    }
}
