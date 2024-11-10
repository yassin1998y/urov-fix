using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent (typeof (Image))]
public class StarImageActivator : MonoBehaviour {

    public StarType starType;
        
    public bool fromCurrentScore = true;

    Image image;
    void Awake() {
        image = GetComponent<Image>();
    }

    void OnEnable() {
        image.enabled = GetStar(starType);
    }

    bool GetStar(StarType star) {
        float target = 0;
        if (fromCurrentScore) {
            switch (star) {
                case StarType.First: target = SessionInfo.current.design.firstStarScore; break; 
                case StarType.Second: target = SessionInfo.current.design.secondStarScore; break;
                case StarType.Third: target = SessionInfo.current.design.thirdStarScore; break;
            }
            return target <= SessionInfo.current.GetScore();
        } else {
            switch (star) {
                case StarType.First: target = LevelDesign.selected.firstStarScore; break; 
                case StarType.Second: target = LevelDesign.selected.secondStarScore; break;
                case StarType.Third: target = LevelDesign.selected.thirdStarScore; break;
            }
            return target <= CurrentUser.main.GetScore(LevelDesign.selected.number);
        }
    }
}
