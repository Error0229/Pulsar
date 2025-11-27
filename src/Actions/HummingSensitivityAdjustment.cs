namespace Loupedeck.Pulsar.Actions;

using System;

public class HummingSensitivityAdjustment : PluginDynamicAdjustment
{
    private PulsarPlugin HapticPlugin => this.Plugin as PulsarPlugin;

    public HummingSensitivityAdjustment()
        : base(displayName: "Sensitivity",
               description: "Adjust audio sensitivity multiplier (0.1x - 5.0x)",
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

        // Each tick = 0.1x change
        var delta = ticks * 0.1f;
        var newValue = this.HapticPlugin.HummingSensitivity + delta;
        this.HapticPlugin.SetHummingSensitivity(newValue);
        this.AdjustmentValueChanged();
    }

    protected override void RunCommand(String actionParameter)
    {
        // Reset to default (1.0x)
        this.HapticPlugin?.SetHummingSensitivity(1.0f);
        this.AdjustmentValueChanged();
    }

    protected override String GetAdjustmentValue(String actionParameter)
    {
        var sensitivity = this.HapticPlugin?.HummingSensitivity ?? 1.0f;
        return $"{sensitivity:F1}x";
    }
}
