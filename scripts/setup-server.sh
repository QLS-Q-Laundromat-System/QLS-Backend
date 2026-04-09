#!/bin/bash
# =============================================================================
# QLS Backend - Oracle Cloud VM Setup Script (Docker version)
# OS: Ubuntu 22.04 / 24.04
# Usage: chmod +x setup-server.sh && sudo bash setup-server.sh
# =============================================================================

set -e

GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
echo_green()  { echo -e "${GREEN}✅ $1${NC}"; }
echo_yellow() { echo -e "${YELLOW}⚙️  $1${NC}"; }
echo_red()    { echo -e "${RED}❌ $1${NC}"; }

APP_USER="ubuntu"
APP_DIR="/home/ubuntu/qls-backend"
DOTNET_PORT=5078

echo "════════════════════════════════════════"
echo "   QLS Backend - Docker Setup           "
echo "════════════════════════════════════════"

# ─── 1. CẬP NHẬT HỆ THỐNG ──────────────────────────────────────────────────
echo_yellow "1/4 Cập nhật hệ thống..."
apt-get update -y && apt-get upgrade -y
apt-get install -y curl git ca-certificates gnupg
echo_green "Xong!"

# ─── 2. CÀI ĐẶT DOCKER ─────────────────────────────────────────────────────
echo_yellow "2/4 Cài đặt Docker..."

if command -v docker &>/dev/null; then
  echo_green "Docker đã được cài! Version: $(docker --version)"
else
  # Thêm Docker GPG key
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  chmod a+r /etc/apt/keyrings/docker.gpg

  # Thêm Docker repo
  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
    https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
    | tee /etc/apt/sources.list.d/docker.list > /dev/null

  apt-get update -y
  apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

  # Thêm user ubuntu vào group docker (không cần sudo)
  usermod -aG docker ubuntu

  systemctl enable docker
  systemctl start docker
  echo_green "Docker $(docker --version) đã cài xong!"
fi

# ─── 3. TẠO THƯ MỤC APP & FILE .env ────────────────────────────────────────
echo_yellow "3/4 Tạo thư mục và cấu hình..."
mkdir -p ${APP_DIR}

# Tạo file .env nếu chưa có
if [ ! -f "${APP_DIR}/.env" ]; then
  cat > ${APP_DIR}/.env <<'ENV'
# QLS Backend - Production Environment
# ⚠️ Đổi các giá trị này trước khi chạy!

# Database
DB_NAME=QLS
DB_USER=qls_user
DB_PASSWORD=QLS@Oracle2026!

# JWT Secret (đổi key này thành chuỗi ngẫu nhiên dài hơn!)
JWT_KEY=QLaundromatSystem123456789@@2026!

# CORS - thêm domain frontend thật
CORS_ORIGIN_0=http://localhost:5173
CORS_ORIGIN_1=https://your-frontend-domain.com

# MQTT
MQTT_HOST=localhost
MQTT_PORT=1883
ENV
  chmod 600 ${APP_DIR}/.env
  echo_green "Tạo file .env tại ${APP_DIR}/.env"
else
  echo_green "File .env đã tồn tại, giữ nguyên."
fi

chown -R ubuntu:ubuntu ${APP_DIR}
echo_green "Thư mục ${APP_DIR} xong!"

# ─── 4. MỞ FIREWALL ─────────────────────────────────────────────────────────
echo_yellow "4/4 Mở firewall port ${DOTNET_PORT}..."
iptables -I INPUT 6 -m state --state NEW -p tcp --dport ${DOTNET_PORT} -j ACCEPT
apt-get install -y iptables-persistent 2>/dev/null || true
netfilter-persistent save 2>/dev/null || true
echo_green "Port ${DOTNET_PORT} đã mở!"

echo ""
echo "════════════════════════════════════════"
echo_green " SETUP HOÀN THÀNH!"
echo "════════════════════════════════════════"
echo ""
echo "  App Dir:  ${APP_DIR}"
echo "  App Port: ${DOTNET_PORT}"
echo ""
echo "  📝 Nhớ chỉnh file .env:"
echo "     nano ${APP_DIR}/.env"
echo ""
echo "  🔑 Câu lệnh quản lý:"
echo "  cd ${APP_DIR}"
echo "  docker compose ps           # xem status"
echo "  docker compose logs -f      # xem logs"
echo "  docker compose down         # dừng"
echo "  docker compose up -d        # chạy lại"
echo ""
echo "⚠️  Nhớ mở port ${DOTNET_PORT} trong Oracle Cloud Security List!"
echo "⚠️  QUAN TRỌNG: Logout và login lại để áp dụng group docker!"
