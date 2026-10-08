# Day 58: multi-stage build. The SDK image (large) compiles; only the small ASP.NET runtime image is shipped.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# copy the project file first so the restore layer is cached until dependencies change
COPY LearnAPI.csproj ./
RUN dotnet restore LearnAPI.csproj
COPY . .
RUN dotnet publish LearnAPI.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
# run as the unprivileged user that the aspnet image ships with (never as root)
RUN mkdir -p /var/learninsurance/storage && chown -R app:app /var/learninsurance
COPY --from=build /app/publish .
USER app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__Root=/var/learninsurance/storage
EXPOSE 8080
# (GET /health is the liveness endpoint; the aspnet image has no curl, so probe it from the orchestrator / nginx instead)
ENTRYPOINT ["dotnet", "LearnAPI.dll"]
