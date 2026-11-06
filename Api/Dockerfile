FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Api/Api.csproj", "Api/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Shared/Shared.Kernel/Shared.Kernel.csproj", "Shared/Shared.Kernel/"]
COPY ["Shared/Shared.Application/Shared.Application.csproj", "Shared/Shared.Application/"]
COPY ["Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
COPY ["Shared/Shared.Api/Shared.Api.csproj", "Shared/Shared.Api/"]
COPY ["Modules/Crm/Crm.Contracts/Crm.Contracts.csproj", "Modules/Crm/Crm.Contracts/"]
COPY ["Modules/Crm/Crm.Domain/Crm.Domain.csproj", "Modules/Crm/Crm.Domain/"]
COPY ["Modules/Crm/Crm.Application/Crm.Application.csproj", "Modules/Crm/Crm.Application/"]
COPY ["Modules/Crm/Crm.Infrastructure/Crm.Infrastructure.csproj", "Modules/Crm/Crm.Infrastructure/"]
COPY ["Modules/Crm/Crm.Api/Crm.Api.csproj", "Modules/Crm/Crm.Api/"]
COPY ["Modules/Scheduling/Scheduling.Contracts/Scheduling.Contracts.csproj", "Modules/Scheduling/Scheduling.Contracts/"]
COPY ["Modules/Scheduling/Scheduling.Domain/Scheduling.Domain.csproj", "Modules/Scheduling/Scheduling.Domain/"]
COPY ["Modules/Scheduling/Scheduling.Application/Scheduling.Application.csproj", "Modules/Scheduling/Scheduling.Application/"]
COPY ["Modules/Scheduling/Scheduling.Infrastructure/Scheduling.Infrastructure.csproj", "Modules/Scheduling/Scheduling.Infrastructure/"]
COPY ["Modules/Scheduling/Scheduling.Api/Scheduling.Api.csproj", "Modules/Scheduling/Scheduling.Api/"]
COPY ["Modules/Identity/Identity.Contracts/Identity.Contracts.csproj", "Modules/Identity/Identity.Contracts/"]
COPY ["Modules/Identity/Identity.Domain/Identity.Domain.csproj", "Modules/Identity/Identity.Domain/"]
COPY ["Modules/Identity/Identity.Application/Identity.Application.csproj", "Modules/Identity/Identity.Application/"]
COPY ["Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj", "Modules/Identity/Identity.Infrastructure/"]
COPY ["Modules/Identity/Identity.Api/Identity.Api.csproj", "Modules/Identity/Identity.Api/"]
COPY ["Modules/Notifications/Notifications.Contracts/Notifications.Contracts.csproj", "Modules/Notifications/Notifications.Contracts/"]
COPY ["Modules/Notifications/Notifications.Infrastructure/Notifications.Infrastructure.csproj", "Modules/Notifications/Notifications.Infrastructure/"]
COPY ["Modules/Notifications/Notifications.Api/Notifications.Api.csproj", "Modules/Notifications/Notifications.Api/"]
COPY ["Modules/Inventory/Inventory.Domain/Inventory.Domain.csproj", "Modules/Inventory/Inventory.Domain/"]
COPY ["Modules/Inventory/Inventory.Application/Inventory.Application.csproj", "Modules/Inventory/Inventory.Application/"]
COPY ["Modules/Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj", "Modules/Inventory/Inventory.Infrastructure/"]
COPY ["Modules/Inventory/Inventory.Api/Inventory.Api.csproj", "Modules/Inventory/Inventory.Api/"]
COPY ["Modules/Marketplace/Marketplace.Domain/Marketplace.Domain.csproj", "Modules/Marketplace/Marketplace.Domain/"]
COPY ["Modules/Marketplace/Marketplace.Application/Marketplace.Application.csproj", "Modules/Marketplace/Marketplace.Application/"]
COPY ["Modules/Marketplace/Marketplace.Infrastructure/Marketplace.Infrastructure.csproj", "Modules/Marketplace/Marketplace.Infrastructure/"]
COPY ["Modules/Marketplace/Marketplace.Api/Marketplace.Api.csproj", "Modules/Marketplace/Marketplace.Api/"]
RUN dotnet restore "Api/Api.csproj"

COPY . .
WORKDIR /src/Api
RUN dotnet publish "Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Time-zone data: studios keep their own zone (e.g. Europe/Warsaw), and the base image has none
RUN apk add --no-cache tzdata

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Api.dll"]