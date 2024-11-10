using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteHelper : MonoBehaviour {
    new SpriteRenderer renderer;

    void Awake () {
        renderer = GetComponent<SpriteRenderer>();
	}

    public void SetTeture(Texture2D texture) {
        renderer.material.SetTexture("_Tex", texture);
        renderer.material.SetVector("_Size", renderer.bounds.size);        
    }
}
