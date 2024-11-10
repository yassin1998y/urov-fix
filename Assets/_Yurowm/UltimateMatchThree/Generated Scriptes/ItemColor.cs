// Auto Generated script. Use "Berry Panel > Item Colors" to edit it.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ItemColor {
    Red = 0,
	Green = 1,
	Blue = 2,
	Yellow = 3,
	Purple = 4,
	Orange = 5,
    Unknown = 100,
    Uncolored = 101,
    Universal = 102
}

public static class RealColors {

    static Dictionary<ItemColor, Color> colors = new Dictionary<ItemColor, Color>() {
        {ItemColor.Red, new Color(0.85f, 0.11f, 0.00f, 1.00f)},
		{ItemColor.Green, new Color(0.59f, 0.90f, 0.09f, 1.00f)},
		{ItemColor.Blue, new Color(0.04f, 0.41f, 0.99f, 1.00f)},
		{ItemColor.Yellow, new Color(1.00f, 0.84f, 0.00f, 1.00f)},
		{ItemColor.Purple, new Color(0.95f, 0.09f, 0.98f, 1.00f)},
		{ItemColor.Orange, new Color(1.00f, 0.39f, 0.00f, 1.00f)}
    };

    public static Color Get(ItemColor color) {
        try {
            if (color.IsPhysicalColor())
                return colors[color];
        } catch (System.Exception) {
        }
        return Color.white;
    }
}