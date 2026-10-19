# Multi-stage Dockerfile for ToDo & Notes Web API (.NET 8)
# Designed for Blitz.cloud PaaS direct deployment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files from Backend directory for optimal Docker layer caching
COPY ["Backend/ToDoList.Domain/ToDoList.Domain.csproj", "Backend/ToDoList.Domain/"]
COPY ["Backend/ToDoList.Application/ToDoList.Application.csproj", "Backend/ToDoList.Application/"]
COPY ["Backend/ToDoList.Infrastructure/ToDoList.Infrastructure.csproj", "Backend/ToDoList.Infrastructure/"]
COPY ["Backend/ToDoList.API/ToDoList.API.csproj", "Backend/ToDoList.API/"]

RUN dotnet restore "Backend/ToDoList.API/ToDoList.API.csproj"

# Copy entire backend source code
COPY Backend/ .

# Build and publish in Release mode
WORKDIR "/src/Backend/ToDoList.API"
RUN dotnet publish "ToDoList.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for container health check
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create persistent storage folder for uploaded user images
RUN mkdir -p /app/wwwroot/files/tabs

COPY --from=build /app/publish .

# Default configuration for Blitz.cloud
ENV ASPNETCORE_URLS="http://+:8080"
ENV ASPNETCORE_ENVIRONMENT="Production"
ENV ASPNETCORE_HTTPS_PORT="443"

# Blitz.cloud listens on port 8080 and sets PORT environment variable
EXPOSE 8080
EXPOSE 80

ENTRYPOINT ["dotnet", "ToDoList.API.dll"]
