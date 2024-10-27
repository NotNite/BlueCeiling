FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-alpine-aot AS build
ARG TARGETARCH
WORKDIR /source

COPY . .
RUN dotnet restore -r linux-musl-$TARGETARCH
RUN dotnet publish -r linux-musl-$TARGETARCH --no-restore -o /app BlueCeiling

FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-alpine-aot
WORKDIR /app
COPY --link --from=build /app .
USER $APP_UID
ENTRYPOINT ["./BlueCeiling"]
