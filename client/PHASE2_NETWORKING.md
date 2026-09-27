# Step 2.2 — Capture a frame and get a diagnosis from the server

Two scripts were added under `Assets/SkinAI/Scripts/`:
- `SkinApiClient.cs` — POSTs a JPG to your server's `/api/predict/` and parses the JSON reply.
- `SkinScanCapture.cs` — on right index trigger, grabs a centered crop of the passthrough
  camera, sends it, and logs the diagnosis.

No UI yet — this step just proves the headset → server → diagnosis round-trip works.

## 1. Start the server + tunnel (on your Mac)
In two terminals (from the Django `Skin AI` folder):
```
# terminal 1 — the server
source .venv/bin/activate
export DJANGO_DEBUG=true
export DJANGO_ALLOWED_HOSTS="*"
python manage.py runserver 0.0.0.0:8000

# terminal 2 — the public HTTPS tunnel
cloudflared tunnel --url http://localhost:8000
```
Copy the `https://xxxx.trycloudflare.com` URL it prints.

> Use the HTTPS tunnel URL, not a plain `http://<ip>:8000` — Android blocks cleartext HTTP
> by default, and the tunnel also lets the headset reach your Mac from any network.

## 2. Wire it up in the scene
1. In the Hierarchy, create an empty GameObject, name it `SkinAI`.
2. Add Component → **Skin Api Client**. Set **Server Base Url** to your tunnel URL
   (no trailing slash).
3. Add Component → **Skin Scan Capture**.
   - Leave `Passthrough Camera` / `Api Client` empty (they auto-find), or drag in the
     `PassthroughCameraAccess` object from the camera building block and this same SkinAI object.
   - `Crop Fraction` 0.5 is a good start (center half of the frame).

## 3. Build, run, and watch the logs
1. `File ▸ Build And Run` to the Quest 3.
2. On your Mac, view the headset logs:
   ```
   adb logcat -s Unity
   ```
   (Leave it running; look for lines tagged `[SkinScan]`.)
3. In the headset, look at some skin (your arm, etc.) and **pull the right index trigger**.
4. You should see, in logcat:
   ```
   [SkinScan] Captured 1280x960, sending NNNNN bytes...
   [SkinScan] DIAGNOSIS: <result>
   <stats text>
   ```
   And in the server terminal, a `POST /api/predict/ 200` line.

✅ **When the diagnosis appears in logcat, Step 2.2 is done** and we move to 2.3
(scope reticle, particle shimmer, and the floating result panel).

## Notes / troubleshooting
- **The tunnel URL changes every time you restart `cloudflared`** — update Server Base Url
  in the Inspector when it does.
- **`Cannot connect` / timeout:** confirm both terminals are running and the URL is current;
  test it from a browser first (open `https://xxxx.trycloudflare.com` — the upload page loads).
- **Trigger does nothing:** make sure your controllers are tracked; the script listens to
  both left and right index triggers. Check logcat for `[SkinScan] Camera not ready`.
- **`Camera not ready`:** wait a second after launch / re-approve the camera permission.
