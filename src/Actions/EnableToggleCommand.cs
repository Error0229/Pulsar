namespace Loupedeck.Pulsar.Actions
{
    using System;

    public class EnableToggleCommand : PluginDynamicCommand
    {
        private PulsarPlugin HapticPlugin => this.Plugin as PulsarPlugin;

        public EnableToggleCommand()
            : base(displayName: "Enable/Disable Haptics",
                   description: "Toggle cursor haptic feedback on/off",
                   groupName: "Settings")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.HapticPlugin == null)
            {
                return;
            }

            var currentState = this.HapticPlugin.IsEnabled;
            this.HapticPlugin.SetEnabled(!currentState);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var isEnabled = this.HapticPlugin?.IsEnabled ?? true;
            var color = isEnabled ? BitmapColor.Green : BitmapColor.Red;
            using var builder = new BitmapBuilder(imageSize);
            builder.Clear(color);
            builder.DrawText(isEnabled ? "ON" : "OFF");
            return builder.ToImage();
        }
    }
}