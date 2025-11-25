namespace Loupedeck.MxHapticCursorPlugin
{
    using System;
    using global::MxHapticCursorPlugin.Monitoring;
    using global::MxHapticCursorPlugin.Haptics;
    using global::MxHapticCursorPlugin.Settings;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class MxHapticCursorPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Settings storage keys
        private const string SettingPreset = "sensitivity_preset";
        private const string SettingMonitoringMode = "monitoring_mode";
        private const string SettingEnabled = "enabled";

        private ICursorMonitor _cursorMonitor;
        private HapticController _hapticController;
        private HapticSettings _settings;

        // Initializes a new instance of the plugin class.
        public MxHapticCursorPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            // Register all haptic events
            this.PluginEvents.AddEvent("sharp_collision", "Sharp Collision", "Resize handle collisions");
            this.PluginEvents.AddEvent("subtle_collision", "Subtle Collision", "Soft transitions");
            this.PluginEvents.AddEvent("sharp_state_change", "Sharp State Change", "Clickable elements");
            this.PluginEvents.AddEvent("damp_state_change", "Damp State Change", "Return to default");
            this.PluginEvents.AddEvent("ringing", "Ringing", "Ongoing processes");
            this.PluginEvents.AddEvent("knock", "Knock", "Notifications");
            this.PluginEvents.AddEvent("mad", "Mad", "Blocked actions");

            // Load settings from storage
            LoadSettingsFromStorage();

            // Create haptic controller
            _hapticController = new HapticController(_settings, TriggerHapticEvent);

            // Create cursor monitor based on settings
            _cursorMonitor = _settings.MonitoringMode == MonitoringMode.Polling
                ? new PollingCursorMonitor(pollIntervalMs: 50)
                : new EventDrivenCursorMonitor();

            // Wire up cursor change events
            _cursorMonitor.CursorChanged += _hapticController.OnCursorChanged;

            // Start monitoring
            _cursorMonitor.Start();

            this.Info.DisplayName = "MX Haptic Cursor Feedback";
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
            _cursorMonitor?.Stop();
            _cursorMonitor?.Dispose();
            _cursorMonitor = null;
            _hapticController = null;
        }

        private void TriggerHapticEvent(string eventName)
        {
            try
            {
                this.PluginEvents.RaiseEvent(eventName);
            }
            catch (Exception ex)
            {
                this.Log.Error($"Failed to trigger haptic event '{eventName}': {ex.Message}");
            }
        }

        public override void RunCommand(string commandName, string parameter)
        {
            // Record device activity when any command is triggered
            _hapticController?.RecordDeviceActivity();
        }

        private void LoadSettingsFromStorage()
        {
            // Load from plugin settings storage
            var presetValue = 1; // Default Medium
            if (this.TryGetPluginSetting(SettingPreset, out var presetStr) && int.TryParse(presetStr, out var p))
            {
                presetValue = p;
            }

            var modeValue = 0; // Default Polling
            if (this.TryGetPluginSetting(SettingMonitoringMode, out var modeStr) && int.TryParse(modeStr, out var m))
            {
                modeValue = m;
            }

            var enabled = true; // Default enabled
            if (this.TryGetPluginSetting(SettingEnabled, out var enabledStr) && bool.TryParse(enabledStr, out var e))
            {
                enabled = e;
            }

            var preset = (SensitivityPreset)presetValue;
            _settings = HapticSettings.CreatePreset(preset);
            _settings.MonitoringMode = (MonitoringMode)modeValue;
            _settings.Enabled = enabled;
        }

        private void SaveSettingsToStorage()
        {
            this.SetPluginSetting(SettingPreset, ((int)_settings.Preset).ToString(), false);
            this.SetPluginSetting(SettingMonitoringMode, ((int)_settings.MonitoringMode).ToString(), false);
            this.SetPluginSetting(SettingEnabled, _settings.Enabled.ToString(), false);
        }

        public void UpdatePreset(SensitivityPreset preset)
        {
            _settings = HapticSettings.CreatePreset(preset);
            SaveSettingsToStorage();

            // Restart monitor with new settings
            RestartMonitor();
        }

        public void UpdateMonitoringMode(MonitoringMode mode)
        {
            _settings.MonitoringMode = mode;
            SaveSettingsToStorage();

            // Restart monitor with new mode
            RestartMonitor();
        }

        public bool IsEnabled => _settings?.Enabled ?? true;

        public void SetEnabled(bool enabled)
        {
            _settings.Enabled = enabled;
            SaveSettingsToStorage();
        }

        private void RestartMonitor()
        {
            _cursorMonitor?.Stop();
            _cursorMonitor?.Dispose();

            _cursorMonitor = _settings.MonitoringMode == MonitoringMode.Polling
                ? new PollingCursorMonitor(pollIntervalMs: 50)
                : new EventDrivenCursorMonitor();

            _cursorMonitor.CursorChanged += _hapticController.OnCursorChanged;
            _cursorMonitor.Start();
        }
    }
}
