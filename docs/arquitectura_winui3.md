# Arquitectura Técnica - Time Tracking Studio (WinUI 3 & C#)

## 1. Visión General del Sistema

**Time Tracking Studio (WinUI 3)** es una aplicación de escritorio de alto rendimiento para Windows 10 y Windows 11 desarrollada en **C# y .NET 8**, empleando la interfaz gráfica nativa **WinUI 3** (Windows App SDK), el patrón de arquitectura **MVVM (Model-View-ViewModel)** mediante **CommunityToolkit.Mvvm** y empaquetado moderno **MSIX**.

La solución mantiene la filosofía fundacional: **100% local, privada, sin llamadas a servicios de terceros ni rastreadores**, con persistencia local relacional en **SQLite**.

---

## 2. Patrón de Arquitectura: MVVM

El proyecto está desacoplado siguiendo estrictamente las mejores prácticas de la arquitectura MVVM:

```
┌────────────────────────────────────────────────────────┐
│                      CAPA DE VISTAS                    │
│    MainWindow.xaml, DashboardPage.xaml, ClientsPage... │
│         (XAML Controls, Converters, NavigationView)    │
└───────────────────────────▲────────────────────────────┘
                            │ Data Binding (x:Bind)
┌───────────────────────────▼────────────────────────────┐
│                    CAPA VIEWMODEL                      │
│   MainViewModel, DashboardViewModel, ClientsViewModel  │
│      (CommunityToolkit.Mvvm: ObservableObject, Relay)   │
└───────────────────────────▲────────────────────────────┘
                            │ Llamadas asíncronas / eventos
┌───────────────────────────▼────────────────────────────┐
│                   CAPA DE SERVICIOS                    │
│    DatabaseService, ClientService, JobService, Time... │
└───────────────────────────▲────────────────────────────┘
                            │ Consultas SQL seguras
┌───────────────────────────▼────────────────────────────┐
│                  CAPA DE PERSISTENCIA                  │
│       SQLite Local (%LocalAppData%/TimeTrackingStudio) │
└────────────────────────────────────────────────────────┘
```

### Componentes de la Arquitectura:

1. **Modelos (`TimeTrackingStudio.WinUI/Models/`):**
   - [`Client.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Models/Client.cs): Entidad cliente (contacto, tarifa horaria, divisa, métricas acumuladas).
   - [`Job.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Models/Job.cs): Tarea o proyecto asignado con estados (`Pendiente`, `En progreso`, `En revisión`, `Entregado`, `Facturado`) y prioridades.
   - [`TimeLog.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Models/TimeLog.cs): Registro de tiempo con cálculo automático de horas decimales e importe facturable.
   - [`DashboardMetrics.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Models/DashboardMetrics.cs): Estructuras agregadas para KPIs y gráfico diario de 7 días.

2. **Servicios (`TimeTrackingStudio.WinUI/Services/`):**
   - [`DatabaseService.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Services/DatabaseService.cs): Manejo de conexión SQLite (`Microsoft.Data.Sqlite`), inicialización de esquemas DDL con foreign keys e inserción de datos semilla en el primer arranque.
   - [`ClientService.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Services/ClientService.cs): Operaciones CRUD y cómputo de facturación por cliente.
   - [`JobService.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Services/JobService.cs): Filtrado por estado, asignación de tareas y cálculo de progreso.
   - [`TimeService.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Services/TimeService.cs): Cronómetro en memoria coordinado con el `DispatcherQueue` de WinUI, soporte de pausa, reanudación, descarte y persistencia determinista.
   - [`ExportService.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Services/ExportService.cs): Exportación a archivos CSV estructurados e informes ejecutivos maquetados en HTML dentro de `outputs/`.

3. **ViewModels (`TimeTrackingStudio.WinUI/ViewModels/`):**
   - Utilizan los atributos generadores de código de `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`) garantizando cero boilerplate y máxima velocidad de ejecución en tiempo de compilación.
   - [`MainViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/MainViewModel.cs): Estado del cronómetro en la barra superior persistente.
   - [`DashboardViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/DashboardViewModel.cs): Actualización de métricas en tiempo real.
   - [`ClientsViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/ClientsViewModel.cs): Filtrado reactivo y gestión de clientes.
   - [`JobsViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/JobsViewModel.cs): Gestión de ciclo de vida de tareas.
   - [`TimesheetViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/TimesheetViewModel.cs): Auditoría histórica y adición manual de horas.
   - [`ReportsViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/ReportsViewModel.cs): Orquestación de reportes y notificaciones.

4. **Vistas XAML (`TimeTrackingStudio.WinUI/Views/`):**
   - [`MainWindow.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/MainWindow.xaml): Ventana principal con barra superior de cronometraje en vivo y menú de navegación Fluent `NavigationView`.
   - [`DashboardPage.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/DashboardPage.xaml): Panel de control con tarjetas KPI y gráfico de barras semanal dinámico.
   - [`ClientsPage.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/ClientsPage.xaml): Directorio con filtro de búsqueda instantánea.
   - [`JobsPage.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/JobsPage.xaml): Tabla de tareas con selector directo de estados de entrega e indicadores de progreso.
   - [`TimesheetPage.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/TimesheetPage.xaml): Vista cronológica y botón de registro manual.
   - [`ReportsPage.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/ReportsPage.xaml): Interfaz de exportación y generación de informes ejecutivos.

---

## 3. Justificación de Decisiones Técnicas

| Decisión Técnica | Alternativa Evaluada | Justificación |
|---|---|---|
| **WinUI 3 (Windows App SDK)** | WPF / WinForms / UWP | WinUI 3 es el estándar moderno de Microsoft para interfaces nativas en Windows 11 y 10, con soporte directo para controles Fluent Design, aceleración por hardware y modo oscuro/claro nativo. |
| **CommunityToolkit.Mvvm** | Prism / MVVMCross / INotifyPropertyChanged manual | Proporciona source generators basados en Roslyn que reducen el código repetitivo en un 80% y maximizan el rendimiento en tiempo de ejecución. |
| **Microsoft.Data.Sqlite** | Entity Framework Core | SQLite ligero y directo sin overhead de ORM pesado para mantener arranques instantáneos (< 200 ms) y mínima huella de memoria. |
| **Empaquetado MSIX** | Instalador InnoSetup / MSI tradicional | MSIX garantiza instalaciones atómicas, desinstalaciones limpias sin residuos en el registro, sandbox seguro y preparación inmediata para distribución empresarial o Microsoft Store. |

---

## 4. Compilación y Generación del Paquete MSIX

Para compilar la aplicación y generar el paquete `.msix` ejecutable para distribución:

```powershell
dotnet publish TimeTrackingStudio.WinUI\TimeTrackingStudio.WinUI.csproj -c Release -p:Platform=x64 -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false
```

El paquete resultante se genera en:
`dist/TimeTrackingStudio_v1.0.0_x64.msix`
