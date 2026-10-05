FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["PSFtoEXCEL/PSFtoEXCEL.csproj", "PSFtoEXCEL/"]

RUN dotnet restore "PSFtoEXCEL/PSFtoEXCEL.csproj"

COPY . .

WORKDIR "/src/PSFtoEXCEL"

RUN dotnet publish "PSFtoEXCEL.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "PSFtoEXCEL.dll"]