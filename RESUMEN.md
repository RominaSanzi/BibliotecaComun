# Resumen - BibliotecaComun (POC)

## Repositorio
https://github.com/RominaSanzi/BibliotecaComun (privado)

## Feed NuGet (GitHub Packages)
https://nuget.pkg.github.com/RominaSanzi/index.json

## Paquete publicado
- `Divinf.Common.Sql` versión `0.1.0-poc.1`

## PAT (guardar en lugar seguro)
- Generado en https://github.com/settings/tokens (classic)
- Scopes: `repo`, `workflow`, `read:packages`, `write:packages`

## CI/CD (GitHub Actions)
- Archivo: `.github/workflows/pack.yml`
- Se dispara automáticamente en cada push a `main`
- Pasos: Build → Test (4 tests) → Pack → Push a GitHub Packages
- Último resultado: ✅ Success

## Cómo consumir el paquete

### 1. Configurar el feed
En `NuGet.config` del proyecto consumidor:
```xml
<add key="github" value="https://nuget.pkg.github.com/RominaSanzi/index.json" />
```

### 2. Autenticar
```powershell
dotnet nuget update source "github" `
  --source "https://nuget.pkg.github.com/RominaSanzi/index.json" `
  --username "RominaSanzi" `
  --password "<TU_PAT>" `
  --store-password-in-clear-text
```

### 3. Agregar referencia
```xml
<PackageReference Include="Divinf.Common.Sql" Version="0.1.0-poc.1" />
```

### 4. Usar
```vb
Imports Divinf.Common.Sql
Dim g = New GeneradorSql(New SqlOptions With { .BaseDeDatos = "MYSQL", .Region = "ARGENTINA" })
```

## Smoke test local
```powershell
dotnet run --project smoketest\Divinf.Common.SmokeTest
```
Esperado: 5/5 [OK] (SELECT, JOIN, INSERT, UPDATE, DELETE)

## Pasos futuros
- Fase 1: Wrapper + feature flag en Loan
- Fase 2: Activar en DEV/STAGING
- Fase 3: Migrar feed a Gitea
- Fase 4: Adopción en Collection
