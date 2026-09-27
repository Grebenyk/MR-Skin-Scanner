# Skin-AI — Remote Server Deployment (Phase 1)

Goal: run the Django + PyTorch app on a remote Hetzner server so the browser
test client can send an image and get a diagnosis back.

This is the **reference**; your assistant will walk you through it step by step.

---

## What you'll do
1. Create a Hetzner Cloud server (CX22, Ubuntu 24.04).
2. Upload this project folder to the server.
3. Create a `.env` file and run `deploy.sh`.
4. Open `test_client.html` on your computer and test.

---

## 1. Create the server
1. Sign up at https://console.hetzner.cloud and create a new **Project**.
2. Add your **SSH key** (Project → Security → SSH Keys). On macOS, your public key is
   usually at `~/.ssh/id_ed25519.pub` (create one with `ssh-keygen -t ed25519` if needed).
3. **Add Server**:
   - Location: closest to you.
   - Image: **Ubuntu 24.04**.
   - Type: **CX22** (2 vCPU, 4 GB RAM).
   - SSH key: select the one you added.
   - Create.
4. Copy the server's **public IPv4** address.

## 2. Upload the project
From your Mac, in the folder that contains this project (one level above `Skin AI/`):

```bash
# replace SERVER_IP with your real IP
rsync -avz --exclude='.venv' --exclude='db.sqlite3' \
  "Skin AI/" root@SERVER_IP:/opt/skinai/
```
(If you don't have rsync, `scp -r "Skin AI" root@SERVER_IP:/opt/skinai` works too.)

## 3. Configure and deploy
SSH in and run the deploy script:

```bash
ssh root@SERVER_IP
cd /opt/skinai
cp .env.example .env
nano .env          # set DJANGO_SECRET_KEY, and DJANGO_ALLOWED_HOSTS=SERVER_IP
bash deploy.sh
```

`deploy.sh` installs Python + dependencies (including PyTorch — takes a few minutes),
runs migrations, starts a gunicorn service, and opens the firewall on port 8000.

Quick check on the server:
```bash
curl http://127.0.0.1:8000/api/predict/    # -> {"success": false, "error": "Тільки POST запити"}
```
That error is expected (GET not allowed) and means the server is up.

## 4. Test from your computer
1. Open `test_client.html` (double-click it).
2. Enter `http://SERVER_IP:8000` as the server URL.
3. Pick a skin photo and click **Analyze**.
4. You should see the diagnosis returned from the remote server.

---

## Useful server commands
```bash
systemctl status skinai      # is it running?
journalctl -u skinai -f      # live logs
systemctl restart skinai     # restart after changes
```

## Notes / what comes later
- Phase 1 uses plain HTTP on port 8000 — fine for desktop browser testing.
- For the Oculus phase we'll add a domain + HTTPS (nginx + Let's Encrypt), because
  Quest apps prefer secure connections. The settings already support running behind
  an HTTPS proxy.
