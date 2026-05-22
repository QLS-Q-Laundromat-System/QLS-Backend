# Deploy 2 Server: DEV + MAIN

## 1) Branch trigger
- Push branch `dev` or `develop` -> deploy DEV server
- Push branch `main` -> deploy MAIN server
- Manual run workflow: choose `dev`, `main`, or `both`

## 2) GitHub Environments
Tao 2 environments trong repository:
- `dev`
- `main`

## 3) Secrets cho moi environment
Them cac secret sau cho tung environment (gia tri khac nhau theo server):
- `ORACLE_HOST`
- `ORACLE_USER`
- `ORACLE_SSH_KEY`

## 4) Variables cho moi environment (khuyen nghi)
Co the bo qua, workflow da co default. Neu set thi them:
- `APP_DIR`
- `STACK_NAME`
- `BACKEND_PORT`
- `POSTGRES_PORT`
- `ASPNETCORE_ENVIRONMENT`

Suggested values:

DEV:
- `APP_DIR=/home/ubuntu/qls-backend-dev`
- `STACK_NAME=qls-dev`
- `BACKEND_PORT=5079`
- `POSTGRES_PORT=5433`
- `ASPNETCORE_ENVIRONMENT=Development`

MAIN:
- `APP_DIR=/home/ubuntu/qls-backend`
- `STACK_NAME=qls-main`
- `BACKEND_PORT=5078`
- `POSTGRES_PORT=5432`
- `ASPNETCORE_ENVIRONMENT=Production`

## 5) File `.env` tren moi server
Tai `APP_DIR`, tao file `.env` (co the copy tu `.env.example`) voi DB/JWT/CORS theo tung moi truong.

## 6) Chay thu
- Push len `dev` de test tren DEV
- Khi on dinh, merge/push `main` de deploy MAIN
