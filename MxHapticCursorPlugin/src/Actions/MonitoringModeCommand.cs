namespace Loupedeck.MxHapticCursorPlugin.Actions
{
    using System;
    using global::MxHapticCursorPlugin.Settings;

    public class MonitoringModeCommand : PluginMultistateDynamicCommand
    {
        private readonly MxHapticCursorPlugin _plugin;

        public MonitoringModeCommand()
        {
            this.DisplayName = "Monitoring Mode";
            this.Description = "Switch between polling and event-driven monitoring";
            this.GroupName = "Settings";

            _plugin = (MxHapticCursorPlugin)base.Plugin;

            this.AddState("Polling", "Polling Mode (Simple, Constant CPU)");
            this.AddState("EventDriven", "Event-Driven Mode (Efficient, Complex)");
        }

        protected override void RunCommand(string actionParameter)
        {
            if (_plugin == null)
            {
                return;
            }

            var mode = actionParameter == "Polling"
                ? MonitoringMode.Polling
                : MonitoringMode.EventDriven;

            _plugin.UpdateMonitoringMode(mode);
            this.ActionImageChanged();
        }
    }
}
