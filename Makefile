DOTNET ?= dotnet
EF ?= dotnet ef

API_PROJECT := src/PrettyWoman.Api
WORKERS_PROJECT := src/PrettyWoman.Workers
INFRA_PROJECT := src/PrettyWoman.Infrastructure
STARTUP_PROJECT := src/PrettyWoman.Api

.PHONY: api api-https workers build test restore migrate migration pending-model-changes

api:
	$(DOTNET) run --project $(API_PROJECT) --launch-profile http

api-https:
	$(DOTNET) run --project $(API_PROJECT) --launch-profile https

workers:
	$(DOTNET) run --project $(WORKERS_PROJECT)

restore:
	$(DOTNET) restore

build:
	$(DOTNET) build

test:
	$(DOTNET) test

migrate:
	$(EF) database update --project $(INFRA_PROJECT) --startup-project $(STARTUP_PROJECT)

pending-model-changes:
	$(EF) migrations has-pending-model-changes --project $(INFRA_PROJECT) --startup-project $(STARTUP_PROJECT)

migration:
ifndef name
	$(error Debes enviar el nombre: make migration name=NombreDeLaMigracion)
endif
	$(EF) migrations add $(name) --project $(INFRA_PROJECT) --startup-project $(STARTUP_PROJECT)
