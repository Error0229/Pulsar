namespace Loupedeck.Pulsar.Actions
{
    using System;

    using global::Pulsar.Settings;

    public class MonitoringModeCommand : PluginDynamicCommand
    {
        private PulsarPlugin HapticPlugin => this.Plugin as PulsarPlugin;

        public MonitoringModeCommand()
            : base(displayName: "Monitor Mode",
                   description: "Toggle between Polling and Event-Driven monitoring",
                   groupName: "Haptic Settings")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.HapticPlugin == null)
            {
                return;
            }

            var currentMode = this.HapticPlugin.CurrentMonitoringMode;
            var nextMode = currentMode == MonitoringMode.Polling
                ? MonitoringMode.EventDriven
                : MonitoringMode.Polling;

            PluginLog.Info($"Monitor: {currentMode} -> {nextMode}");
            this.HapticPlugin.UpdateMonitoringMode(nextMode);
            this.ActionImageChanged();
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            var mode = this.HapticPlugin?.CurrentMonitoringMode ?? MonitoringMode.Polling;
            return mode == MonitoringMode.Polling ? "Poll" : "Event";
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var mode = this.HapticPlugin?.CurrentMonitoringMode ?? MonitoringMode.Polling;
            using var builder = new BitmapBuilder(imageSize);
            builder.Clear(BitmapColor.Black);
            builder.DrawText(mode == MonitoringMode.Polling ? "Poll" : "Event");
            return builder.ToImage();
        }
    }
}