# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только .csproj — слой restore кэшируется, пока не меняются зависимости.
COPY src/API/API.csproj                       src/API/
COPY src/Application/Application.csproj        src/Application/
COPY src/Domain/Domain.csproj                 src/Domain/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
RUN dotnet restore src/API/API.csproj

# Затем остальной исходный код и публикация.
COPY . .
RUN dotnet publish src/API/API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Непривилегированный пользователь (входит в образ aspnet).
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "API.dll"]
