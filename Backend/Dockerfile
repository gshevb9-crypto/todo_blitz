# Multi-stage Dockerfile for ToDo & Notes Web API (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for optimal Docker layer caching
COPY ["ToDoList.Domain/ToDoList.Domain.csproj", "ToDoList.Domain/"]
COPY ["ToDoList.Application/ToDoList.Application.csproj", "ToDoList.Application/"]
COPY ["ToDoList.Infrastructure/ToDoList.Infrastructure.csproj", "ToDoList.Infrastructure/"]
COPY ["ToDoList.API/ToDoList.API.csproj", "ToDoList.API/"]

RUN dotnet restore "ToDoList.API/ToDoList.API.csproj"

# Copy entire backend source
COPY . .

# Build and publish in Release mode
WORKDIR "/src/ToDoList.API"
RUN dotnet publish "ToDoList.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create persistent storage folder for uploaded user images
RUN mkdir -p /app/wwwroot/files/tabs

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS="http://+:8080"
ENV ASPNETCORE_ENVIRONMENT="Production"
ENV ASPNETCORE_HTTPS_PORT="443"

EXPOSE 8080
EXPOSE 80

ENTRYPOINT ["dotnet", "ToDoList.API.dll"]
