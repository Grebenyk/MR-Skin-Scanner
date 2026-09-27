using UnityEngine;

namespace SkinAI
{
    /// <summary>
    /// Draws a head-locked, centered "scope" reticle (four corner brackets) in passthrough.
    /// It is centered in the user's view because the capture sends the CENTER crop of the
    /// head-facing camera — so what's inside the brackets is roughly what gets scanned.
    /// Built entirely at runtime with LineRenderers; just add this component to a GameObject.
    /// </summary>
    public class ScanScopeUI : MonoBehaviour
    {
        [Header("Placement")]
        [Tooltip("Distance in front of the eyes, in meters.")]
        public float distance = 0.5f;
        [Tooltip("Side length of the square scope at that distance, in meters.")]
        public float sizeMeters = 0.15f;

        [Header("Look")]
        [Tooltip("Length of each corner bracket arm as a fraction of the scope size.")]
        [Range(0.1f, 0.5f)] public float cornerFraction = 0.3f;
        public float lineWidth = 0.004f;
        public Color color = new Color(0.2f, 0.9f, 1f, 1f);

        [Header("Scan shimmer")]
        [Tooltip("Particles emitted per second while scanning.")]
        public float shimmerRate = 140f;
        public Color shimmerColor = new Color(0.4f, 0.95f, 1f, 1f);

        private Transform _scope;
        private bool _built;
        private ParticleSystem _shimmer;

        private void LateUpdate()
        {
            if (_built)
                return;

            Camera cam = Camera.main;
            if (cam == null)
            {
                // Fall back to any enabled camera (the XR rig camera may init a frame late).
                foreach (var c in Camera.allCameras) { cam = c; break; }
            }
            if (cam == null)
                return;

            Build(cam.transform);
            _built = true;
        }

        private void Build(Transform head)
        {
            var root = new GameObject("ScanScope");
            _scope = root.transform;
            _scope.SetParent(head, false);              // head-locked
            _scope.localPosition = new Vector3(0f, 0f, distance);
            _scope.localRotation = Quaternion.identity;

            float h = sizeMeters * 0.5f;
            float arm = sizeMeters * cornerFraction;

            // Top-left, top-right, bottom-left, bottom-right corner brackets.
            BuildCorner("TL", new Vector3(-h, h - arm, 0), new Vector3(-h, h, 0), new Vector3(-h + arm, h, 0));
            BuildCorner("TR", new Vector3(h - arm, h, 0), new Vector3(h, h, 0), new Vector3(h, h - arm, 0));
            BuildCorner("BL", new Vector3(-h, -h + arm, 0), new Vector3(-h, -h, 0), new Vector3(-h + arm, -h, 0));
            BuildCorner("BR", new Vector3(h - arm, -h, 0), new Vector3(h, -h, 0), new Vector3(h, -h + arm, 0));

            BuildShimmer();
        }

        private void BuildShimmer()
        {
            var go = new GameObject("ScanShimmer");
            go.transform.SetParent(_scope, false);
            go.transform.localPosition = Vector3.zero;

            _shimmer = go.AddComponent<ParticleSystem>();
            _shimmer.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _shimmer.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.012f);
            main.startColor = shimmerColor;
            main.maxParticles = 400;

            var emission = _shimmer.emission;
            emission.rateOverTime = shimmerRate;

            // Spawn anywhere inside the scope square (thin box on the scope plane).
            var shape = _shimmer.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(sizeMeters, sizeMeters, 0.02f);

            // Twinkle: fade alpha in then out over each particle's life.
            var col = _shimmer.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            // Gentle pulse in size.
            var sol = _shimmer.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.3f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.3f)));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.mainTexture = MakeDotTexture();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        /// <summary>Soft round dot texture so particles read as glints, not squares.</summary>
        private static Texture2D MakeDotTexture(int size = 32)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                a = a * a; // soft falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return tex;
        }

        /// <summary>Start the scan shimmer (call when the trigger is pulled).</summary>
        public void PlayShimmer()
        {
            if (_shimmer != null) _shimmer.Play(true);
        }

        /// <summary>Stop the scan shimmer (call when the result arrives).</summary>
        public void StopShimmer()
        {
            if (_shimmer != null) _shimmer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void BuildCorner(string name, Vector3 a, Vector3 b, Vector3 c)
        {
            var go = new GameObject("Corner_" + name);
            go.transform.SetParent(_scope, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;                  // positions are local to the scope
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCornerVertices = 2;
            lr.numCapVertices = 2;
            lr.widthMultiplier = lineWidth;
            lr.positionCount = 3;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.SetPosition(2, c);

            // Sprites/Default honors vertex colors and renders fine under URP.
            var mat = new Material(Shader.Find("Sprites/Default"));
            lr.material = mat;
            lr.startColor = color;
            lr.endColor = color;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }
    }
}
