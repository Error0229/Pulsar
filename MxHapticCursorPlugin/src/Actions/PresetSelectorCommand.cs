namespace Loupedeck.MxHapticCursorPlugin.Actions
{
    using System;
    using global::MxHapticCursorPlugin.Settings;

    public class PresetSelectorCommand : PluginMultistateDynamicCommand
    {
        private readonly MxHapticCursorPlugin _plugin;

        public PresetSelectorCommand()
        {
            this.DisplayName = "Sensitivity Preset";
            this.Description = "Select haptic sensitivity level";
            this.GroupName = "Settings";

            _plugin = (MxHapticCursorPlugin)base.Plugin;

            // Add states for each preset
            this.AddState("Low", "Low Sensitivity");
            this.AddState("Medium", "Medium Sensitivity");
            this.AddState("High", "High Sensitivity");
        }

        protected override void RunCommand(string actionParameter)
        {
            if (_plugin == null)
            {
                return;
            }

            var preset = actionParameter switch
            {
                "Low" => SensitivityPreset.Low,
                "Medium" => SensitivityPreset.Medium,
                "High" => SensitivityPreset.High,
                _ => SensitivityPreset.Medium
            };

            _plugin.UpdatePreset(preset);
            this.ActionImageChanged();
        }
    }
}
