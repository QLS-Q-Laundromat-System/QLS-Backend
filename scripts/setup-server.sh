#!/usr/bin/env bash
# QLS Backend - VPS Docker setup
# OS: Ubuntu 20.04+
# Usage: bash scripts/setup-server.sh

set -euo pipefail

APP_MAIN_DIR="/root/qls-backend"
APP_DEV_DIR="/root/qls-backend-dev"
MAIN_PORT=5078
DEV_PORT=5079

echo "QLS Backend - VPS Docker setup"

apt-get update
apt-get install -y ca-certificates curl gnupg lsb-release

install -m 0755 -d /etc/apt/keyrings
if [ ! -f /etc/apt/keyrings/docker.gpg ]; then
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
fi
chmod a+r /etc/apt/keyrings/docker.gpg

echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" > /etc/apt/sources.list.d/docker.list

apt-get update
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl enable --now docker

mkdir -p "$APP_MAIN_DIR" "$APP_DEV_DIR"

if command -v ufw >/dev/null 2>&1; then
  ufw allow 22/tcp
  ufw allow "${MAIN_PORT}/tcp"
  ufw allow "${DEV_PORT}/tcp"
fi

cat <<EOF

Setup complete.

VPS:
  IP: 180.93.114.25
  SSH: ssh root@180.93.114.25

App directories:
  MAIN: ${APP_MAIN_DIR}
  DEV:  ${APP_DEV_DIR}

Ports:
  MAIN: ${MAIN_PORT}
  DEV:  ${DEV_PORT}

Next:
  1. Set GitHub secrets DOCKER_USERNAME, DOCKER_PASSWORD.
  2. Set GitHub variable DOCKER_IMAGE, for example: your-dockerhub-user/qls-backend.
  3. Set environment secrets VPS_PASSWORD, optional VPS_HOST=180.93.114.25.
  4. Push branch dev or main.
EOF

docker --version
docker compose version
