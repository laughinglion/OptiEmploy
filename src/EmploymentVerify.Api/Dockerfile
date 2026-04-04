FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/EmploymentVerify.Domain/EmploymentVerify.Domain.csproj src/EmploymentVerify.Domain/
COPY src/EmploymentVerify.Application/EmploymentVerify.Application.csproj src/EmploymentVerify.Application/
COPY src/EmploymentVerify.Infrastructure/EmploymentVerify.Infrastructure.csproj src/EmploymentVerify.Infrastructure/
COPY src/EmploymentVerify.Api/EmploymentVerify.Api.csproj src/EmploymentVerify.Api/

RUN dotnet restore src/EmploymentVerify.Api/EmploymentVerify.Api.csproj

COPY src/EmploymentVerify.Domain/ src/EmploymentVerify.Domain/
COPY src/EmploymentVerify.Application/ src/EmploymentVerify.Application/
COPY src/EmploymentVerify.Infrastructure/ src/EmploymentVerify.Infrastructure/
COPY src/EmploymentVerify.Api/ src/EmploymentVerify.Api/

RUN dotnet publish src/EmploymentVerify.Api/EmploymentVerify.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EmploymentVerify.Api.dll"]
