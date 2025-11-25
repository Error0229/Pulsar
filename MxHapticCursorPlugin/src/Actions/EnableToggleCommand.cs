namespace Loupedeck.MxHapticCursorPlugin.Actions
{
    using System;

    public class EnableToggleCommand : PluginDynamicCommand
    {
        private readonly MxHapticCursorPlugin _plugin;

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

            bool currentState = _plugin.IsEnabled;
            _plugin.SetEnabled(!currentState);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            bool isEnabled = _plugin?.IsEnabled ?? true;
            var color = isEnabled ? BitmapColor.Green : BitmapColor.Red;
            using var builder = new BitmapBuilder(imageSize);
            builder.Clear(color);
            builder.DrawText(isEnabled ? "ON" : "OFF");
            return builder.ToImage();
        }
    }
}
