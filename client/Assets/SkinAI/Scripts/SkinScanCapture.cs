using System.Collections;
using UnityEngine;
using Meta.XR;

namespace SkinAI
{
    /// <summary>
    /// Step 2.2 tester: on right index trigger, grabs a centered crop of the passthrough
    /// camera image, sends it to the server, and logs the diagnosis. No UI yet — watch the
    /// logs via:  adb logcat -s Unity   (filter for [SkinScan]).
    /// </summary>
    public class SkinScanCapture : MonoBehaviour
    {
        [Header("References (auto-found if left empty)")]
        public PassthroughCameraAccess passthroughCamera;
        public SkinApiClient apiClient;
        public ScanScopeUI scope;
        public ResultPanelUI resultPanel;

        [Header("Capture settings")]
        [Tooltip("Fraction of the (square) center of the frame to send. Matches the scope area.")]
        [Range(0.2f, 1f)] public float cropFraction = 0.5f;
        [Range(1, 100)] public int jpgQuality = 85;
        [Tooltip("How long the scan shimmer plays before the frame is captured, in seconds.")]
        public float scanDuration = 1.2f;

        private bool _busy;

        private void Start()
        {
            if (passthroughCamera == null) passthroughCamera = FindFirstObjectByType<PassthroughCameraAccess>();
            if (apiClient == null) apiClient = FindFirstObjectByType<SkinApiClient>();
            if (scope == null) scope = FindFirstObjectByType<ScanScopeUI>();
            if (resultPanel == null) resultPanel = FindFirstObjectByType<ResultPanelUI>();

            if (passthroughCamera == null) Debug.LogError("[SkinScan] No PassthroughCameraAccess found in scene.");
            if (apiClient == null) Debug.LogError("[SkinScan] No SkinApiClient found in scene.");
        }

        private void Update()
        {
            bool pressed = OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)
                        || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
            if (pressed && !_busy)
                StartCoroutine(ScanRoutine());
        }

        /// <summary>Trigger -> shimmer -> capture -> send. Shimmer keeps playing until the result returns.</summary>
        private IEnumerator ScanRoutine()
        {
            if (passthroughCamera == null || apiClient == null) yield break;
            if (!passthroughCamera.IsPlaying)
            {
                Debug.LogWarning("[SkinScan] Camera not ready yet (permission/initialization).");
                yield break;
            }

            _busy = true;
            if (scope != null) scope.PlayShimmer();

            // Let the user hold still while the scan animates.
            yield return new WaitForSeconds(scanDuration);

            Texture src = passthroughCamera.GetTexture();
            if (src == null)
            {
                Debug.LogWarning("[SkinScan] Camera texture is null.");
                if (scope != null) scope.StopShimmer();
                _busy = false;
                yield break;
            }

            byte[] jpg = CaptureCroppedJpg(src, cropFraction, jpgQuality);
            if (jpg == null || jpg.Length == 0)
            {
                Debug.LogWarning("[SkinScan] Failed to encode JPG.");
                if (scope != null) scope.StopShimmer();
                _busy = false;
                yield break;
            }

            Debug.Log($"[SkinScan] Captured {src.width}x{src.height}, sending {jpg.Length} bytes...");
            apiClient.Predict(jpg, OnResult);
        }

        private void OnResult(SkinApiClient.PredictResult r)
        {
            _busy = false;
            if (scope != null) scope.StopShimmer();
            if (r == null)
            {
                Debug.LogError("[SkinScan] Null result.");
                if (resultPanel != null) resultPanel.ShowError("No response from server.");
                return;
            }
            if (r.success)
            {
                Debug.Log($"[SkinScan] DIAGNOSIS: {r.prediction}\n{r.stats_text}");
                if (resultPanel != null) resultPanel.ShowResult(r.prediction, r.stats_text);
            }
            else
            {
                Debug.LogError($"[SkinScan] SERVER ERROR: {r.error}");
                if (resultPanel != null) resultPanel.ShowError(r.error);
            }
        }

        /// <summary>Reads a centered square crop from the camera texture and encodes it to JPG.</summary>
        public static byte[] CaptureCroppedJpg(Texture src, float cropFraction, int quality)
        {
            int side = Mathf.RoundToInt(Mathf.Min(src.width, src.height) * Mathf.Clamp(cropFraction, 0.1f, 1f));
            int x = (src.width - side) / 2;
            int y = (src.height - side) / 2;

            RenderTexture rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(side, side, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x, y, side, side), 0, 0);
            tex.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            byte[] jpg = tex.EncodeToJPG(quality);
            Destroy(tex);
            return jpg;
        }
    }
}
