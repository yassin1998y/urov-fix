using System.Collections.Generic;
using System.Linq;
using Yurowm.GameCore;
using UnityEngine;

public class ContentSound : MonoBehaviour {

    public List<Sound> clips = new List<Sound>();
    public bool onlyDuringSession = true;
    ContentVibration vibration = null;

    Dictionary<string, string> _clips = new Dictionary<string, string>();
    void Awake() {
        _clips = clips.ToDictionary(x => x.name, x => x.clip);
        vibration = GetComponent<ContentVibration>();
    }

    public List<string> GetClipNames() {
        List<string> result = new List<string>();

        foreach (Component component in GetComponents<Component>())
            if (component is ISounded)
                result.AddRange(Utils.Collect<string>((component as ISounded).GetSoundNames()).ToList());

        result.Sort();

        return result.Distinct().ToList();
    }
    
    public void Play(string clip_name) {
        if (!onlyDuringSession || (SessionInfo.current.isPlaying && _clips.ContainsKey(clip_name))) {
            AudioAssistant.Shot(_clips[clip_name]);
            if (vibration) vibration.Shot(clip_name);
        }
    }

    [System.Serializable]
    public class Sound {
        public string name;
        public string clip;

        public Sound(string _name) {
            name = _name;
        }

        public static bool operator ==(Sound a, Sound b) {
            return a.Equals(b);
        }

        public static bool operator !=(Sound a, Sound b) {
            return !a.Equals(b);
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is Sound))
                return false;
            return name == ((Sound) obj).name;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }
}
