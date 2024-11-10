using System;

namespace UnityEngine.UI {
    [AddComponentMenu("UI/Particle System UI")]
    [RequireComponent (typeof (ParticleSystem))]
    public class UIParticleSystem : MaskableGraphic {

        public override Texture mainTexture {
            get {
                if (overrideSprite == null) {
                    if (material != null && material.mainTexture != null) {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }

                return overrideSprite.texture;
            }
        }

        public bool clearOnEnable = true;

        ParticleSystem pSystem;
        protected override void Awake() {
            base.Awake();

            pSystem = GetComponent<ParticleSystem>();

        }

        protected override void OnEnable() {
            base.OnEnable();
            if (clearOnEnable) {
                pSystem.Clear();
            }
        }

        void Update() {
            if (pSystem && pSystem.IsAlive())
                SetVerticesDirty();
        }

        [NonSerialized]
        private Sprite m_OverrideSprite;
        public Sprite overrideSprite {
            get {
                return m_OverrideSprite ?? sprite;
            }
            set {
                if (SetPropertyUtility.SetClass(ref m_OverrideSprite, value))
                    SetAllDirty();
            }
        }


        [SerializeField]
        private Sprite m_Sprite;
        public Sprite sprite {
            get {
                return m_Sprite;
            }
            set {
                if (SetPropertyUtility.SetClass(ref m_Sprite, value))
                    SetAllDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper toFill) {
            GenerateMesh(toFill);
        }

        readonly Vector2[] quad = new Vector2[] {
            new Vector2(-.5f, .5f),
            new Vector2(-.5f, -.5f),
            new Vector2(.5f, -.5f),
            new Vector2(.5f, .5f) 
        };

        readonly Vector2[] uv = new Vector2[] {
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1)
        };

        void GenerateMesh(VertexHelper vh) {
            vh.Clear();

            Vector3 vertex = new Vector3();

            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[pSystem.particleCount];
            pSystem.GetParticles(particles);

            Color32 color;
            float angle;
            int i = 0;
            foreach (var particle in particles) {
                color = particle.GetCurrentColor(pSystem);

                for (int s = 0; s < 4; s++) {
                    angle = particle.rotation * Mathf.Deg2Rad;
                    vertex.x = quad[s].x * Mathf.Cos(angle) - quad[s].y * Mathf.Sin(angle);
                    vertex.y = quad[s].x * Mathf.Sin(angle) + quad[s].y * Mathf.Cos(angle);

                    vertex *= particle.GetCurrentSize(pSystem);

                    vertex += particle.position;

                    vertex.x *= rectTransform.rect.size.x;
                    vertex.y *= rectTransform.rect.size.y;

                    vh.AddVert(vertex, color, uv[s]);
                }

                vh.AddTriangle(i + 0, i + 1, i + 2);
                vh.AddTriangle(i + 0, i + 2, i + 3);

                i += 4;
            }
        }
    }

    internal static class SetPropertyUtility {
        public static bool SetColor(ref Color currentValue, Color newValue) {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }
    }
}
