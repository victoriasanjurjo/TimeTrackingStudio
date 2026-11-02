# Time Tracking Studio ⏱️ (WinUI 3 & C#)

**Time Tracking Studio** es una aplicación de escritorio nativa, moderna y de alto rendimiento para **Windows 10 y Windows 11**, diseñada para profesionales independientes, desarrolladores, agencias y estudios que desean gestionar sus clientes, proyectos y seguimiento de tiempos trabajados **con privacidad absoluta (100% local)**, sin telemetría ni suscripciones en la nube.

La aplicación está construida íntegramente en **C# y .NET 8**, utilizando la interfaz gráfica nativa **WinUI 3 (Windows App SDK)** con controles **Fluent Design**, arquitectura **MVVM (Model-View-ViewModel)** mediante **CommunityToolkit.Mvvm**, persistencia relacional con **SQLite** y empaquetado nativo en formato **MSIX**.

---

## 🌟 Características Principales

- **🔒 100% Local y Privada:** Sin llamadas de red, sin publicidad, sin rastreadores ni telemetría. La base de datos SQLite reside en tu propio equipo.
- **⏱ Cronómetro Global en Vivo:** Barra superior persistente con selector dinámico de cliente y trabajo, display digital de precisión (`00:00:00`), botones de iniciar, pausar, reanudar, descartar y guardar sesión trabajada.
- **👥 Gestión de Clientes:** Fichas de clientes con datos de contacto (empresa, email, teléfono), tarifas por hora configurables, divisa (`USD`, `EUR`, `ARS`, `MXN`, `GBP`) y cálculo automático de facturación acumulada.
- **📁 Control Total de Trabajos y Tareas:** Registro de tareas con estados oficiales:
  - 🟡 `Pendiente`
  - 🟠 `En progreso`
  - 🔵 `En revisión`
  - 🟢 `Entregado`
  - 🟣 `Facturado`
  - Prioridades (`Baja`, `Media`, `Alta`, `Urgente`), cálculo automático de horas estimadas vs registradas y barras de progreso.
- **📊 Panel de Control (Dashboard):** Tarjetas de KPIs en vivo (Horas Hoy, Total Facturable Hoy, Horas Mes, Trabajos Activos) y visualizador dinámico del rendimiento en los últimos 7 días.
- **⏱ Hoja de Tiempos (Timesheet):** Registro histórico detallado con auditoría de sesiones, adición manual de horas trabajadas y eliminación de entradas.
- **📄 Reportes Ejecutivos y Exportación (`outputs/`):** Generador de informes maquetados en **HTML** con diseño formal y exportación completa a formato **CSV**.
- **📦 Empaquetado Nativo MSIX (.msix):** Listo para su instalación y distribución en Windows con integración directa al sistema operativo.

---

## 🏛 Arquitectura y Componentes del Sistema

El software sigue estrictamente el patrón arquitectónico **MVVM (Model-View-ViewModel)**:

```text
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

1. **Capa de Persistencia y Negocio (`TimeTrackingStudio.WinUI/Services/`):**
   - [`DatabaseService.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Services/DatabaseService.cs): Gestor SQLite (`Microsoft.Data.Sqlite`) con claves foráneas activas y semilla de datos demostrativa.
   - [`ClientService.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Services/ClientService.cs): Operaciones CRUD de clientes y cálculo de ingresos.
   - [`JobService.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Services/JobService.cs): Lógica de estados, prioridades y horas estimadas.
   - [`TimeService.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Services/TimeService.cs): Cronómetro en memoria coordinado con el `DispatcherQueue` de WinUI.
   - [`ExportService.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Services/ExportService.cs): Exportación a CSV y generación de informes ejecutivos en HTML dentro de `outputs/`.

