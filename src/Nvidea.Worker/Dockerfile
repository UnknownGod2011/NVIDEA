FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Nvidea.Core/Nvidea.Core.csproj src/Nvidea.Core/
COPY src/Nvidea.Worker/Nvidea.Worker.csproj src/Nvidea.Worker/
RUN dotnet restore src/Nvidea.Worker/Nvidea.Worker.csproj
COPY src/Nvidea.Core/ src/Nvidea.Core/
COPY src/Nvidea.Worker/ src/Nvidea.Worker/
RUN dotnet publish src/Nvidea.Worker/Nvidea.Worker.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
USER 65532:65532
ENTRYPOINT ["dotnet", "Nvidea.Worker.dll"]
