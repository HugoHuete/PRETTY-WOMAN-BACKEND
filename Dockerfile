FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/PrettyWoman.Api/PrettyWoman.Api.csproj", "src/PrettyWoman.Api/"]
COPY ["src/PrettyWoman.Application/PrettyWoman.Application.csproj", "src/PrettyWoman.Application/"]
COPY ["src/PrettyWoman.Domain/PrettyWoman.Domain.csproj", "src/PrettyWoman.Domain/"]
COPY ["src/PrettyWoman.Infrastructure/PrettyWoman.Infrastructure.csproj", "src/PrettyWoman.Infrastructure/"]

RUN dotnet restore "src/PrettyWoman.Api/PrettyWoman.Api.csproj" --runtime linux-x64

COPY . .

RUN dotnet publish "src/PrettyWoman.Api/PrettyWoman.Api.csproj" \
    --configuration Release \
    --no-restore \
    --runtime linux-x64 \
    --output /app/publish

FROM build AS migrations

RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.8

RUN /tools/dotnet-ef migrations bundle \
    --project "src/PrettyWoman.Infrastructure/PrettyWoman.Infrastructure.csproj" \
    --startup-project "src/PrettyWoman.Api/PrettyWoman.Api.csproj" \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained \
    --output /app/migrations/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .
COPY --from=migrations /app/migrations/efbundle ./efbundle

RUN chmod +x ./efbundle

EXPOSE 8080

ENTRYPOINT ["dotnet", "PrettyWoman.Api.dll"]
