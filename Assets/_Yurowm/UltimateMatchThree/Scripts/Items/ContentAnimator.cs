using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Yurowm.GameCore;
using System;
using System.Linq;

[RequireComponent (typeof (Animation))]
public class ContentAnimator : MonoBehaviour {

    public List<Clip> clips = new List<Clip>();
    public bool ignoreTimeScale = false;

    string current_clip;

    Animation anim;
    Dictionary<string, AnimationClip> _clips = new Dictionary<string, AnimationClip>();

    public Clip this[string name] {
        get {
            return clips.FirstOrDefault(x => x.name == name);
        }
    }

    void Awake() {
        anim = GetComponent<Animation>();
        PutAnimations();
    }

    public List<string> GetClipNames() {
        List<string> result = new List<string>();

        foreach (Component component in GetComponents<Component>())
            if (component is IAnimated)
                result.AddRange(Utils.Collect<string>((component as IAnimated).GetAnimationNames()).ToList());

        result.Sort();

        return result.Distinct().ToList();
    }

    public void Play(string clip_name) {
        if (_clips.ContainsKey(clip_name)) {
            current_clip = clip_name;
            anim.Play(_clips[clip_name].name);
        }
    }

    public WaitWhile PlayAndWait(string clip_name) {
        if (_clips.ContainsKey(clip_name)) {
            current_clip = clip_name;
            anim.Play(_clips[clip_name].name);
            return new WaitWhile(() => anim.IsPlaying(_clips[clip_name].name));
        }
        return new WaitWhile(() => false);
    }

    public void Stop(string clip_name) {
        if (_clips.ContainsKey(clip_name))
            anim.Stop(_clips[clip_name].name);
        current_clip = null;
    }

    public void Complete(string clip_name) {
        if (_clips.ContainsKey(clip_name))
            StartCoroutine(CompleteRoutine(clip_name));
    }

    public void PutAnimations() {
        anim = GetComponent<Animation>();
        _clips.Clear();
        foreach (Clip clip in clips)
            if (clip.clip != null)
                _clips.Add(clip.name, clip.clip);
        foreach (KeyValuePair<string, AnimationClip> pair in _clips)
            if (pair.Value != null)
                anim.AddClip(pair.Value, pair.Value.name);
    }

    public void CompleteAndPlay(string clip_name) {
        if (_clips.ContainsKey(clip_name))
            StartCoroutine(CompleteAndPlayRoutine(clip_name));
    }

    public bool IsPlaying(string clip_name) {
        if (_clips.ContainsKey(clip_name))
            return anim.IsPlaying(_clips[clip_name].name);
        return false;
    }

    public bool IsPlaying() {
        return anim.isPlaying;
    }

    IEnumerator CompleteRoutine(string clip_name) {
        while (anim[_clips[clip_name].name].time % anim[_clips[clip_name].name].length > 0.1f)
            yield return 0;
        anim[_clips[clip_name].name].time = 0;
        yield return 0;
        anim.Stop(_clips[clip_name].name);
        current_clip = null;
    }

    IEnumerator CompleteAndPlayRoutine(string clip_name) {
        while (IsPlaying())
            yield return 0;
        anim.Play(clip_name);
    }

    void Update() {
        if (ignoreTimeScale)
            if (Time.timeScale == 0 && IsPlaying())
                anim[_clips[current_clip].name].time += Time.unscaledDeltaTime;
    }

    [System.Serializable]
    public class Clip {
        public string name;
        public AnimationClip clip;

        public Clip(string _name) {
            name = _name;
        }

        public static bool operator ==(Clip a, Clip b) {
            return a.Equals(b);
        }

        public static bool operator !=(Clip a, Clip b) {
            return !a.Equals(b);
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is Clip))
                return false;
            return name == ((Clip) obj).name;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }
}
