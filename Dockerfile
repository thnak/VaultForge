FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM ubuntu:20.04


RUN apt-get update

# Extract VERSION_ID and use it in the URL
RUN VERSION_ID=$(grep VERSION_ID /etc/os-release | cut -d '=' -f 2 | tr -d '"') && \
    echo "Ubuntu version: $VERSION_ID" && \
    curl -sSL -O https://packages.microsoft.com/config/ubuntu/$VERSION_ID/packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update

RUN rm packages-microsoft-prod.deb

# Install dependencies (e.g., libmsquic)
RUN apt-get install -y libmsquic


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebApp/WebApp.csproj", "WebApp/"]
COPY ["WebApp.Client/WebApp.Client.csproj", "WebApp.Client/"]
COPY ["Business/Business.csproj", "Business/"]
COPY ["Protector/Protector.csproj", "Protector/"]


RUN dotnet restore "WebApp/WebApp.csproj"
RUN dotnet workload restore "WebApp/WebApp.csproj"
COPY . .
WORKDIR "/src/WebApp"
RUN dotnet build "WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WebApp.csproj" -r linux-x64 --self-contained true -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApp.dll"]
