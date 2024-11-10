
using System.Collections.Generic;
using Yurowm.GameCore;

namespace UnityEngine.UI
{
	[AddComponentMenu("UI/Effects/Outline 2", 16)]
	public class UIOutline2 : Shadow
	{
        [Range(0, 5)]
        public float outlineSize = 0.1f;
        [Range(4, 16)]
        public int sides = 9;
        static List<UIVertex> list = new List<UIVertex>(1000);
        static Dictionary<int, Vector2[]> angles = new Dictionary<int, Vector2[]>();

        protected UIOutline2() {}

		public override void ModifyMesh(VertexHelper vh)
		{
			if (this.IsActive())
			{
                if (!angles.ContainsKey(sides)) {
                    angles.Add(sides, new Vector2[sides]);
                    for (int i = 0; i < sides; i++)
                        angles[sides][i] = new Vector2(Mathf.Cos(Mathf.PI * 2 * i / sides), Mathf.Sin(Mathf.PI * 2 * i / sides));
                }

                vh.GetUIVertexStream(list);
				int count = list.Count * sides;
                if (list.Capacity < count) list.Capacity = count;
                int start = 0;
                count = list.Count;

                Vector2[] offset = angles[sides];
                for (int i = 0; i < offset.Length; i++) {
                    ApplyShadowZeroAlloc(list, effectColor, start, start + count, effectDistance.x + outlineSize * offset[i].x, effectDistance.y + outlineSize * offset[i].y);
                    start = start + count;
                }

                vh.Clear();
				vh.AddUIVertexTriangleStream(list);
			}
		}
	}
}