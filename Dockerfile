# ─── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj và restore (tận dụng layer cache)
COPY *.csproj ./
RUN dotnet restore

# Copy toàn bộ source và publish
COPY . ./
RUN dotnet publish QLS.Backend.csproj -c Release -o /app/publish --no-restore

# ─── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy từ stage build
COPY --from=build /app/publish .

# Port app lắng nghe
ENV ASPNETCORE_URLS=http://+:5078
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5078

ENTRYPOINT ["dotnet", "QLS.Backend.dll"]
