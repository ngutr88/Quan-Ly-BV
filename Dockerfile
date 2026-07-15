FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["QuanLyBenhVien.csproj", "."]
RUN dotnet restore "QuanLyBenhVien.csproj"
COPY . .
RUN dotnet publish "QuanLyBenhVien.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
RUN mkdir -p /app/data
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "QuanLyBenhVien.dll"]
