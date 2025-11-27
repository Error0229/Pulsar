namespace Loupedeck.Pulsar.Actions
{
    using System;

    using global::Pulsar.Settings;

    public class PresetSelectorCommand : PluginDynamicCommand
    {
        private PulsarPlugin HapticPlugin => this.Plugin as PulsarPlugin;

        private static readonly SensitivityPreset[] Presets =
        {
            SensitivityPreset.Low,
            SensitivityPreset.Medium,
            SensitivityPreset.High
        };

        public PresetSelectorCommand()
            : base(displayName: "Sensitivity",
                   description: "Cycle through haptic sensitivity levels",
                   groupName: "Haptic Settings")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.HapticPlugin == null)
            {
                return;
            }

            var currentPreset = this.HapticPlugin.CurrentPreset;
            var currentIndex = Array.IndexOf(Presets, currentPreset);
            if (currentIndex < 0)
            {
                currentIndex = 1;
            }

            var nextIndex = (currentIndex + 1) % Presets.Length;
            var nextPreset = Presets[nextIndex];

            PluginLog.Info($"Preset: {currentPreset} -> {nextPreset}");
            this.HapticPlugin.UpdatePreset(nextPreset);
            this.ActionImageChanged();
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            var preset = this.HapticPlugin?.CurrentPreset ?? SensitivityPreset.Medium;
            return preset.ToString();
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var preset = this.HapticPlugin?.CurrentPreset ?? SensitivityPreset.Medium;
            using var builder = new BitmapBuilder(imageSize);
            builder.Clear(BitmapColor.Black);
            builder.DrawText(preset.ToString());
            return builder.ToImage();
        }
    }
}