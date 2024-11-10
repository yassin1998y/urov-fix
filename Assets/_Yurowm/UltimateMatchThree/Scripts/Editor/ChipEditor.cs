using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Yurowm.GameCore;
using Clip = ContentAnimator.Clip;
using Sound = ContentSound.Sound;
using VibroClip = ContentVibration.VibroClip;

[CustomEditor(typeof(ContentAnimator))]
[CanEditMultipleObjects]
public class ContentAnimatorEditor : Editor {

    ContentAnimator provider;

    IEnumerable<IGrouping<string, Clip>> clipGroups;

    void OnEnable () {
        provider = (ContentAnimator) target;

        foreach (ContentAnimator animator in serializedObject.targetObjects) {
            List<Clip> clips = animator.GetClipNames().Select(x => new Clip(x)).ToList();

            foreach (Clip clip in clips)
                if (!animator.clips.Contains(clip))
                    animator.clips.Add(clip);

            foreach (Clip clip in new List<Clip>(animator.clips))
                if (!clips.Contains(clip) && animator.clips.Contains(clip))
                    animator.clips.Remove(clip);

            animator.clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        if (serializedObject.isEditingMultipleObjects)
            clipGroups = serializedObject.targetObjects.SelectMany(x => (x as ContentAnimator).clips).GroupBy(x => x.name);
    }

    public override void OnInspectorGUI() {
        if (serializedObject.isEditingMultipleObjects) {
            Undo.RecordObjects(serializedObject.targetObjects, "Chips is changed");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreTimeScale"));

            if (GUILayout.Button("Put into Animation", GUILayout.Width(130)))
                serializedObject.targetObjects.ForEach(x => (x as ContentAnimator).PutAnimations());

            AnimationClip value;
            foreach (IGrouping<string, Clip> group in clipGroups) {
                value = group.First().clip;
                EditorGUI.showMixedValue = !group.All(x => x.clip == value);
                EditorGUI.BeginChangeCheck();
                value = ClipSelector(group.Key, value);
                if (EditorGUI.EndChangeCheck())
                    group.ForEach(x => x.clip = value);
                EditorGUI.showMixedValue = false;
            }
        } else {
            Undo.RecordObject(target, "Chip is changed");

            provider.ignoreTimeScale = EditorGUILayout.Toggle("Ignore Time Scale", provider.ignoreTimeScale);

            if (GUILayout.Button("Put into Animation", GUILayout.Width(130)))
                provider.PutAnimations();

            foreach (Clip clip in provider.clips)
                clip.clip = ClipSelector(clip.name, clip.clip);

            EditorUtility.SetDirty(target);
        }
    }

    AnimationClip ClipSelector(string name, AnimationClip selected) {
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
        if (Event.current.type == EventType.Layout)
            return selected;

        Rect rect2 = EditorGUI.PrefixLabel(rect, new GUIContent(name));
        rect.xMin = rect2.x;

        Rect objectRect = new Rect(rect);
        return (AnimationClip) EditorGUI.ObjectField(objectRect, selected, typeof(AnimationClip), false);
    }
}

[CustomEditor(typeof(ContentSound))]
[CanEditMultipleObjects]
public class ContentSoundEditor : Editor {

    ContentSound provider;
    AudioEditor.SoundSelector selector = null;

    IEnumerable<IGrouping<string, Sound>> clipGroups;

    void OnEnable() {
        selector = new AudioEditor.SoundSelector();

        foreach (ContentSound sound in serializedObject.targetObjects) {
            List<Sound> clips = sound.GetClipNames().Select(x => new Sound(x)).ToList();

            foreach (Sound clip in clips)
                if (!sound.clips.Contains(clip))
                    sound.clips.Add(clip);

            foreach (Sound clip in new List<Sound>(sound.clips))
                if (!clips.Contains(clip) && sound.clips.Contains(clip))
                    sound.clips.Remove(clip);

            sound.clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        if (serializedObject.isEditingMultipleObjects)
            clipGroups = serializedObject.targetObjects.SelectMany(x => (x as ContentSound).clips).GroupBy(x => x.name);
        else
            provider = (ContentSound) target;
    }

    public override void OnInspectorGUI() {
        if (AudioAssistant.main == null) {
            EditorGUILayout.HelpBox("AudioAssistant is missing", MessageType.Error);
            return;
        }

        string value;
        if (serializedObject.isEditingMultipleObjects) {
            Undo.RecordObjects(serializedObject.targetObjects, "Chips is changed");


            foreach (IGrouping<string, Sound> group in clipGroups) {
                value = group.First().clip;
                EditorGUI.showMixedValue = !group.All(x => x.clip == value);
                EditorGUI.BeginChangeCheck();
                value = selector.Select(group.Key, value);
                if (EditorGUI.EndChangeCheck())
                    group.ForEach(x => x.clip = value);
                EditorGUI.showMixedValue = false;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("onlyDuringSession"));
        } else {
            Undo.RecordObject(target, "Chip is changed");
            foreach (Sound clip in provider.clips) 
                clip.clip = selector.Select(clip.name, clip.clip);
            provider.onlyDuringSession = EditorGUILayout.Toggle("Only During Session", provider.onlyDuringSession);

            EditorUtility.SetDirty(target);
        }


    }
}

[CustomEditor(typeof(ContentVibration))]
[CanEditMultipleObjects]
public class ContentVibrationEditor : Editor {

    ContentVibration provider;

    IEnumerable<IGrouping<string, VibroClip>> clipGroups;

    void OnEnable() {
        foreach (ContentVibration vibrator in serializedObject.targetObjects) {
            List<VibroClip> clips = vibrator.GetClipNames().Select(x => new VibroClip(x)).ToList();

            foreach (VibroClip clip in clips)
                if (!vibrator.clips.Contains(clip))
                    vibrator.clips.Add(clip);

            foreach (VibroClip clip in new List<VibroClip>(vibrator.clips))
                if (!clips.Contains(clip) && vibrator.clips.Contains(clip))
                    vibrator.clips.Remove(clip);

            vibrator.clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        if (serializedObject.isEditingMultipleObjects)
            clipGroups = serializedObject.targetObjects.SelectMany(x => (x as ContentVibration).clips).GroupBy(x => x.name);
        else
            provider = (ContentVibration) target;
    }

    public override void OnInspectorGUI() {
        long value;
        if (serializedObject.isEditingMultipleObjects) {
            Undo.RecordObjects(serializedObject.targetObjects, "Chips is changed");

            foreach (IGrouping<string, VibroClip> group in clipGroups) {
                value = group.First().duration;
                EditorGUI.showMixedValue = !group.All(x => x.duration == value);
                EditorGUI.BeginChangeCheck();
                value = (long) Mathf.Max(0, EditorGUILayout.LongField(group.Key, value));
                if (EditorGUI.EndChangeCheck())
                    group.ForEach(x => x.duration = value);
                EditorGUI.showMixedValue = false;
            }
        } else {
            Undo.RecordObject(target, "Chip is changed");
            foreach (VibroClip clip in provider.clips) 
                clip.duration = (long) Mathf.Max(0, EditorGUILayout.LongField(clip.name, clip.duration));

            EditorUtility.SetDirty(target);
        }


    }
}