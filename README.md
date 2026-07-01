# CSharpQuartzScheduler

Ejemplo profesional de automatización de tareas (rutinas) con [Quartz.NET](https://www.quartz-scheduler.net/) sobre **.NET 8**, usando el *Generic Host*, inyección de dependencias, configuración externa y logging estructurado.

## Características

- **.NET 8 Worker** (`Microsoft.NET.Sdk.Worker`) con `Host.CreateApplicationBuilder`.
- **Quartz.Extensions.Hosting**: el scheduler se registra vía DI y corre como *hosted service* con apagado ordenado (`WaitForJobsToComplete`).
- **Configuración externa** en `appsettings.json` (cron, nombre, grupo, zona horaria) con opciones fuertemente tipadas y validadas al arrancar.
- **Logging estructurado** con Serilog a consola y archivo (rotación diaria).
- **Ejecutable como servicio de Windows** sin cambios de código (`AddWindowsService`).
- Job desacoplado de la lógica de negocio (`IRoutineService`) para facilitar pruebas.
- `[DisallowConcurrentExecution]` para evitar solapamiento de ejecuciones.

## Estructura

```
CSharpQuartzScheduler/
├── Program.cs                     # Composición del host, DI y registro de Quartz
├── appsettings.json               # Cron y configuración de logging
├── Configuration/
│   └── RoutineJobOptions.cs        # Opciones tipadas (sección "RoutineJob")
├── Jobs/
│   └── RoutineJob.cs               # IJob: orquesta y maneja errores/cancelación
└── Services/
    ├── IRoutineService.cs          # Contrato de la lógica de negocio
    └── RoutineService.cs           # Implementación (aquí va tu trabajo real)
```

## Ejecución

```bash
dotnet run --project CSharpQuartzScheduler
```

Detén el proceso con `Ctrl+C`: espera a que los jobs en curso terminen antes de salir.

## Configuración

Edita la sección `RoutineJob` de `appsettings.json`:

| Clave            | Descripción                                                        |
|------------------|--------------------------------------------------------------------|
| `Name`           | Nombre lógico del job.                                              |
| `Group`          | Grupo de Quartz para job y trigger.                                 |
| `CronExpression` | Cron de Quartz (7 campos). `0 0/1 * 1/1 * ? *` = cada minuto.       |
| `TimeZone`       | Zona horaria (ID de Windows/IANA). Vacío = UTC.                     |
| `Enabled`        | `false` para no registrar el job.                                  |

> El cron de Quartz usa el formato `segundo minuto hora díaMes mes díaSemana [año]`, distinto del cron de Unix. Prueba expresiones en [freeformatter.com/cron-expression-generator-quartz](https://www.freeformatter.com/cron-expression-generator-quartz.html).

## Instalar como servicio de Windows

```powershell
dotnet publish CSharpQuartzScheduler -c Release -o C:\Scheduler
sc.exe create CSharpQuartzScheduler binPath= "C:\Scheduler\CSharpQuartzScheduler.exe" start= auto
sc.exe start CSharpQuartzScheduler
```

## Personalizar la rutina

Implementa tu lógica en `Services/RoutineService.RunAsync`. Para varias rutinas, crea más `IJob` + `AddTrigger` en `Program.cs`, cada uno con su propia sección de configuración.
