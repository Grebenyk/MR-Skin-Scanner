# Skin-AI XR — Phase 2 (Unity / Oculus Quest 3)

Project: `XRSkinAI` · Unity 6000.4.4f1 · Meta XR SDK All v203 · URP

Goal: in passthrough, aim a scope at skin → pull trigger → scan shimmer →
capture the real camera image → send to the Skin-AI server → show the diagnosis
on a panel floating in front of you.

We build this in three approved sub-steps:
- **2.1 — Camera access foundation** (this document)
- 2.2 — Networking: capture a frame and get a diagnosis from the server
- 2.3 — Full experience: scope, trigger, particle shimmer, result panel

---

# Step 2.1 — Get the passthrough camera working

The real-world view in passthrough is composited by the headset, so a normal
screenshot can't see it. We must use Meta's **Passthrough Camera API (PCA)**.

> Reminder: PCA only works on a **physical Quest 3 / 3S** (Horizon OS v74+),
> NOT in the XR Simulator. You'll deploy a build to the headset to test.

## A. Switch the project to Android (Quest)
1. `File ▸ Build Profiles` (or Build Settings) → select **Android** → **Switch Platform**.
   (First switch can take a while as it reimports assets.)

## B. Run Meta's Project Setup Tool
1. `Meta ▸ Tools ▸ Project Setup Tool`.
2. Click **Fix All**, then **Apply All** for both Android and the active build target.
   This configures XR plugin, graphics, and most player settings automatically.

> No cloning needed. The Passthrough building block only renders the see-through
> view — it does NOT expose camera pixels. Camera-pixel access comes from
> **MR Utility Kit (MRUK)**, which is already in your v203 SDK and provides a
> ready-made building block plus the `PassthroughCameraAccess` component our
> scripts will use in Step 2.2.

## C. Add the camera building block
1. Open `Meta ▸ Tools ▸ Building Blocks` (the same window where you added
   Passthrough and Controller Tracking).
2. Add the **Passthrough Camera Visualizer** block. This:
   - adds the `PassthroughCameraAccess` component (the camera API), and
   - shows a quad with the live camera feed so we can confirm it works.
   (There's also a plain **Passthrough Camera Access** block with no visual — we'll
   switch to that later; for now the Visualizer is the easy confidence check.)

## D. Let the Project Setup Tool add the permission
1. `Meta ▸ Tools ▸ Project Setup Tool`.
2. Click **Fix All**, then **Apply All** (for Android).
   It automatically adds `horizonos.permission.HEADSET_CAMERA` to the manifest and
   sets OVRManager to request the camera permission on app startup — no manual
   manifest editing required.

## E. Confirm the camera works on-device
1. Connect your Quest 3 (USB, developer mode on; `adb devices` should list it).
2. `File ▸ Build And Run`.
3. Put on the headset. Approve the camera permission prompt the first time.
4. You should see the live passthrough camera feed on the visualizer quad.

✅ **When you can see the camera feed on the headset, Step 2.1 is done.**

---

## What I need from you to start 2.2
Once the camera feed shows on the headset, tell me. I've already read MRUK's
`PassthroughCameraAccess` API (it exposes `GetTexture()` at a requested resolution,
default 1280×960), so I'll write the capture + upload code against it directly —
crop to the scope, encode JPG, POST to `/api/predict/`.

## Troubleshooting
- **Black/blank feed:** check the permission was granted (Settings ▸ Apps ▸ your
  app ▸ Permissions ▸ Camera), and that you're on Horizon OS v74+.
- **No camera building block listed:** ensure MR Utility Kit is installed
  (Package Manager → it ships with the All-in-One SDK v203), then reopen Building Blocks.
- **Can't deploy:** confirm Developer Mode is enabled in the Meta Horizon phone app
  and `adb devices` lists the headset.
