using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

[RequireComponent (typeof (ContentSound))]
public class ContentVibration : MonoBehaviour {
    public List<VibroClip> clips = new List<VibroClip>();
    Dictionary<string, long> _clips = new Dictionary<string, long>();

    void Awake() {
        _clips = clips.ToDictionary(x => x.name, x => x.duration);
    }

    public void Shot(string name) {
        if (_clips.ContainsKey(name))
            Vibration.main.Vibrate(_clips[name]);
    }

    public List<string> GetClipNames() {
        List<string> result = new List<string>();

        foreach (Component component in GetComponents<Component>())
            if (component is ISounded)
                result.AddRange(Utils.Collect<string>((component as ISounded).GetSoundNames()).ToList());

        result.Sort();

        return result.Distinct().ToList();
    }

    [Serializable]
    public class VibroClip {
        public string name;
        public long duration;

        public VibroClip(string _name) {
            name = _name;
        }

        public static bool operator ==(VibroClip a, VibroClip b) {
            return a.Equals(b);
        }

        public static bool operator !=(VibroClip a, VibroClip b) {
            return !a.Equals(b);
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is VibroClip))
                return false;
            return name == ((VibroClip) obj).name;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }
}
