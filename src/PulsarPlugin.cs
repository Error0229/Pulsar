namespace Loupedeck.Pulsar
{
    using System;

    using global::Pulsar.Audio;
    using global::Pulsar.Haptics;
    using global::Pulsar.Monitoring;
    using global::Pulsar.Settings;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class PulsarPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Settings storage keys
        private const String SettingPreset = "sensitivity_preset";
        private const String SettingMonitoringMode = "monitoring_mode";
        private const String SettingEnabled = "enabled";
        private const String SettingHummingEnabled = "humming_enabled";
        private const String SettingHummingMode = "humming_mode";
        private const String SettingHummingThreshold = "humming_threshold";
        private const String SettingHummingSensitivity = "humming_sensitivity";

        private ICursorMonitor _cursorMonitor;
        private HapticController _hapticController;
        private HapticSettings _settings;
        private HummingController _hummingController;

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
            this.PluginEvents.AddEvent("click", "Click", "Light feedback");

            // Load settings from storage
            this.LoadSettingsFromStorage();

            // Create haptic controller with debug logging
            this._hapticController = new HapticController(this._settings, this.TriggerHapticEvent, msg => this.Log.Info(msg));

            // Create cursor monitor based on settings
            this._cursorMonitor = this._settings.MonitoringMode == MonitoringMode.Polling
                ? new PollingCursorMonitor(pollIntervalMs: 50)
                : new EventDrivenCursorMonitor();

            // Wire up cursor change events
            this._cursorMonitor.CursorChanged += this._hapticController.OnCursorChanged;

            // Start monitoring
            this._cursorMonitor.Start();

            // Initialize Humming mode (if enabled)
            this._hummingController = new HummingController(this._settings.Humming, this.TriggerHapticEvent, msg => this.Log.Info(msg));
            if (this._settings.Humming.Enabled)
            {
                try
                {
                    this._hummingController.Start();
                    this.Log.Info("Humming mode started on plugin load");
                }
                catch (Exception ex)
                {
                    this.Log.Warning($"Failed to start Humming mode: {ex.Message}");
                }
            }

            this.Info.DisplayName = "Pulsar";
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
            this._cursorMonitor?.Stop();
            this._cursorMonitor?.Dispose();
            this._cursorMonitor = null;
            this._hapticController = null;
            this._hummingController?.Dispose();
            this._hummingController = null;
        }

        private void TriggerHapticEvent(String eventName)
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

        public override void RunCommand(String commandName, String parameter) =>
            // Record device activity when any command is triggered
            this._hapticController?.RecordDeviceActivity();

        private void LoadSettingsFromStorage()
        {
            // Load from plugin settings storage
            var presetValue = 1; // Default Medium
            if (this.TryGetPluginSetting(SettingPreset, out var presetStr) && Int32.TryParse(presetStr, out var p))
            {
                presetValue = p;
            }

            var modeValue = 0; // Default Polling
            if (this.TryGetPluginSetting(SettingMonitoringMode, out var modeStr) && Int32.TryParse(modeStr, out var m))
            {
                modeValue = m;
            }

            var enabled = true; // Default enabled
            if (this.TryGetPluginSetting(SettingEnabled, out var enabledStr) && Boolean.TryParse(enabledStr, out var e))
            {
                enabled = e;
            }

            // Humming settings
            var hummingEnabled = false;
            if (this.TryGetPluginSetting(SettingHummingEnabled, out var hummingEnabledStr) && Boolean.TryParse(hummingEnabledStr, out var he))
            {
                hummingEnabled = he;
            }

            var hummingModeValue = 2; // Default BeatDetection
            if (this.TryGetPluginSetting(SettingHummingMode, out var hummingModeStr) && Int32.TryParse(hummingModeStr, out var hm))
            {
                hummingModeValue = hm;
            }

            var hummingThreshold = 0.1f;
            if (this.TryGetPluginSetting(SettingHummingThreshold, out var thresholdStr) && Single.TryParse(thresholdStr, out var t))
            {
                hummingThreshold = t;
            }

            var hummingSensitivity = 1.0f;
            if (this.TryGetPluginSetting(SettingHummingSensitivity, out var sensitivityStr) && Single.TryParse(sensitivityStr, out var s))
            {
                hummingSensitivity = s;
            }

            var preset = (SensitivityPreset)presetValue;
            this._settings = HapticSettings.CreatePreset(preset);
            this._settings.MonitoringMode = (MonitoringMode)modeValue;
            this._settings.Enabled = enabled;
            this._settings.Humming.Enabled = hummingEnabled;
            this._settings.Humming.AnalysisMode = (AnalysisMode)hummingModeValue;
            this._settings.Humming.Threshold = hummingThreshold;
            this._settings.Humming.Sensitivity = hummingSensitivity;
        }

        private void SaveSettingsToStorage()
        {
            this.SetPluginSetting(SettingPreset, ((Int32)this._settings.Preset).ToString(), false);
            this.SetPluginSetting(SettingMonitoringMode, ((Int32)this._settings.MonitoringMode).ToString(), false);
            this.SetPluginSetting(SettingEnabled, this._settings.Enabled.ToString(), false);
            this.SetPluginSetting(SettingHummingEnabled, this._settings.Humming.Enabled.ToString(), false);
            this.SetPluginSetting(SettingHummingMode, ((Int32)this._settings.Humming.AnalysisMode).ToString(), false);
            this.SetPluginSetting(SettingHummingThreshold, this._settings.Humming.Threshold.ToString("F2"), false);
            this.SetPluginSetting(SettingHummingSensitivity, this._settings.Humming.Sensitivity.ToString("F2"), false);
        }

        public SensitivityPreset CurrentPreset => this._settings?.Preset ?? SensitivityPreset.Medium;
        public MonitoringMode CurrentMonitoringMode => this._settings?.MonitoringMode ?? MonitoringMode.Polling;
        public Boolean IsEnabled => this._settings?.Enabled ?? true;

        // Humming mode properties
        public Boolean IsHummingEnabled => this._settings?.Humming.Enabled ?? false;
        public AnalysisMode HummingMode => this._settings?.Humming.AnalysisMode ?? AnalysisMode.BeatDetection;
        public Boolean IsHummingAubioActive => this._hummingController?.IsAubioActive ?? false;
        public Single HummingBpm => this._hummingController?.CurrentBpm ?? 0;
        public Single HummingThreshold => this._settings?.Humming.Threshold ?? 0.1f;
        public Single HummingSensitivity => this._settings?.Humming.Sensitivity ?? 1.0f;

        public void UpdatePreset(SensitivityPreset preset)
        {
            var wasEnabled = this._settings?.Enabled ?? true;
            var currentMode = this._settings?.MonitoringMode ?? MonitoringMode.Polling;
            var hummingSettings = this._settings?.Humming ?? new HummingSettings();

            this._settings = HapticSettings.CreatePreset(preset);
            this._settings.Enabled = wasEnabled;
            this._settings.MonitoringMode = currentMode;
            this._settings.Humming = hummingSettings;
            this.SaveSettingsToStorage();

            // Recreate controller and monitor with new settings
            this.RecreateControllerAndMonitor();

            this.Log.Info($"Preset changed to: {preset}");
        }

        public void UpdateMonitoringMode(MonitoringMode mode)
        {
            this._settings.MonitoringMode = mode;
            this.SaveSettingsToStorage();

            // Recreate controller and monitor with new mode
            this.RecreateControllerAndMonitor();

            this.Log.Info($"Monitoring mode changed to: {mode}");
        }

        public void SetEnabled(Boolean enabled)
        {
            this._settings.Enabled = enabled;
            this.SaveSettingsToStorage();

            this.Log.Info($"Haptics enabled: {enabled}");
        }

        public void SetHummingEnabled(Boolean enabled)
        {
            this._settings.Humming.Enabled = enabled;
            this.SaveSettingsToStorage();

            if (enabled)
            {
                try
                {
                    this._hummingController?.Start();
                    this.Log.Info("Humming mode enabled");
                }
                catch (Exception ex)
                {
                    this.Log.Warning($"Failed to start Humming mode: {ex.Message}");
                    this._settings.Humming.Enabled = false;
                }
            }
            else
            {
                this._hummingController?.Stop();
                this.Log.Info("Humming mode disabled");
            }
        }

        public void SetHummingMode(AnalysisMode mode)
        {
            this._settings.Humming.AnalysisMode = mode;
            this.SaveSettingsToStorage();

            this._hummingController?.SetMode(mode);
            this.Log.Info($"Humming mode changed to: {mode}");
        }

        public void SetHummingThreshold(Single threshold)
        {
            this._settings.Humming.Threshold = Math.Clamp(threshold, 0f, 1f);
            this.SaveSettingsToStorage();
            this.Log.Info($"Humming threshold changed to: {this._settings.Humming.Threshold:F2}");
        }

        public void SetHummingSensitivity(Single sensitivity)
        {
            this._settings.Humming.Sensitivity = Math.Clamp(sensitivity, 0.1f, 5f);
            this.SaveSettingsToStorage();
            this.Log.Info($"Humming sensitivity changed to: {this._settings.Humming.Sensitivity:F2}");
        }

        private void RecreateControllerAndMonitor()
        {
            // Stop old monitor
            this._cursorMonitor?.Stop();
            this._cursorMonitor?.Dispose();

            // Recreate haptic controller with new settings
            this._hapticController = new HapticController(this._settings, this.TriggerHapticEvent, msg => this.Log.Info(msg));

            // Create new monitor
            this._cursorMonitor = this._settings.MonitoringMode == MonitoringMode.Polling
                ? new PollingCursorMonitor(pollIntervalMs: 50)
                : new EventDrivenCursorMonitor();

            this._cursorMonitor.CursorChanged += this._hapticController.OnCursorChanged;
            this._cursorMonitor.Start();
        }
    }
}