using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// received stars indicator (for current level and current score)
public class ScoreStar : MonoBehaviour {

    Animation anim;
	bool filled = false;

	//public Sprite fullStar; // Image of received star
	//public Sprite emptyStar; // Image of unreceived star
	public StarType starType; // displaying star type - first, second, third
	public bool fromCurrentScore = true;

    public GameObject star1;
    public GameObject star2;
    public GameObject star3;

	void Awake () {
        anim = GetComponent<Animation>();
	}

    void OnEnable() {
        star1.SetActive(GetStar(StarType.First));
        star2.SetActive(GetStar(StarType.Second));
        star3.SetActive(GetStar(StarType.Third));
    }

    bool GetStar (StarType star) {
		filled = false;

		float target = 0;
        if (fromCurrentScore) {
            switch (star) {
                case StarType.First: target = SessionInfo.current.design.firstStarScore; break; 
                case StarType.Second: target = SessionInfo.current.design.secondStarScore; break;
                case StarType.Third: target = SessionInfo.current.design.thirdStarScore; break;
            }
            target = SessionInfo.current.GetScore();
        } else {
            switch (star) {
                case StarType.First: target = LevelDesign.selected.firstStarScore; break; 
                case StarType.Second: target = LevelDesign.selected.secondStarScore; break;
                case StarType.Third: target = LevelDesign.selected.thirdStarScore; break;
            }
            target = CurrentUser.main.GetScore(LevelDesign.selected.number);
        }

        if (fromCurrentScore)
            filled = target <= SessionInfo.current.GetScore();
        else
            filled = target <= CurrentUser.main.GetScore(LevelDesign.selected.number);

        if (anim)
            anim.enabled = filled;

        return filled;
	}
}
