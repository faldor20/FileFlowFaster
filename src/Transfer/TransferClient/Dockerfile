FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source


COPY *.fsproj .
RUN dotnet restore


COPY . .
RUN dotnet publish -c release -o /app --no-restore


FROM mcr.microsoft.com/dotnet/runtime:5.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./TransferClient"]