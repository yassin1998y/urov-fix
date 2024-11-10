using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Reflection;
using Yurowm.GameCore;
using System.Collections.Generic;

public class SetSpriteColor : MonoBehaviour {

    public Target target = Target.Sprite;
    public Action action = Action.ChangeColor;

    public List<SpriteInfo> sprites = new List<SpriteInfo>();

    Component _component;
    Component component {
        get {
            if (!_component)
                _component = GetComponent();
            return _component;
        }
    }

    Color color {
        get {
            switch (target) {
                case Target.Sprite: return (component as SpriteRenderer).color;
                case Target.Image: return (component as Image).color;
                case Target.Text: return (component as Text).color;
                case Target.ParticleSystem: return (component as ParticleSystem).main.startColor.color;
                case Target.TrailRenderer: return (component as TrailRenderer).startColor;
            }
            return Color.white;
        }
        set {
            switch (target) {
                case Target.Sprite: (component as SpriteRenderer).color = value; break;
                case Target.Image: (component as Image).color = value; break;
                case Target.Text: (component as Text).color = value; break;
                case Target.TrailRenderer: {
                        (component as TrailRenderer).startColor = value;
                        (component as TrailRenderer).endColor = value;
                    } break;
                case Target.ParticleSystem: {
                        var module = (component as ParticleSystem).main;
                        module.startColor = value;
                    } break;
            }
        }
    }
    Sprite sprite {
        get {
            switch (target) {
                case Target.Sprite: return (component as SpriteRenderer).sprite;
                case Target.Image: return (component as Image).sprite;
            }
            return null;
        }
        set {
            switch (target) {
                case Target.Sprite: (component as SpriteRenderer).sprite = value; break;
                case Target.Image: (component as Image).sprite = value; break;
            }
        }
    }

    public void SetColor(ItemColor itemColor, bool changeAlpha = true) {
        switch (action) {
            case Action.ChangeColor: {
                    Color color = RealColors.Get(itemColor);
                    if (!changeAlpha) color.a = this.color.a;
                    this.color = color;
                    break;
                }

            case Action.ChangeSprite: {
                    SpriteInfo info = sprites.Find(x => x.color == itemColor);
                    if (info != null && info.sprite != null) {
                        sprite = info.sprite;
                    }
                    break;
                }
        }
    }
    public Sprite GetSprite(ItemColor color) {
        if (action == Action.ChangeSprite) {
            SpriteInfo info = sprites.Find(x => x.color == color);
            if (info != null && info.sprite != null)
                return info.sprite;
        }
        return null;
    }

    Component GetComponent() {
        switch (target) {
            case Target.Sprite: return GetComponent<SpriteRenderer>();
            case Target.Image: return GetComponent<Image>();
            case Target.Text: return GetComponent<Text>();
            case Target.ParticleSystem: return GetComponent<ParticleSystem>();
            case Target.TrailRenderer: return GetComponent<TrailRenderer>();
        }
        return null;
    }

    public enum Target {
        Sprite, Image, Text, ParticleSystem, TrailRenderer
    }

    public enum Action {
        ChangeColor, ChangeSprite
    }

    [System.Serializable]
    public class SpriteInfo {
        public ItemColor color;
        public Sprite sprite;
    }
}
