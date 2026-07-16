FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["QuanLyBenhVien.csproj", "."]
RUN dotnet restore "QuanLyBenhVien.csproj"
COPY . .
RUN dotnet publish "QuanLyBenhVien.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
# Render containers have a low inotify watcher limit; polling keeps config reloads reliable.
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
RUN mkdir -p /app/data
COPY --from=build /app/publish .
# Start production with the same checked-in SQLite snapshot used locally.
# On Render Free this snapshot is restored whenever a fresh container is created.
COPY --from=build /src/hms.db /app/data/hms.db
ENTRYPOINT ["dotnet", "QuanLyBenhVien.dll"]
