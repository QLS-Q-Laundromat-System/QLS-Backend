# Ket noi pgAdmin voi PostgreSQL tren VPS

> Cau hinh 1 lan duy nhat, sau do pgAdmin se ket noi qua SSH Tunnel den PostgreSQL dang chay trong Docker.

## Thong tin VPS

| | |
|---|---|
| Server IP | `180.93.114.25` |
| SSH User | `root` |
| SSH Command | `ssh root@180.93.114.25` |
| MAIN DB | `QLS_PROD` |
| DEV DB | `QLS_DEV` |
| DB User | `qls_user` |
| DB Password | Xem trong file `.env` tren VPS |

## MAIN database

### Tab General

| Truong | Gia tri |
|---|---|
| Name | `QLS VPS MAIN` |

### Tab Connection

| Truong | Gia tri |
|---|---|
| Host name/address | `localhost` |
| Port | `5432` |
| Maintenance database | `QLS_PROD` |
| Username | `qls_user` |
| Password | Gia tri `DB_PASSWORD` trong `/root/qls-backend/.env` |
| Save password | Tick vao neu muon |

### Tab SSH Tunnel

| Truong | Gia tri |
|---|---|
| Use SSH tunneling | Bat ON |
| Tunnel host | `180.93.114.25` |
| Tunnel port | `22` |
| Username | `root` |
| Authentication | Password hoac Identity file |

## DEV database

Tao them server khac trong pgAdmin:

| Truong | Gia tri |
|---|---|
| Name | `QLS VPS DEV` |
| Host name/address | `localhost` |
| Port | `5433` |
| Maintenance database | `QLS_DEV` |
| Username | `qls_user` |
| Password | Gia tri `DB_PASSWORD` trong `/root/qls-backend-dev/.env` |
| Tunnel host | `180.93.114.25` |
| Tunnel username | `root` |

## Ghi chu

- PostgreSQL duoc bind vao `127.0.0.1`, nen khong mo truc tiep port database ra internet.
- Ket noi tu pgAdmin nen di qua SSH Tunnel.
- Sau deploy lan dau, doi `DB_PASSWORD` va `JWT_KEY` trong `.env`, roi chay lai:

```bash
cd /root/qls-backend
docker compose up -d

cd /root/qls-backend-dev
docker compose up -d
```
