#!/bin/bash
# =============================================================================
# QLS Backend - Oracle Cloud VM Setup Script
# OS: Ubuntu 22.04 / 24.04
# Usage: chmod +x setup-server.sh && sudo bash setup-server.sh
# =============================================================================

set -e

GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
echo_green()  { echo -e "${GREEN}✅ $1${NC}"; }
echo_yellow() { echo -e "${YELLOW}⚙️  $1${NC}"; }

# ─── CẤU HÌNH ──────────────────────────────────────────────────────────────
DB_NAME="QLS"
DB_USER="qls_user"
DB_PASSWORD="QLS@Oracle2026!"   # ← Đổi password này trước khi chạy
APP_USER="qls"
APP_DIR="/opt/qls-backend"
DOTNET_PORT=5078

echo "════════════════════════════════════════"
echo "   QLS Backend - Oracle Cloud Setup     "
echo "════════════════════════════════════════"

# ─── 1. CẬP NHẬT HỆ THỐNG ──────────────────────────────────────────────────
echo_yellow "1/5 Cập nhật hệ thống..."
apt-get update -y && apt-get upgrade -y
echo_green "Xong!"

# ─── 2. CÀI ĐẶT POSTGRESQL ─────────────────────────────────────────────────
echo_yellow "2/5 Cài đặt PostgreSQL..."
apt-get install -y postgresql postgresql-contrib
systemctl enable postgresql
systemctl start postgresql

sudo -u postgres psql <<EOF
DO \$\$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${DB_USER}') THEN
    CREATE USER ${DB_USER} WITH PASSWORD '${DB_PASSWORD}';
  END IF;
END \$\$;
SELECT 'CREATE DATABASE "${DB_NAME}" OWNER ${DB_USER}'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '${DB_NAME}')\gexec
GRANT ALL PRIVILEGES ON DATABASE "${DB_NAME}" TO ${DB_USER};
ALTER USER ${DB_USER} CREATEDB;
EOF
echo_green "PostgreSQL + DB '${DB_NAME}' xong!"

# ─── 3. CÀI ĐẶT .NET 10 ────────────────────────────────────────────────────
echo_yellow "3/5 Cài đặt .NET 10..."
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O /tmp/ms-prod.deb
dpkg -i /tmp/ms-prod.deb
rm /tmp/ms-prod.deb
apt-get update -y
apt-get install -y aspnetcore-runtime-10.0 dotnet-sdk-10.0
echo_green ".NET $(dotnet --version) xong!"

# ─── 4. TẠO USER & THƯ MỤC APP ─────────────────────────────────────────────
echo_yellow "4/5 Tạo user hệ thống và thư mục..."
id -u "${APP_USER}" &>/dev/null || useradd -r -s /bin/false -d ${APP_DIR} ${APP_USER}
mkdir -p ${APP_DIR}
chown -R ${APP_USER}:${APP_USER} ${APP_DIR}

# Tạo appsettings.Production.json trên server
cat > ${APP_DIR}/appsettings.Production.json <<APPSETTINGS
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Jwt": {
    "Key": "QLaundromatSystem123456789@@2026!",
    "Issuer": "QLS_Backend",
    "Audience": "QLS_Web",
    "ExpireDays": 7
  },
  "CorsSettings": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "https://your-frontend-domain.com"
    ]
  },
  "Zigbee2Mqtt": {
    "Host": "localhost",
    "Port": "1883"
  },
  "LgApi": {
    "BaseUrl": "https://kic-laundry.lgthinq.com/status/",
    "ApiKey": "vV6bStCpqr5Hqxbcr8Kmp9XkFh4VdlVp568YxBp5",
    "AppVer": "0.1",
    "ClientType": "USER",
    "CountryCode": "VN",
    "ClientId": "12345",
    "ServiceCode": "CHN000035",
    "ServicePhase": "OP"
  },
  "AllowedHosts": "*"
}
APPSETTINGS
chown ${APP_USER}:${APP_USER} ${APP_DIR}/appsettings.Production.json
chmod 600 ${APP_DIR}/appsettings.Production.json
echo_green "User & thư mục xong!"

# ─── 5. TẠO SYSTEMD SERVICE ─────────────────────────────────────────────────
echo_yellow "5/5 Tạo systemd service..."
cat > /etc/systemd/system/qls-backend.service <<SERVICE
[Unit]
Description=QLS Backend API (.NET 10)
After=network.target postgresql.service
Wants=postgresql.service

[Service]
Type=simple
User=${APP_USER}
WorkingDirectory=${APP_DIR}
ExecStart=/usr/bin/dotnet ${APP_DIR}/QLS.Backend.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=qls-backend
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:${DOTNET_PORT}
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
SERVICE

systemctl daemon-reload
systemctl enable qls-backend
echo_green "Service 'qls-backend' đã tạo!"

# ─── MỞ FIREWALL ────────────────────────────────────────────────────────────
iptables -I INPUT 6 -m state --state NEW -p tcp --dport ${DOTNET_PORT} -j ACCEPT
netfilter-persistent save 2>/dev/null || true

echo ""
echo "════════════════════════════════════════"
echo_green " SETUP HOÀN THÀNH!"
echo "════════════════════════════════════════"
echo ""
echo "  DB Host:     localhost:5432"
echo "  DB Name:     ${DB_NAME}"
echo "  DB User:     ${DB_USER}"
echo "  DB Password: ${DB_PASSWORD}"
echo "  App Dir:     ${APP_DIR}"
echo "  App Port:    ${DOTNET_PORT}"
echo ""
echo "  Quản lý service:"
echo "  sudo systemctl start|stop|restart|status qls-backend"
echo "  sudo journalctl -u qls-backend -f"
echo ""
echo "⚠️  Nhớ mở port ${DOTNET_PORT} trong Oracle Cloud Security List!"
