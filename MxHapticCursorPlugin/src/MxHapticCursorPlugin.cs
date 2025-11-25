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

            // Load settings (default to Medium preset for now)
            _settings = HapticSettings.CreatePreset(SensitivityPreset.Medium);

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
    }
}
