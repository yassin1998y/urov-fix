using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent (typeof (Graphic))]
public class UIAnimationIssue : MonoBehaviour {

    Animation anim;
    CPanel panel;
    Graphic graphic;

    public bool vertices = true;
    public bool layout = false;
    public bool material = false;

    void Awake () {
        anim = GetComponentInParent<Animation>();
        panel = GetComponentInParent<CPanel>();
        graphic = GetComponent<Graphic>();
	}
	
	// Update is called once per frame
	void Update () {
        if (anim.isPlaying || (panel && panel.isPlaying)) {
            if (vertices) graphic.SetVerticesDirty();
            if (layout) graphic.SetLayoutDirty();
            if (material) graphic.SetMaterialDirty();
        }
	}
}
