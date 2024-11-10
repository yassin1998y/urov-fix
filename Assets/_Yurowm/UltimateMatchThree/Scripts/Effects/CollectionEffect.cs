using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;
public class CollectionEffect : NodeEffect {

    public SpriteRenderer icon;

    public void SetItem(ISlotContent item, ItemColor color = ItemColor.Unknown) {
        Transform icon = item.transform.Find("Icon");
        if (!icon) return;
        if (color.IsPhysicalColor()) {
            SetSpriteColor coloredSprite = icon.GetComponent<SetSpriteColor>();
            if (coloredSprite) {
                SetIcon(coloredSprite.GetSprite(color));
                return;
            }
        }

        SpriteRenderer iconR = icon.gameObject.GetComponent<SpriteRenderer>();
        if (!iconR) return;
        SetIcon(iconR.sprite);
    }

    public void SetIcon(Sprite sprite) {
        Vector2? originalSize = GetSpriteSize(icon.sprite);
        Vector2? newSize = GetSpriteSize(sprite);
        icon.sprite = sprite;

        if (originalSize.HasValue && newSize.HasValue) {
            float scale = Mathf.Min(originalSize.Value.x / newSize.Value.x, originalSize.Value.y / newSize.Value.y);
            icon.transform.localScale = icon.transform.localScale * scale;
        }
    }

    Vector2? GetSpriteSize(Sprite s) {
        if (s == null) return null;
        Vector2 result = s.bounds.size;
        result.x *= s.rect.width / s.texture.width;
        result.y *= s.rect.height / s.texture.height;
        return result;
    }

    public void SetOrder(int v) {
        icon.sortingOrder += v;
    }
}
