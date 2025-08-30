FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["MotorbikeConsumer/MotorbikeConsumer.csproj", "MotorbikeConsumer/"]
RUN dotnet restore "MotorbikeConsumer/MotorbikeConsumer.csproj"
COPY . .
WORKDIR "/src/MotorbikeConsumer"
RUN dotnet build "MotorbikeConsumer.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "MotorbikeConsumer.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MotorbikeConsumer.dll"]