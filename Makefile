DOTNET ?= dotnet

.PHONY: build test run disasm bench embedding clean

build:
	$(DOTNET) build src/MoonByte.Cli/MoonByte.Cli.csproj -c Release

test:
	$(DOTNET) run --project tests/MoonByte.Tests/MoonByte.Tests.csproj -c Release

run:
	$(DOTNET) run --project src/MoonByte.Cli/MoonByte.Cli.csproj -c Release -- run examples/scripts/language_tour.mb

disasm:
	$(DOTNET) run --project src/MoonByte.Cli/MoonByte.Cli.csproj -c Release -- disasm examples/scripts/language_tour.mb

bench:
	$(DOTNET) run --project src/MoonByte.Cli/MoonByte.Cli.csproj -c Release -- bench examples/scripts/language_tour.mb 1000

embedding:
	$(DOTNET) run --project examples/EmbeddingDemo/EmbeddingDemo.csproj -c Release

clean:
	$(DOTNET) clean

