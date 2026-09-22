# CSharpQuartzScheduler

Ejemplo profesional de automatización de tareas (rutinas) con [Quartz.NET](https://www.quartz-scheduler.net/) 4 sobre **.NET 10**, usando el *Generic Host*, inyección de dependencias, configuración externa y logging estructurado.

Proyecto hermano en Java: [JavaQuartzScheduler](https://github.com/carlosalbertoxw/JavaQuartzScheduler).

## Características

- **.NET 10 Worker** (`Microsoft.NET.Sdk.Worker`) con `Host.CreateApplicationBuilder`.
- **Quartz.NET 4**: el scheduler se registra vía DI y corre como *hosted service* con apagado ordenado (`WaitForJobsToComplete`).
- **Varias rutinas desde configuración**: cada entrada del arreglo `Routines` de `appsettings.json` crea un job y un trigger.
- **Validación al arrancar**: cron, zona horaria, servicio registrado e identidad única; un error de configuración detiene el host con un mensaje claro.
- **Logging estructurado** con Serilog a consola y archivo (rotación diaria, junto al ejecutable).
- **Ejecutable como servicio de Windows** sin cambios de código (`AddWindowsService`).
- Job desacoplado de la lógica de negocio (`IRoutineService` como *keyed service*) para facilitar pruebas.
- `[DisallowConcurrentExecution]` para evitar solapamiento de ejecuciones de una misma rutina.
- Disparos perdidos (servicio detenido) se ignoran: se espera al siguiente disparo programado.
- Pruebas unitarias con xUnit y CI en GitHub Actions.

## Requisitos

- [SDK de .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).
- Windows para instalarlo como servicio. Como aplicación de consola corre también en Linux y macOS.

## Estructura

```
.
├── CSharpQuartzScheduler/
│   ├── Program.cs                          # Composición del host, DI y registro de Quartz
│   ├── appsettings.json                    # Rutinas, reportes y logging
│   ├── appsettings.Development.json        # Logging en nivel Debug para desarrollo
│   ├── Properties/
│   │   └── launchSettings.json             # `dotnet run` arranca en entorno Development
│   ├── Configuration/
│   │   ├── RoutineOptions.cs               # Opciones tipadas (arreglo "Routines")
│   │   ├── RoutinesOptionsValidator.cs     # Validación de cron, zona horaria, servicio, etc.
│   │   └── ReportsOptions.cs               # Opciones de las rutinas de reportes (sección "Reports")
│   ├── Jobs/
│   │   └── RoutineJob.cs                   # IJob: resuelve el servicio y maneja errores/cancelación
│   └── Services/
│       ├── IRoutineService.cs              # Contrato de la lógica de negocio
│       ├── RoutineService.cs               # Rutina mínima de ejemplo ("Example")
│       ├── DailyReportService.cs           # Genera un reporte de texto ("DailyReport")
│       └── ReportCleanupService.cs         # Borra reportes antiguos ("ReportCleanup")
├── CSharpQuartzScheduler.Tests/            # Pruebas unitarias (xUnit + NSubstitute)
├── .github/workflows/build.yml             # CI: compila y ejecuta las pruebas
└── .editorconfig                           # Estilo de código
```

## Cómo funciona

```
appsettings.json ("Routines")
        │  una entrada por rutina
        ▼
Program.cs ── valida opciones ──► registra en Quartz: JobKey (Group.Name) + trigger cron
        │                          (el JobDataMap guarda la clave "Service")
        ▼
Quartz dispara ──► RoutineJob ──► resuelve IRoutineService por su clave ──► RunAsync()
```

1. Al arrancar, `RoutinesOptionsValidator` revisa todas las rutinas (incluso las deshabilitadas). Si algo está mal, el host no inicia y el log muestra todos los errores juntos.
2. Por cada rutina con `Enabled: true` se registra un job de tipo `RoutineJob` y un trigger cron en la zona horaria configurada.
3. En cada disparo, Quartz crea un scope de DI, instancia `RoutineJob` y este resuelve el servicio indicado en `Service`.

### Manejo de errores y apagado

| Situación                          | Comportamiento                                                                                  |
|------------------------------------|-------------------------------------------------------------------------------------------------|
| La rutina lanza una excepción      | Se registra una sola vez como `Error` y se relanza como `JobExecutionException`, sin reintento inmediato: el siguiente disparo del cron vuelve a intentarlo. |
| Una ejecución sigue en curso al llegar el siguiente disparo | `[DisallowConcurrentExecution]` evita el solapamiento: el disparo se retrasa hasta que termine la ejecución actual y, si se retrasa más del umbral de *misfire* de Quartz (60 s), se omite. |
| El servicio estuvo detenido        | Los disparos perdidos se ignoran (`DoNothing`); la rutina corre en su siguiente horario.        |
| `Ctrl+C` o detener el servicio     | Se cancela el `CancellationToken` y se espera a que las rutinas en curso terminen.              |

## Ejecución

```bash
dotnet run --project CSharpQuartzScheduler
```

Detén el proceso con `Ctrl+C`: espera a que los jobs en curso terminen antes de salir.

`dotnet run` usa `Properties/launchSettings.json`, que activa el entorno `Development` y con él `appsettings.Development.json` (logs en nivel `Debug`).

## Rutinas incluidas

| Rutina          | Servicio (clave)                         | Cuándo corre                                        | Qué hace                                                        |
|-----------------|------------------------------------------|-----------------------------------------------------|-----------------------------------------------------------------|
| `RoutineJob`    | `RoutineService` (`Example`)             | Cada minuto (UTC)                                   | Ejemplo mínimo: registra inicio y fin.                          |
| `DailyReport`   | `DailyReportService` (`DailyReport`)     | Lunes a viernes a las 7:00, `America/Mexico_City`   | Escribe `reports/report-<fecha>.txt` con un resumen del equipo. |
| `ReportCleanup` | `ReportCleanupService` (`ReportCleanup`) | Todos los días a las 3:30 (UTC)                     | Borra los `report-*.txt` con más de `RetentionDays` días.       |

Las dos rutinas de reportes muestran cómo un servicio recibe su propia configuración (`IOptions<ReportsOptions>`) y otras dependencias (`TimeProvider`, que permite fijar la hora en las pruebas). La limpieza solo borra archivos con el prefijo `report-`; cualquier otro archivo de la carpeta se conserva.

Para verlas correr sin esperar, sobrescribe el cron desde la línea de comandos (el índice es la posición en el arreglo `Routines`):

```bash
dotnet run --project CSharpQuartzScheduler -- --Routines:1:CronExpression "0/5 * * ? * *" --Routines:2:CronExpression "2/5 * * ? * *"
```

## Configuración

Cualquier valor puede sobrescribirse con variables de entorno (`Routines__0__Enabled=false`) o argumentos de línea de comandos (`--Routines:0:Enabled false`).

### `Routines`

Cada elemento del arreglo es una rutina:

| Clave            | Descripción                                                              |
|------------------|--------------------------------------------------------------------------|
| `Name`           | Nombre lógico del job (único dentro de su grupo). Obligatorio.           |
| `Group`          | Grupo de Quartz para job y trigger. Por defecto `Routines`.              |
| `Service`        | Clave del `IRoutineService` registrado con `AddKeyedScoped`. Obligatorio. |
| `CronExpression` | Cron de Quartz (6-7 campos). `0 * * ? * *` = cada minuto. Obligatorio.   |
| `TimeZone`       | Zona horaria (ID de Windows o IANA). Vacío = UTC.                        |
| `Enabled`        | `false` para no programar la rutina. Por defecto `true`.                 |

> El cron de Quartz usa el formato `segundo minuto hora díaMes mes díaSemana [año]`, distinto del cron de Unix. Prueba expresiones en [freeformatter.com/cron-expression-generator-quartz](https://www.freeformatter.com/cron-expression-generator-quartz.html).

Ejemplos:

| Expresión             | Significado                              |
|-----------------------|------------------------------------------|
| `0 * * ? * *`         | Cada minuto                              |
| `0 0/15 * ? * *`      | Cada 15 minutos                          |
| `0 0 7 ? * MON-FRI`   | Lunes a viernes a las 7:00               |
| `0 30 3 * * ?`        | Todos los días a las 3:30                |
| `0 0 9 1 * ?`         | El día 1 de cada mes a las 9:00          |

### `Reports`

Configura las rutinas de reportes:

| Clave           | Descripción                                                     |
|-----------------|-----------------------------------------------------------------|
| `Directory`     | Carpeta de reportes. Relativa = junto al ejecutable.            |
| `RetentionDays` | Días que se conservan los reportes antes de borrarlos (1-3650). |

### `Serilog`

- Los logs se escriben en consola y en `logs/scheduler-<fecha>.log` junto al ejecutable (también al correr como servicio), con rotación diaria y 14 archivos de retención.
- El filtro `ByExcluding` descarta el mensaje duplicado de Quartz (`JobRunShell`) cuando una rutina falla, porque `RoutineJob` ya registra ese error con más contexto.

## Agregar una rutina

1. Crea una clase que implemente `IRoutineService` con tu lógica en `RunAsync` y una constante `Key` (ver `DailyReportService` como guía):
   ```csharp
   public sealed class MiRutinaService : IRoutineService
   {
       public const string Key = "MiRutina";

       public async Task RunAsync(CancellationToken cancellationToken)
       {
           // Tu lógica aquí; pasa el cancellationToken a las llamadas asíncronas.
       }
   }
   ```
2. Regístrala con su clave en `Program.cs`:
   ```csharp
   builder.Services.AddKeyedScoped<IRoutineService, MiRutinaService>(MiRutinaService.Key);
   ```
3. Agrega una entrada en `Routines`:
   ```json
   {
     "Name": "MiRutina",
     "Group": "Routines",
     "Service": "MiRutina",
     "CronExpression": "0 0 7 ? * MON-FRI",
     "TimeZone": "America/Mexico_City"
   }
   ```
4. Agrega pruebas en `CSharpQuartzScheduler.Tests` (ver `ReportServicesTests` como guía).

Si la clave de `Service` no coincide con ningún servicio registrado, la aplicación no arranca e indica cuál falta.

## Pruebas

```bash
dotnet test
```

| Archivo                            | Qué cubre                                                                  |
|------------------------------------|----------------------------------------------------------------------------|
| `RoutineJobTests.cs`               | Resolución del servicio, errores, cancelación y clave faltante.            |
| `RoutinesOptionsValidatorTests.cs` | Cron, zona horaria, servicio no registrado, duplicados y UTC por defecto.  |
| `ReportServicesTests.cs`           | Generación de reportes y limpieza por antigüedad.                          |

El workflow `.github/workflows/build.yml` compila y ejecuta las pruebas en cada push a `master` y en cada pull request.

## Instalar como servicio de Windows

En una consola de PowerShell como administrador:

```powershell
dotnet publish CSharpQuartzScheduler -c Release -o C:\Scheduler
sc.exe create CSharpQuartzScheduler binPath= "C:\Scheduler\CSharpQuartzScheduler.exe" start= auto
sc.exe start CSharpQuartzScheduler
```

Los logs y reportes quedan en `C:\Scheduler\logs` y `C:\Scheduler\reports`.

Para detenerlo o desinstalarlo:

```powershell
sc.exe stop CSharpQuartzScheduler
sc.exe delete CSharpQuartzScheduler
```

## Migración desde la versión anterior (.NET 8 / Quartz 3)

- La sección `RoutineJob` de `appsettings.json` se reemplazó por el arreglo `Routines`; cada entrada necesita además el campo `Service`.
- `TimeZone` vacío ahora significa UTC de verdad (antes Quartz usaba la hora local del equipo).
- `IJob.Execute` en Quartz 4 devuelve `ValueTask` y recibe el `CancellationToken` como parámetro.
- El paquete `Quartz.Extensions.Hosting` ya no se usa: en Quartz 4 todo está en el paquete `Quartz`.
