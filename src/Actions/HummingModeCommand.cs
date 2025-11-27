namespace Loupedeck.Pulsar.Actions
{
    using System;

    using global::Pulsar.Settings;

    public class HummingModeCommand : PluginDynamicCommand
    {
        private PulsarPlugin HapticPlugin => this.Plugin as PulsarPlugin;

        public HummingModeCommand()
            : base(displayName: "Humming Mode",
                   description: "Cycle through audio analysis modes (Bass, MultiBand, Beat, Amplitude)",
                   groupName: "Humming")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.HapticPlugin == null)
            {
                return;
            }

            var currentMode = this.HapticPlugin.HummingMode;
            var modes = (AnalysisMode[])Enum.GetValues(typeof(AnalysisMode));
            var currentIndex = Array.IndexOf(modes, currentMode);
            var nextIndex = (currentIndex + 1) % modes.Length;

            this.HapticPlugin.SetHummingMode(modes[nextIndex]);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var mode = this.HapticPlugin?.HummingMode ?? AnalysisMode.Bass;
            var isEnabled = this.HapticPlugin?.IsHummingEnabled ?? false;

            var color = isEnabled ? GetModeColor(mode) : new BitmapColor(128, 128, 128);

            using var builder = new BitmapBuilder(imageSize);
            builder.Clear(color);
            builder.DrawText(GetModeShortName(mode));
            return builder.ToImage();
        }

        private static BitmapColor GetModeColor(AnalysisMode mode)
        {
            return mode switch
            {
                AnalysisMode.Bass => new BitmapColor(255, 64, 0),      // Orange-red for bass
                AnalysisMode.MultiBand => new BitmapColor(0, 128, 255), // Blue for multi
                AnalysisMode.BeatDetection => new BitmapColor(255, 0, 128), // Pink for beats
                AnalysisMode.Amplitude => new BitmapColor(0, 255, 128),  // Teal for amplitude
                _ => BitmapColor.White
            };
        }

        private static String GetModeShortName(AnalysisMode mode)
        {
            return mode switch
            {
                AnalysisMode.Bass => "BASS",
                AnalysisMode.MultiBand => "MULTI",
                AnalysisMode.BeatDetection => "BEAT",
                AnalysisMode.Amplitude => "AMP",
                _ => mode.ToString()
            };
        }
    }
}