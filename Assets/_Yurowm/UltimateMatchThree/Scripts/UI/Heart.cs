using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class Heart : MonoBehaviour {

    public int number;
    Image image;
    Color color;

    void Awake () {
        image = GetComponent<Image>();
        color = image.color;
        color.a = 1;
        image.color = color;
	}
	
	// Update is called once per frame
	void Update () {
        color = image.color;
        if (CurrentUser.main.lifeSystem.IsFull())
            color.a = 1;
        else if (number < CurrentUser.main[ItemID.life])
            color.a = 1;
        else if (number > CurrentUser.main[ItemID.life])
            color.a = 0;
        else
            color.a =  CurrentUser.main.lifeSystem.GetLifeProgression();
        image.color = color;
	}
}
