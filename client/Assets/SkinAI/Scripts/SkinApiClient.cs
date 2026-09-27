using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SkinAI
{
    /// <summary>
    /// Sends a JPG image to the Skin-AI server (/api/predict/) and returns the parsed result.
    /// The server responds with JSON: { "success": bool, "prediction": str, "stats_text": str, "error": str }
    /// </summary>
    public class SkinApiClient : MonoBehaviour
    {
        [Tooltip("Base URL of the Skin-AI server. NO trailing slash.\n" +
                 "Use your Cloudflare tunnel HTTPS URL, e.g. https://xxxx.trycloudflare.com\n" +
                 "(Plain http:// is blocked on Quest by default, so prefer the https tunnel.)")]
        public string serverBaseUrl = "https://CHANGE-ME.trycloudflare.com";

        [Tooltip("Request timeout in seconds.")]
        public int timeoutSeconds = 30;

        [Serializable]
        public class PredictResult
        {
            public bool success;
            public string prediction;
            public string stats_text;
            public string error;
        }

        /// <summary>Posts the JPG and invokes onDone with the result (always called, even on failure).</summary>
        public void Predict(byte[] jpgBytes, Action<PredictResult> onDone)
        {
            StartCoroutine(PredictRoutine(jpgBytes, onDone));
        }

        private IEnumerator PredictRoutine(byte[] jpgBytes, Action<PredictResult> onDone)
        {
            string baseUrl = (serverBaseUrl ?? "").Trim();
            if (baseUrl.Length == 0 || baseUrl.Contains("CHANGE-ME"))
            {
                onDone?.Invoke(new PredictResult { success = false, error = "serverBaseUrl is not set on SkinApiClient." });
                yield break;
            }

            // Be forgiving: prepend https:// if the scheme is missing.
            if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
                baseUrl = "https://" + baseUrl;

            string url = baseUrl.TrimEnd('/') + "/api/predict/";
            Debug.Log("[SkinScan] POST " + url);
            var form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("image", jpgBytes, "scan.jpg", "image/jpeg")
            };

            using (UnityWebRequest req = UnityWebRequest.Post(url, form))
            {
                req.timeout = timeoutSeconds;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onDone?.Invoke(new PredictResult
                    {
                        success = false,
                        error = $"{req.error} (HTTP {req.responseCode})"
                    });
                    yield break;
                }

                PredictResult result;
                try
                {
                    result = JsonUtility.FromJson<PredictResult>(req.downloadHandler.text);
                }
                catch (Exception e)
                {
                    result = new PredictResult { success = false, error = "JSON parse error: " + e.Message };
                }
                onDone?.Invoke(result);
            }
        }
    }
}
