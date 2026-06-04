# Deploy VPS moi: DEV + MAIN

VPS hien tai:

- IP: `180.93.114.25`
- Hostname: `vps-tmnf6r`
- SSH: `ssh root@180.93.114.25`
- OS: `ubuntu-20.04_x86_64`

## 1) Branch trigger

- Push branch `dev` -> deploy environment `dev`
- Push branch `main` -> deploy environment `main`
- Manual run workflow -> deploy theo branch dang chon khi run workflow

## 2) Docker Hub

Workflow se build image va push cac tag:

- `<DOCKER_IMAGE>:dev-latest`
- `<DOCKER_IMAGE>:dev-<commit-sha>`
- `<DOCKER_IMAGE>:main-latest`
- `<DOCKER_IMAGE>:main-<commit-sha>`
- `<DOCKER_IMAGE>:latest` chi duoc push tu branch `main`

Bat buoc them Repository variable:

- `DOCKER_IMAGE=<dockerhub-username>/<image-name>`

Vi du:

- `DOCKER_IMAGE=myuser/devpool-backend`

## 3) GitHub Environments

Tao 2 environments trong repository:

- `dev`
- `main`

## 4) Repository secrets

Them secrets dung chung:

- `DOCKER_USERNAME`
- `DOCKER_PASSWORD`

Them Repository variable:

- `DOCKER_IMAGE`

## 5) Environment secrets

Them cac secret sau cho tung environment `dev` va `main`:

- `VPS_HOST=180.93.114.25`
- `VPS_PASSWORD=<mat-khau-root-cua-vps>`

Optional:

- `VPS_USER` mac dinh la `root`
- `VPS_PORT` mac dinh la `22`
- `VPS_SSH_KEY` neu muon deploy bang SSH key thay cho password

## 6) Environment variables

Co the bo qua, workflow da co default. Khuyen nghi set rieng cho tung environment:

- `APP_DIR`
- `STACK_NAME`
- `BACKEND_PORT`
- `POSTGRES_PORT`
  <<<<<<< Updated upstream
- # `PGADMIN_PORT`
- `POSTGRES_BIND_IP`
  > > > > > > > Stashed changes
- `ASPNETCORE_ENVIRONMENT`
- `DB_NAME`

Suggested values neu DEV va MAIN chung mot VPS:

DEV:

- `APP_DIR=/root/qls-backend-dev`
- `STACK_NAME=qls-dev`
- `BACKEND_PORT=5079`
- `POSTGRES_PORT=5433`
- `POSTGRES_BIND_IP=127.0.0.1`
- `ASPNETCORE_ENVIRONMENT=Development`
- `DB_NAME=QLS_DEV`

MAIN:

- `APP_DIR=/root/qls-backend`
- `STACK_NAME=qls-main`
- `BACKEND_PORT=5078`
- `POSTGRES_PORT=5432`
- `POSTGRES_BIND_IP=127.0.0.1`
- `ASPNETCORE_ENVIRONMENT=Production`
- `DB_NAME=QLS_PROD`

## 7) File tren VPS

Workflow se tu copy `docker-compose.yml` len VPS moi lan deploy.

Lenh tao nhanh:

```bash
mkdir -p /root/qls-backend /root/qls-backend-dev
```

Workflow se tao `.env` mac dinh neu chua co. Sau lan deploy dau tien, SSH vao VPS va sua `.env` theo tung moi truong:

```bash
nano /root/qls-backend/.env
nano /root/qls-backend-dev/.env
```

Toi thieu nen doi:

```bash
DB_USER=qls_user
DB_PASSWORD=change-this-password
JWT_KEY=change-this-long-secret-key
CORS_ORIGIN_0=https://your-frontend-domain.com
```

## 8) Setup VPS lan dau

SSH vao VPS:

```bash
ssh root@180.93.114.25
```

Chay:

```bash
apt-get update
apt-get install -y ca-certificates curl gnupg lsb-release
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" > /etc/apt/sources.list.d/docker.list
apt-get update
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl enable --now docker
mkdir -p /root/qls-backend /root/qls-backend-dev
```

Neu bat firewall tren VPS:

```bash
ufw allow 22/tcp
ufw allow 5078/tcp
ufw allow 5079/tcp
ufw --force enable
```

## 9) Chay thu

- Push len `dev` de test DEV
- Khi on dinh, merge/push `main` de deploy MAIN

## 10) Health check

Workflow se kiem tra:

- DEV: `http://180.93.114.25:5079/health`
- MAIN: `http://180.93.114.25:5078/health`

Neu dung Nginx reverse proxy, dam bao port `BACKEND_PORT` da accessible noi bo hoac public theo cach ban cau hinh.
