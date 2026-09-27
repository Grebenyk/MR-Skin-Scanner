#!/usr/bin/env bash
# Skin-AI deployment script for Ubuntu 24.04 (Hetzner CX22 or any Debian/Ubuntu VPS).
# Run this ON THE SERVER, from inside the uploaded project folder, as root (or with sudo).
#
#   cd /opt/skinai
#   sudo bash deploy.sh
#
# It is safe to re-run (idempotent-ish): it reinstalls deps and restarts the service.

set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVICE_USER="${SUDO_USER:-root}"
VENV_DIR="$PROJECT_DIR/.venv"

echo "==> Project dir: $PROJECT_DIR"

# 1. System packages
echo "==> Installing system packages..."
export DEBIAN_FRONTEND=noninteractive
apt-get update -y
apt-get install -y python3 python3-venv python3-pip ufw

# 2. Python virtual environment + dependencies
echo "==> Creating virtual environment..."
python3 -m venv "$VENV_DIR"
"$VENV_DIR/bin/pip" install --upgrade pip
echo "==> Installing Python requirements (this includes PyTorch, may take a few minutes)..."
"$VENV_DIR/bin/pip" install -r "$PROJECT_DIR/requirements.txt"

# 3. .env check
if [ ! -f "$PROJECT_DIR/.env" ]; then
  echo "!! No .env file found. Copy .env.example to .env and edit it, then re-run."
  echo "   cp .env.example .env && nano .env"
  exit 1
fi
# shellcheck disable=SC1091
set -a; source "$PROJECT_DIR/.env"; set +a
PORT="${PORT:-8000}"

# 4. Django migrate
echo "==> Running migrations..."
"$VENV_DIR/bin/python" "$PROJECT_DIR/manage.py" migrate --noinput

# 5. systemd service
echo "==> Installing systemd service..."
cat > /etc/systemd/system/skinai.service <<EOF
[Unit]
Description=Skin-AI gunicorn server
After=network.target

[Service]
User=$SERVICE_USER
WorkingDirectory=$PROJECT_DIR
EnvironmentFile=$PROJECT_DIR/.env
ExecStart=$VENV_DIR/bin/gunicorn skinai.wsgi:application \\
    --bind 0.0.0.0:$PORT \\
    --workers 2 \\
    --threads 2 \\
    --timeout 120
Restart=always
RestartSec=3

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable skinai.service
systemctl restart skinai.service

# 6. Firewall: allow SSH + the app port
echo "==> Configuring firewall..."
ufw allow OpenSSH || true
ufw allow "${PORT}/tcp" || true
yes | ufw enable || true

echo ""
echo "==> Done. Service status:"
systemctl --no-pager status skinai.service | head -n 12 || true
echo ""
echo "==> Test from your machine:"
echo "    curl http://<SERVER_IP>:${PORT}/api/predict/   (should say 'Only POST')"
