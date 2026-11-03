# Bitácora de Desarrollo — Time Tracking Studio

Registro de decisiones técnicas, hitos completados y cambios arquitectónicos implementados.

---

## [Versión 3.0.0] - 2026-11-02

### Migración Completa a Rust con Tauri v2 y Eliminación de C#
- **Remoción Absoluta del Stack C# / .NET:** Eliminación completa de `TimeTrackingStudio.WinUI`, `tests/TimeTrackingStudio.Tests`, `dist/*.msix`, soluciones `.sln`, proyectos `.csproj`, código C# (`.cs`), definiciones XAML (`.xaml`), archivos de manifiesto MSIX y cachés `bin/` y `obj/`.
- **Arquitectura de Alto Rendimiento en Rust & Tauri v2:**
  - Creación del núcleo nativo en Rust con Tauri v2 (`tauri 2.12.5`, `tauri-build 2.7.4`).
  - Capa de persistencia SQLite embebida y compilada estáticamente con `rusqlite` (feature `bundled`), asegurando independencia total de dependencias externas en el sistema operativo.
  - Implementación de comandos Tauri IPC en `src-tauri/src/commands.rs` con tipado fuerte y serialización/deserialización con `serde` / `serde_json`.
  - Módulos desacoplados: `models.rs` (entidades de dominio), `db.rs` (gestor SQLite, claves foráneas, semilla de datos y consultas indexadas), `export.rs` (generador de reportes CSV y HTML maquetados) y `lib.rs` / `main.rs` (orquestación).
- **Interfaz Web Nativa y Ergonomía Visual:**
  - Maquetación y estilos modernos en `src/` (`index.html`, `styles.css`, `app.js`) utilizando estándares web limpios y modernos, sin sobrecarga de frameworks ni dependencias superfluas en node_modules.
  - Cronómetro global persistente en tiempo real en la barra superior con display digital digital monospace, selector reactivo de trabajos, inicio, pausa, reanudación, guardado y descarte.
  - Vistas completas: Panel de Control (Dashboard con tarjetas KPI y gráfico de barras semanal dinámico), Trabajos y Tareas (seguimiento de horas estimadas vs registradas, barra de progreso y cambio rápido de estados), Clientes (directorio, tarifas y facturación acumulada), Hoja de Tiempos (timesheet con auditoría histórica y entrada manual), y Reportes (exportación a CSV y HTML con apertura directa).
- **Pruebas Unitarias e Integración en Rust:**
  - Suite de pruebas automatizadas en `src-tauri/tests/calculations_tests.rs` y espejo en `tests/calculations_tests.rs`.
  - 100% de tests aprobados (`cargo test`), validando cálculos de tiempos decimales, importes facturables, topes porcentuales de avance, formateo de tarifas y ciclo CRUD con base de datos en memoria.
- **Compilación Release y Generación del Ejecutable Nativo:**
  - Compilación optimizada en modo release con Rust (`cargo build --release`).
  - Generación del archivo binario ejecutable standalone `dist/TimeTrackingStudio.exe` (~11 MB) listo para su ejecución inmediata y distribución.
  - Verificación en tiempo de ejecución: arranque limpio, sin excepciones, con respuesta inmediata de interfaz (`Responding: True`).

---

## [Versión 2.0.0] - 2026-11-01

### Migración a C# WinUI 3 y Eliminación de Python
- **Remoción del Stack Python:** Eliminación completa del entorno virtual `.venv`, `requirements.txt`, punto de entrada `main.py`, spec de PyInstaller `TimeTrackingStudio.spec` y código heredado en `src/`.
- **Arquitectura MVVM Nativa:** Implementación en C# (.NET 8) y WinUI 3 con `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
- **Persistencia en C#:** Implementación de `DatabaseService`, `ClientService`, `JobService`, `TimeService` y `ExportService` utilizando `Microsoft.Data.Sqlite`.
- **Interfaz XAML Fluent:** `MainWindow.xaml`, `DashboardPage.xaml`, `ClientsPage.xaml`, `JobsPage.xaml`, `TimesheetPage.xaml`, `ReportsPage.xaml` y diálogos modales.
- **Empaquetado MSIX:** Configuración de `Package.appxmanifest` y generación del paquete nativo distribuible en `dist/TimeTrackingStudio_v1.0.0_x64.msix`.
- **Suite de Pruebas en C#:** Creación de `tests/TimeTrackingStudio.Tests` con MSTest para validación continua de modelos y cálculos de facturación.

---

## [Versión 1.0.0] - 2026-11-01

### Inicialización y Entorno
- Creación de entorno virtual `.venv` con CPython 3.12.
- Instalación de librerías base: `PySide6`, `PyInstaller`, `pandas`, `numpy`, `matplotlib`, `seaborn`, `reportlab`.
- Generación y bloqueo de dependencias exactas en `requirements.txt`.
- Adición de `LICENSE.txt` (Licencia MIT) y `.gitignore` con exclusiones de entornos, caches y ejecutables.
- Identidad visual y recursos gráficos en `assets/`.
