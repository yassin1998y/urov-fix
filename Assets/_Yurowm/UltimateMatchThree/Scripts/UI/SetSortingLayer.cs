using UnityEngine;
using System.Collections;
using Yurowm.GameCore;

public class SetSortingLayer : MonoBehaviour {

	public enum SortingLayerType {Mesh, Particle, Trail, Sprite};
	public SortingLayerType type = SortingLayerType.Mesh;
    public SortingLayerAndOrder sorting;
	
	void Start () {
		Refresh ();
	}

    [ContextMenu("Refresh")]
    public void Refresh() {
        switch (type) {
            case SortingLayerType.Mesh:
                GetComponent<Renderer>().sortingLayerID = sorting.layerID;
                GetComponent<Renderer>().sortingOrder = sorting.order;
                break;
            case SortingLayerType.Particle:
                GetComponent<ParticleSystem>().GetComponent<Renderer>().sortingLayerID = sorting.layerID;
                GetComponent<ParticleSystem>().GetComponent<Renderer>().sortingOrder = sorting.order;
                break;
            case SortingLayerType.Trail:
                TrailRenderer trail = GetComponent<TrailRenderer>();
                trail.sortingLayerID = sorting.layerID;
                trail.sortingOrder = sorting.order;
                break;
            case SortingLayerType.Sprite:
                SpriteRenderer sprite = GetComponent<SpriteRenderer>();
                sprite.sortingLayerID = sorting.layerID;
                sprite.sortingOrder = sorting.order;
                break;
        }
    }

}