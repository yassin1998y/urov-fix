using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yurowm.GameCore;

public class ObjectTag : MonoBehaviour {
    static Dictionary<string, List<GameObject>> objects = new Dictionary<string, List<GameObject>>();

    [RuntimeInitializeOnLoadMethod]
    public static void Initialize() {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject root in roots) {
            foreach (var comp in root.GetComponentsInChildren<ObjectTag>(true))
                comp.Add();
        }
    }
    
    public new string tag = "";
    string _tag;
	
	void OnDestroy() {
        Remove();
    }

    void Add() {
        _tag = tag;
        if (!objects.ContainsKey(_tag))
            objects.Add(_tag, new List<GameObject>());
        objects[_tag].Add(gameObject);
    }

    void Remove() {
        if (!_tag.IsNullOrEmpty() && objects.ContainsKey(_tag))
            objects[_tag].Remove(gameObject);
    }

    public static List<GameObject> Get(string tag) {
        if (objects.ContainsKey(tag) && objects[tag].Count > 0)
            return objects[tag].ToList();
        return new List<GameObject>();
    }

    public static GameObject GetFirst(string tag) {
        if (objects.ContainsKey(tag) && objects[tag].Count > 0)
            return objects[tag][0];
        return null;
    }

    public static GameObject GetRandom(string tag) {
        if (objects.ContainsKey(tag) && objects[tag].Count > 0)
            return objects[tag].GetRandom();
        return null;
    }

    public static List<T> Get<T>(string tag) where T : Component {
        if (objects.ContainsKey(tag) && objects[tag].Count > 0)
            return objects[tag].Select(x => x.GetComponent<T>()).ToList();
        return new List<T>();
    }

    public static T GetFirst<T>(string tag) where T : Component {
        if (objects.ContainsKey(tag) && objects[tag].Count > 0)
            return objects[tag][0].GetComponent<T>();
        return null;
    }

    public static T GetRandom<T>(string tag) where T : Component {
        if (objects.ContainsKey(tag) && objects[tag].Count > 0)
            return objects[tag].GetRandom().GetComponent<T>();
        return null;
    }
}
