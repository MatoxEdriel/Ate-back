.DEFAULT_GOAL := back

.PHONY: back api build run

back:
	dotnet watch --project Ate.Api

api: back

run:
	dotnet run --project Ate.Api

build:
	dotnet build Ate.Api/Ate.Api.csproj
