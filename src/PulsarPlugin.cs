namespace Loupedeck.Pulsar
{
    using System;
    using global::Pulsar.Monitoring;
    using global::Pulsar.Haptics;
    using global::Pulsar.Settings;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class PulsarPlugin : Plugin
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
        public PulsarPlugin()
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

            // Create haptic controller with debug logging
            _hapticController = new HapticController(_settings, TriggerHapticEvent, msg => this.Log.Info(msg));

            // Create cursor monitor based on settings
            _cursorMonitor = _settings.MonitoringMode == MonitoringMode.Polling
                ? new PollingCursorMonitor(pollIntervalMs: 50)
                : new EventDrivenCursorMonitor();

            // Wire up cursor change events
            _cursorMonitor.CursorChanged += _hapticController.OnCursorChanged;

            // Start monitoring
            _cursorMonitor.Start();

            this.Info.DisplayName = "Pulsar";
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

        public SensitivityPreset CurrentPreset => _settings?.Preset ?? SensitivityPreset.Medium;
        public MonitoringMode CurrentMonitoringMode => _settings?.MonitoringMode ?? MonitoringMode.Polling;
        public bool IsEnabled => _settings?.Enabled ?? true;

        public void UpdatePreset(SensitivityPreset preset)
        {
            var wasEnabled = _settings?.Enabled ?? true;
            var currentMode = _settings?.MonitoringMode ?? MonitoringMode.Polling;

            _settings = HapticSettings.CreatePreset(preset);
            _settings.Enabled = wasEnabled;
            _settings.MonitoringMode = currentMode;
            SaveSettingsToStorage();

            // Recreate controller and monitor with new settings
            RecreateControllerAndMonitor();

            this.Log.Info($"Preset changed to: {preset}");
        }

        public void UpdateMonitoringMode(MonitoringMode mode)
        {
            _settings.MonitoringMode = mode;
            SaveSettingsToStorage();

            // Recreate controller and monitor with new mode
            RecreateControllerAndMonitor();

            this.Log.Info($"Monitoring mode changed to: {mode}");
        }

        public void SetEnabled(bool enabled)
        {
            _settings.Enabled = enabled;
            SaveSettingsToStorage();

            this.Log.Info($"Haptics enabled: {enabled}");
        }

        private void RecreateControllerAndMonitor()
        {
            // Stop old monitor
            _cursorMonitor?.Stop();
            _cursorMonitor?.Dispose();

            // Recreate haptic controller with new settings
            _hapticController = new HapticController(_settings, TriggerHapticEvent, msg => this.Log.Info(msg));

            // Create new monitor
            _cursorMonitor = _settings.MonitoringMode == MonitoringMode.Polling
                ? new PollingCursorMonitor(pollIntervalMs: 50)
                : new EventDrivenCursorMonitor();

            _cursorMonitor.CursorChanged += _hapticController.OnCursorChanged;
            _cursorMonitor.Start();
        }
    }
}
