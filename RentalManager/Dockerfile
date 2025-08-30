FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5289
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:5289

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["RentalManager/RentalManager.csproj", "RentalManager/"]
RUN dotnet restore "RentalManager/RentalManager.csproj"
COPY . .
WORKDIR "/src/RentalManager"
RUN dotnet build "RentalManager.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "RentalManager.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RentalManager.dll"]