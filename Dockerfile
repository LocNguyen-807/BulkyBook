# Build ứng dụng bằng .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy toàn bộ source, bao gồm các project được tham chiếu
COPY . .

# Publish project web
RUN dotnet publish "BulkyBookWeb/BulkyBookWeb.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Chạy ứng dụng bằng ASP.NET Core Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "BulkyBookWeb.dll"]