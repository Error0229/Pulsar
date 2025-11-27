namespace Loupedeck.Pulsar.Actions;

using System;

public class HummingThresholdAdjustment : PluginDynamicAdjustment
{
    private PulsarPlugin HapticPlugin => this.Plugin as PulsarPlugin;

    public HummingThresholdAdjustment()
        : base(displayName: "Threshold",
               description: "Adjust minimum threshold to trigger haptics (0-100%)",
               groupName: "Humming",
               hasReset: true)
    {
    }

    protected override void ApplyAdjustment(String actionParameter, Int32 ticks)
    {
        if (this.HapticPlugin == null)
        {
            return;
        }

        // Each tick = 5% change
        var delta = ticks * 0.05f;
        var newValue = this.HapticPlugin.HummingThreshold + delta;
        this.HapticPlugin.SetHummingThreshold(newValue);
        this.AdjustmentValueChanged();
    }

    protected override void RunCommand(String actionParameter)
    {
        // Reset to default (0.1 = 10%)
        this.HapticPlugin?.SetHummingThreshold(0.1f);
        this.AdjustmentValueChanged();
    }

    protected override String GetAdjustmentValue(String actionParameter)
    {
        var threshold = this.HapticPlugin?.HummingThreshold ?? 0.1f;
        return $"{threshold * 100:F0}%";
    }
}
