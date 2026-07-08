public sealed class ColorSwitchRuleDefinition : ResourceRuleDefinition
{
    public readonly ColorId[] ColorList;

    public ColorSwitchRuleDefinition(ColorId[] colorList)
    {
        ColorList = colorList ?? new ColorId[0];
    }
}
