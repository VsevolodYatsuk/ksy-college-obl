# Используем базовый образ для ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Копируем все необходимые файлы в контейнер
COPY ["api-college.server/api-college.server.dll", "/app/api-college.server.dll"]
COPY ["api-college.server/api-college.server.runtimeconfig.json", "/app/api-college.server.runtimeconfig.json"]
COPY ["api-college.server/Swashbuckle.AspNetCore.SwaggerGen.dll", "/app/Swashbuckle.AspNetCore.SwaggerGen.dll"]
COPY ["api-college.server/Microsoft.AspNetCore.OpenApi.dll", "/app/Microsoft.AspNetCore.OpenApi.dll"]
COPY ["api-college.server/Npgsql.dll", "/app/Npgsql.dll"]

# Копируем все остальные dll из корневой папки бэкенда в контейнер
COPY api-college.server/*.dll /app/

RUN ls -la /app

ENTRYPOINT ["dotnet", "/app/api-college.server.dll"]