2. **Capa de ViewModels (`TimeTrackingStudio.WinUI/ViewModels/`):**
   - [`MainViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/MainViewModel.cs): Orquesta el cronómetro persistente global.
   - [`DashboardViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/DashboardViewModel.cs): Métricas y analíticas.
   - [`ClientsViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/ClientsViewModel.cs): Directorio y búsqueda de clientes.
   - [`JobsViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/JobsViewModel.cs): Seguimiento de proyectos y estados.
   - [`TimesheetViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/TimesheetViewModel.cs): Hoja de tiempos y entradas manuales.
   - [`ReportsViewModel.cs`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/ViewModels/ReportsViewModel.cs): Exportación y resumen ejecutivo.

3. **Capa de Vistas XAML (`TimeTrackingStudio.WinUI/Views/`):**
   - [`MainWindow.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/MainWindow.xaml): Ventana principal con barra de cronómetro superior y `NavigationView`.
   - [`DashboardPage.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/DashboardPage.xaml): Panel de control con tarjetas KPI y gráfico de barras semanal.
   - [`ClientsPage.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/ClientsPage.xaml) / [`ClientDialog.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/ClientDialog.xaml): Directorio modal de clientes.
   - [`JobsPage.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/JobsPage.xaml) / [`JobDialog.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/JobDialog.xaml): Gestión modal de tareas.
   - [`TimesheetPage.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/TimesheetPage.xaml) / [`TimeEntryDialog.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/TimeEntryDialog.xaml): Hoja de tiempos y registro manual.
   - [`ReportsPage.xaml`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/TimeTrackingStudio.WinUI/Views/ReportsPage.xaml): Generación de informes.

---

## 📁 Estructura del Directorio

```text
TimeTrackingStudio/
├── assets/                          # Iconos oficiales (.ico, .png)
├── config/                          # Archivo de base de datos local SQLite
├── dist/                            # Paquete de instalación (.msix)
├── docs/                            # Documentación técnica y arquitectura
│   └── arquitectura_winui3.md
├── outputs/                         # Salida de informes HTML y CSV
├── tests/                           # Suite de pruebas unitarias (.NET)
│   └── TimeTrackingStudio.Tests/
├── TimeTrackingStudio.WinUI/        # Código fuente de la aplicación WinUI 3
│   ├── Assets/                      # Recursos visuales del paquete MSIX
│   ├── Converters/                  # Conversores XAML de estilo
│   ├── Models/                      # Entidades de dominio
│   ├── Services/                    # Servicios de negocio y persistencia
│   ├── ViewModels/                  # ViewModels MVVM
│   ├── Views/                       # Páginas y Diálogos XAML
│   ├── App.xaml / .xaml.cs          # Ciclo de vida de la aplicación
│   ├── MainWindow.xaml / .cs        # Ventana principal
│   ├── Package.appxmanifest         # Manifiesto MSIX
│   └── TimeTrackingStudio.WinUI.csproj
├── .gitignore
├── LICENSE.txt
└── README.md
```

---

## 🚀 Guía de Instalación y Ejecución

### Prerrequisitos
- **Windows 10 versión 1809 (build 17763) o superior / Windows 11**.
- **.NET 8 SDK** instalado.

### 1. Ejecutar en modo desarrollo
Para compilar y lanzar la aplicación directamente en modo desarrollo:
```powershell
dotnet run --project TimeTrackingStudio.WinUI\TimeTrackingStudio.WinUI.csproj -p:Platform=x64
```

### 2. Ejecutar la Suite de Pruebas Unitarias
Para ejecutar las pruebas automáticas de modelos y cálculos:
```powershell
dotnet test tests\TimeTrackingStudio.Tests\TimeTrackingStudio.Tests.csproj
```

### 3. Compilar el Paquete MSIX (.msix) para Distribución
Para generar el paquete de instalación atómico `.msix`:
```powershell
dotnet publish TimeTrackingStudio.WinUI\TimeTrackingStudio.WinUI.csproj -c Release -p:Platform=x64 -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false
```

El instalador MSIX queda disponible en:
```text
dist/TimeTrackingStudio_v1.0.0_x64.msix
```

---

## 📄 Licencia

Este proyecto está bajo la Licencia **MIT**. Consulta el archivo [`LICENSE.txt`](file:///d:/Proyectos/Repos/Nueva%20carpeta%20%282%29/LICENSE.txt) para más detalles.
