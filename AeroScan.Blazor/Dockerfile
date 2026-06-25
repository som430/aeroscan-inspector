FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AeroScan.Blazor/AeroScan.Blazor.csproj", "AeroScan.Blazor/"]
COPY ["AeroScan.API/AeroScan.API.csproj", "AeroScan.API/"]
COPY ["AeroScan.PointCloud/AeroScan.PointCloud.csproj", "AeroScan.PointCloud/"]
RUN dotnet restore "AeroScan.Blazor/AeroScan.Blazor.csproj"
COPY . .
RUN dotnet publish "AeroScan.Blazor/AeroScan.Blazor.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
