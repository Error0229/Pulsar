namespace Loupedeck.MxHapticCursorPlugin.Actions
{
    using System;

    public class EnableToggleCommand : PluginDynamicCommand
    {
        private readonly MxHapticCursorPlugin _plugin;
        private bool _isEnabled = true;

        public EnableToggleCommand()
        {
            this.DisplayName = "Enable/Disable Haptics";
            this.Description = "Toggle cursor haptic feedback on/off";
            this.GroupName = "Settings";

            _plugin = (MxHapticCursorPlugin)base.Plugin;
        }

        protected override void RunCommand(string actionParameter)
        {
            if (_plugin == null)
            {
                return;
            }

            _isEnabled = !_isEnabled;
            _plugin.SetEnabled(_isEnabled);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            var color = _isEnabled ? BitmapColor.Green : BitmapColor.Red;
            using var builder = new BitmapBuilder(imageSize);
            builder.Clear(color);
            builder.DrawText(_isEnabled ? "ON" : "OFF");
            return builder.ToImage();
        }
    }
}
