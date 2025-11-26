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

        protected override void RunCommand(string actionParameter)
        {
            if (this.HapticPlugin == null)
            {
                return;
            }

            bool currentState = this.HapticPlugin.IsEnabled;
            this.HapticPlugin.SetEnabled(!currentState);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            bool isEnabled = this.HapticPlugin?.IsEnabled ?? true;
            var color = isEnabled ? BitmapColor.Green : BitmapColor.Red;
            using var builder = new BitmapBuilder(imageSize);
            builder.Clear(color);
            builder.DrawText(isEnabled ? "ON" : "OFF");
            return builder.ToImage();
        }
    }
}
