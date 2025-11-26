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

        protected override void RunCommand(string actionParameter)
        {
            if (this.HapticPlugin == null) return;

            var currentMode = this.HapticPlugin.CurrentMonitoringMode;
            var nextMode = currentMode == MonitoringMode.Polling
                ? MonitoringMode.EventDriven
                : MonitoringMode.Polling;

            PluginLog.Info($"Monitor: {currentMode} -> {nextMode}");
            this.HapticPlugin.UpdateMonitoringMode(nextMode);
            this.ActionImageChanged();
        }

        protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
        {
            var mode = this.HapticPlugin?.CurrentMonitoringMode ?? MonitoringMode.Polling;
            return mode == MonitoringMode.Polling ? "Poll" : "Event";
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            var mode = this.HapticPlugin?.CurrentMonitoringMode ?? MonitoringMode.Polling;
            using var builder = new BitmapBuilder(imageSize);
            builder.Clear(BitmapColor.Black);
            builder.DrawText(mode == MonitoringMode.Polling ? "Poll" : "Event");
            return builder.ToImage();
        }
    }
}
