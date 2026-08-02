# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Central package management needs these two files present before any csproj restores.
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]

# The Api project pulls in Domain/Persistence/SharedKernel via ProjectReference, so restore needs
# all four csproj files up front — copying just the Api one (as the single-project template did)
# fails outright once project references exist.
COPY ["src/VerticalSliceArchitecture.Api/VerticalSliceArchitecture.Api.csproj", "src/VerticalSliceArchitecture.Api/"]
COPY ["src/VerticalSliceArchitecture.Domain/VerticalSliceArchitecture.Domain.csproj", "src/VerticalSliceArchitecture.Domain/"]
COPY ["src/VerticalSliceArchitecture.Persistence/VerticalSliceArchitecture.Persistence.csproj", "src/VerticalSliceArchitecture.Persistence/"]
COPY ["src/VerticalSliceArchitecture.SharedKernel/VerticalSliceArchitecture.SharedKernel.csproj", "src/VerticalSliceArchitecture.SharedKernel/"]
RUN dotnet restore "./src/VerticalSliceArchitecture.Api/VerticalSliceArchitecture.Api.csproj"
COPY . .
WORKDIR "/src/src/VerticalSliceArchitecture.Api"
RUN dotnet build "./VerticalSliceArchitecture.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VerticalSliceArchitecture.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VerticalSliceArchitecture.Api.dll"]