using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public static class ItemColorUtils {

    public static readonly List<ItemColor> physiscalColors;
    public static readonly List<ItemColor> allColors;

    static ItemColorUtils() {
        allColors = Enum.GetValues(typeof(ItemColor)).Cast<ItemColor>().ToList();
        physiscalColors = allColors.Where(x => x.IsPhysicalColor()).ToList();
    }

    public static bool IsMatchWith(this ItemColor colorA, ItemColor colorB) {
        if (!colorA.IsMatchableColor() || !colorB.IsMatchableColor())
            return false;
        if (colorA == colorB)
            return true;
        if (colorA.IsUniversalColor() || colorB.IsUniversalColor())
            return true;
        return false;
    }

    public static bool IsPhysicalColor(this ItemColor color) {
        return (int) color >= 0 && (int) color < 100;
    }

    public static bool IsMatchableColor(this ItemColor color) {
        return color.IsPhysicalColor() || color.IsUniversalColor();
    }

    public static bool IsUniversalColor(this ItemColor color) {
        return color == ItemColor.Universal;
    }

    public static bool IsUnknown(this ItemColor color) {
        return color == ItemColor.Unknown;
    }

    public static bool IsColored(this ItemColor color) {
        return color.IsUnknown() || color.IsPhysicalColor();
    }
}