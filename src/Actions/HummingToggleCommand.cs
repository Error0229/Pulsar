namespace Loupedeck.Pulsar.Actions
{
    using System;

    public class HummingToggleCommand : PluginDynamicCommand
    {
        private PulsarPlugin HapticPlugin => this.Plugin as PulsarPlugin;

        public HummingToggleCommand()
            : base(displayName: "Enable/Disable Humming",
                   description: "Toggle audio-reactive haptic feedback on/off",
                   groupName: "Humming")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.HapticPlugin == null)
            {
                return;
            }

            var currentState = this.HapticPlugin.IsHummingEnabled;
            this.HapticPlugin.SetHummingEnabled(!currentState);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var isEnabled = this.HapticPlugin?.IsHummingEnabled ?? false;
            var isAubioActive = this.HapticPlugin?.IsHummingAubioActive ?? false;

            var color = isEnabled
                ? (isAubioActive ? new BitmapColor(128, 0, 255) : BitmapColor.Blue) // Purple if aubio, blue otherwise
                : new BitmapColor(128, 128, 128);

            using var builder = new BitmapBuilder(imageSize);
            builder.Clear(color);

            if (isEnabled && isAubioActive)
            {
                var bpm = this.HapticPlugin?.HummingBpm ?? 0;
                builder.DrawText(bpm > 0 ? $"{bpm:F0}\nBPM" : "HUM");
            }
            else
            {
                builder.DrawText(isEnabled ? "HUM" : "OFF");
            }

            return builder.ToImage();
        }
    }
}