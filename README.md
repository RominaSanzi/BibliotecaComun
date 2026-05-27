# BibliotecaComun

Libreria privada compartida entre Loan y Collection.
Distribuida como paquete NuGet privado via **GitHub Packages** (POC); luego migrara a Gitea Package Registry.

> La solucion se llama `BibliotecaComun.sln`; los paquetes publicados al feed conservan el prefijo `Divinf.Common.*` (ej. `Divinf.Common.Sql`).

## Estado actual

**Fase 0 (POC)** — primer modulo: `Divinf.Common.Sql` (port de `GeneradorSql` desde Loan/Utils).

## Estructura

```
Divinf.Common/                          (carpeta local)
├─ src/
│  └─ Divinf.Common.Sql/                ← libreria principal (netstandard2.0)
├─ tests/
│  └─ Divinf.Common.Sql.Tests/          ← tests de paridad contra Loan.Utils.dll
└─ smoketest/
   └─ Divinf.Common.SmokeTest/          ← consola que valida consumo via paquete
```

## Como compilar localmente

```powershell
cd C:\Users\Usuarioi7\source\repos\Divinf.Common
dotnet restore
dotnet build -c Release
dotnet test
```

## Como empaquetar y publicar al feed GitHub Packages (POC)

El push al feed ocurre **automaticamente** desde GitHub Actions (`.github/workflows/pack.yml`) en cada push a `main`. Para publicar manual:

```powershell
# 1. Bumpear version en Directory.Build.props si corresponde
# 2. Pack
dotnet pack src\Divinf.Common.Sql\Divinf.Common.Sql.csproj -c Release -o .\nupkgs

# 3. Push al feed privado (requiere NuGet.local.config con PAT — ver template)
dotnet nuget push .\nupkgs\Divinf.Common.Sql.0.1.0-poc.1.nupkg --source "github" --api-key "<github-pat>"
```

## Como consumir desde Loan o Collection

1. Crear `NuGet.config` en la raiz del repo consumidor con el feed GitHub:
   ```xml
   <add key="github" value="https://nuget.pkg.github.com/RominaSanzi/index.json" />
   ```
2. Crear `NuGet.local.config` (gitignored) con un PAT que tenga `read:packages` — usar `NuGet.local.config.template` de este repo como referencia.
3. Agregar al proyecto consumidor:

   **Proyecto SDK-style (csproj/vbproj nuevo):**
   ```xml
   <PackageReference Include="Divinf.Common.Sql" Version="0.1.0-poc.1" />
   ```

   **Proyecto VB clasico (packages.config):**
   Tools → NuGet Package Manager → fuente "github" → instalar `Divinf.Common.Sql`. Eso genera la entrada en `packages.config` y la `<Reference>` con `<HintPath>` en el `.vbproj`.

4. En el codigo VB: `Imports Divinf.Common.Sql`. La API es camelCase (compatible con los call sites legacy de Loan).

## Migracion futura a Gitea

Cuando el ciclo este validado en GitHub:
1. Crear repo en Gitea + registrar runner Windows self-hosted.
2. Crear PAT Gitea con `write:package` + agregarlo como secret `GITEA_PACKAGE_TOKEN`.
3. Reactivar `.gitea/workflows/pack.yml` (queda archivado en el repo).
4. Cambiar URL del feed en `NuGet.config` y en `.vbproj` de Loan.
5. `git remote set-url origin https://<gitea-host>/<org>/BibliotecaComun.git && git push`.

Codigo y tests no cambian — solo URLs y credenciales.

## Documentacion

Toda la documentacion ejecutiva y tecnica esta en:
`C:\Users\Usuarioi7\Documents\ProyectoLibrerias\Anotaciones\`

- `Presentacion-Divinf-Common.html` — presentacion ejecutiva
- `README-Handoff.md` — guia tecnica completa para retomar el proyecto
- `Prueba-en-Loan1-PasoAPaso.md` — procedimiento de validacion dentro de Loan

## Roadmap

| Fase | Descripcion | Status |
|---|---|---|
| 0 | POC `Divinf.Common.Sql` publicado en GitHub Packages | En curso |
| 1 | Wrapper + feature flag en Loan | Pendiente |
| 2 | Activacion en DEV/STAGING | Pendiente |
| 3 | Migrar feed a Gitea | Pendiente |
| 4 | Adopcion en Collection | Pendiente |
| 5 | Eliminar GeneradorSql.vb legacy | Pendiente |
| 6 | Siguiente modulo (Files / Email / Excel / Pdf / DataAccess) | Pendiente |
