.DEFAULT_GOAL := back

# Entornos restringidos (terminal del IDE) dejan NuGet en ".nuget/packages" relativo y bloquean
# la lectura de ~/.gitconfig: se fija la cache global y se desactiva la consulta de git del SDK.
export NUGET_PACKAGES := $(HOME)/.nuget/packages
export EnableSourceControlManagerQueries := false
export DOTNET_CLI_TELEMETRY_OPTOUT := 1

.PHONY: back api build run mongo mongo-down mongo-logs

# Levanta MongoDB en Docker (PostgreSQL es externo) y luego la API con hot reload
back: mongo
	dotnet watch --project Ate.Api

api: back

# Solo MongoDB (idempotente: si ya esta corriendo no hace nada)
mongo:
	docker compose up -d --wait mongo

mongo-down:
	docker compose stop mongo

mongo-logs:
	docker compose logs -f mongo

# Variantes sin Docker
run:
	dotnet run --project Ate.Api

build:
	dotnet build Ate.Api/Ate.Api.csproj
