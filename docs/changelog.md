# Bitácora de Desarrollo — Time Tracking Studio

Registro de decisiones técnicas, hitos completados y cambios arquitectónicos implementados.

---

## [Versión 1.0.0] - 2026-11-01

### Inicialización y Entorno
- Creación de entorno virtual `.venv` con CPython 3.12.
- Instalación de librerías base: `PySide6`, `PyInstaller`, `pandas`, `numpy`, `matplotlib`, `seaborn`, `reportlab`.
- Generación y bloqueo de dependencias exactas en `requirements.txt`.
- Adición de `LICENSE.txt` (Licencia MIT) y `.gitignore` con exclusiones de entornos, caches y ejecutables.

### Identidad Visual y Recursos
- Implementación de `src/utils/icon_generator.py` utilizando Pillow para generar el icono representativo del cronómetro con paleta clara zafiro/esmeralda.
- Generación de `assets/app_icon.png` (512x512) y `assets/app_icon.ico` (multi-resolución para Windows).

### Base de Datos y Persistencia
- Implementación de `src/db/database.py` utilizando SQLite con soporte de Foreign Keys, índices de aceleración y context manager de cierre determinista de conexiones.
- Implementación de dataclasses en `src/db/models.py` (`Client`, `Job`, `TimeLog`).
- Módulo `src/db/seed_data.py` con datos de prueba realistas para pruebas inmediatas.

### Lógica de Negocio y Reportes
- Implementación de `ClientService`: operaciones CRUD y cómputo de métricas por cliente.
- Implementación de `JobService`: administración de estados de entrega (`Pendiente`, `En progreso`, `En revisión`, `Entregado`, `Facturado`) y prioridades.
- Implementación de `TimeService`: cronómetro activo en memoria con inicio, pausa, reanudación y guardado, más KPIs de dashboard y desgloses de horas.
- Implementación de `ReportService`: generación de informes ejecutivos en formato PDF en `outputs/` con tablas detalladas y gráficos estadísticos Matplotlib/Seaborn.

### Interfaz Gráfica (PySide6)
- Creación de hoja de estilos en modo claro `LIGHT_QSS` en `src/ui/theme.py`.
- Componentes personalizados: `TimerBar`, `MetricCard`, `StatusBadge`, `PriorityBadge`.
- Formularios modales: `ClientDialog`, `JobDialog`, `TimeEntryDialog`, `ExportReportDialog`.
- Vistas completas: `DashboardView`, `ClientsView`, `JobsView`, `TimesheetView`, `ReportsView`.
- Coordinador `MainWindow` con barra lateral y sincronización reactiva entre vistas.

### Pruebas Unitarias y Validación
- Batería de tests en `tests/test_services.py` con 100% de éxito en CRUD de clientes, estados de trabajos, registro de tiempos y generación de PDFs.

### Refinamiento de Dimensiones y Ergonomía Visual (v1.0.1)
- **Barra de Cronómetro Global (`TimerBar`):** Altura uniforme de 34px en todos los controles (combos, inputs, reloj digital y botones) y políticas de tamaño flexibles para evitar compresiones en resoluciones estándar.
- **Tablas de Datos:**
  - `JobsView`: anchos explícitos para columnas fijas y 260px para acciones, con altura de fila de 54px para acomodar el selector rápido de estado, botones e información de dos líneas en títulos sin recortes.
  - `ClientsView`: altura de fila de 48px y 160px para la columna de acciones (Editar y Eliminar con espacio suficiente).
  - `TimesheetView`: altura de fila de 46px y anchos proporcionales en fecha, cliente, trabajo, duración y totales.
  - `ReportsView`: altura de fila de 44px en la tabla de PDFs y anchos optimizados para nombres de archivo y acciones.
- **Tarjetas de Métricas (`MetricCard`):** Altura mínima de 104px con tipografía de 23px y márgenes generosos para evitar colapsos.
- **Formularios Modales:** Campos de texto, combos y datepickers con altura de 34px, anchos mínimos entre 480px y 520px y espaciado de 12-18px.
- **Barra Lateral (`Sidebar`):** Ancho ajustado a 240px con botones de navegación de 40px de altura y tipografía de 13.5px.
- **Recompilación PyInstaller:** Generación del ejecutable actualizado en `dist/TimeTrackingStudio/TimeTrackingStudio.exe`.

---

## [Versión 2.0.0] - 2026-11-01

### Migración Completa a C# WinUI 3 y Eliminación de Python
- **Remoción del Stack Python:** Eliminación completa del entorno virtual `.venv`, `requirements.txt`, punto de entrada `main.py`, spec de PyInstaller `TimeTrackingStudio.spec` y código heredado en `src/`.
- **Arquitectura MVVM Nativa:** Implementación en C# (.NET 8) y WinUI 3 con `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
- **Persistencia en C#:** Implementación de `DatabaseService`, `ClientService`, `JobService`, `TimeService` y `ExportService` utilizando `Microsoft.Data.Sqlite`.
- **Interfaz XAML Fluent:** `MainWindow.xaml`, `DashboardPage.xaml`, `ClientsPage.xaml`, `JobsPage.xaml`, `TimesheetPage.xaml`, `ReportsPage.xaml` y diálogos modales.
- **Empaquetado MSIX:** Configuración de `Package.appxmanifest` y generación del paquete nativo distribuible en `dist/TimeTrackingStudio_v1.0.0_x64.msix`.
- **Suite de Pruebas en C#:** Creación de `tests/TimeTrackingStudio.Tests` con MSTest para validación continua de modelos y cálculos de facturación.

