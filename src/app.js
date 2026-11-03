/**
 * Time Tracking Studio - Frontend Application Logic
 * Integrado de forma reactiva con el backend nativo de Rust mediante Tauri v2 Core IPC.
 */

class TimeTrackerApp {
  constructor() {
    this.currentView = "dashboard";
    this.clients = [];
    this.jobs = [];
    this.timeLogs = [];
    this.metrics = null;

    // Cronómetro en memoria
    this.timer = {
      state: "stopped", // "stopped" | "running" | "paused"
      seconds: 0,
      intervalId: null,
      jobId: null,
      jobTitle: "",
      clientName: "",
      hourlyRate: 0,
      description: "",
      startTime: null,
    };

    // Tauri invoke helper con compatibilidad segura
    this.invoke = async (cmd, args = {}) => {
      if (window.__TAURI__ && window.__TAURI__.core && window.__TAURI__.core.invoke) {
        return await window.__TAURI__.core.invoke(cmd, args);
      } else if (window.__TAURI_INTERNALS__ && window.__TAURI_INTERNALS__.invoke) {
        return await window.__TAURI_INTERNALS__.invoke(cmd, args);
      } else {
        console.warn(`[Mock] Tauri invoke not found for "${cmd}", using local mock.`);
        return this.mockInvoke(cmd, args);
      }
    };
  }

  async init() {
    this.setupNavigation();
    this.setupTimerControls();
    this.setupClientsEvents();
    this.setupJobsEvents();
    this.setupTimesheetEvents();
    this.setupReportsEvents();

    await this.refreshAllData();
  }

  // ================= NAVEGACIÓN ENTRE VISTAS =================

  setupNavigation() {
    const navItems = document.querySelectorAll(".nav-item");
    navItems.forEach((btn) => {
      btn.addEventListener("click", () => {
        const view = btn.dataset.view;
        this.navigate(view);
      });
    });
  }

  navigate(viewName) {
    this.currentView = viewName;
    document.querySelectorAll(".nav-item").forEach((btn) => {
      btn.classList.toggle("active", btn.dataset.view === viewName);
    });

    document.querySelectorAll(".view-panel").forEach((panel) => {
      panel.classList.toggle("active", panel.id === `view-${viewName}`);
    });

    if (viewName === "dashboard") this.loadDashboard();
    if (viewName === "jobs") this.loadJobs();
    if (viewName === "clients") this.loadClients();
    if (viewName === "timesheet") this.loadTimesheet();
    if (viewName === "reports") this.loadReports();
  }

  // ================= CRONÓMETRO GLOBAL =================

  setupTimerControls() {
    const btnStart = document.getElementById("btn-timer-start");
    const btnPause = document.getElementById("btn-timer-pause");
    const btnSave = document.getElementById("btn-timer-save");
    const btnDiscard = document.getElementById("btn-timer-discard");
    const jobSelect = document.getElementById("timer-job-select");
    const descInput = document.getElementById("timer-desc-input");

    btnStart.addEventListener("click", () => {
      if (this.timer.state === "paused") {
        this.resumeTimer();
      } else {
        const selectedJobId = parseInt(jobSelect.value, 10);
        if (!selectedJobId) {
          alert("Por favor selecciona un trabajo o tarea antes de iniciar el cronómetro.");
          return;
        }
        const job = this.jobs.find((j) => j.id === selectedJobId);
        const client = this.clients.find((c) => c.id === (job ? job.client_id : null));
        const rate = (job && job.hourly_rate) ? job.hourly_rate : (client ? client.hourly_rate : 0.0);

        this.startTimer(
          selectedJobId,
          job ? job.title : "",
          job ? job.client_name : "",
          rate,
          descInput.value.trim()
        );
      }
    });

    btnPause.addEventListener("click", () => this.pauseTimer());
    btnSave.addEventListener("click", () => this.stopAndSaveTimer());
    btnDiscard.addEventListener("click", () => {
      if (confirm("¿Seguro que deseas descartar la sesión actual? No se guardará en el historial.")) {
        this.discardTimer();
      }
    });
  }

  startTimer(jobId, jobTitle, clientName, hourlyRate, description) {
    this.timer.state = "running";
    this.timer.seconds = 0;
    this.timer.jobId = jobId;
    this.timer.jobTitle = jobTitle;
    this.timer.clientName = clientName;
    this.timer.hourlyRate = hourlyRate;
    this.timer.description = description;
    this.timer.startTime = new Date().toISOString();

    clearInterval(this.timer.intervalId);
    this.timer.intervalId = setInterval(() => {
      this.timer.seconds++;
      this.updateTimerDisplay();
    }, 1000);

    this.updateTimerUIState();
  }

  pauseTimer() {
    if (this.timer.state === "running") {
      this.timer.state = "paused";
      clearInterval(this.timer.intervalId);
      this.updateTimerUIState();
    }
  }

  resumeTimer() {
    if (this.timer.state === "paused") {
      this.timer.state = "running";
      this.timer.intervalId = setInterval(() => {
        this.timer.seconds++;
        this.updateTimerDisplay();
      }, 1000);
      this.updateTimerUIState();
    }
  }

  async stopAndSaveTimer() {
    if (this.timer.state === "stopped" || !this.timer.jobId || this.timer.seconds <= 0) {
      this.discardTimer();
      return;
    }

    clearInterval(this.timer.intervalId);

    const descInput = document.getElementById("timer-desc-input");
    const description = descInput.value.trim() || this.timer.description || "Sesión de trabajo";

    const payload = {
      job_id: this.timer.jobId,
      start_time: this.timer.startTime || new Date().toISOString(),
      end_time: new Date().toISOString(),
      duration_seconds: this.timer.seconds,
      description: description,
      is_billable: true,
      hourly_rate: this.timer.hourlyRate,
    };

    try {
      await this.invoke("add_time_log", { input: payload });
      this.discardTimer();
      await this.refreshAllData();
    } catch (err) {
      alert("Error guardando el registro de tiempo: " + err);
    }
  }

  discardTimer() {
    clearInterval(this.timer.intervalId);
    this.timer.state = "stopped";
    this.timer.seconds = 0;
    this.timer.jobId = null;
    this.timer.jobTitle = "";
    this.timer.clientName = "";
    this.timer.hourlyRate = 0;
    this.timer.description = "";
    this.timer.startTime = null;

    document.getElementById("timer-desc-input").value = "";
    this.updateTimerDisplay();
    this.updateTimerUIState();
  }

  updateTimerDisplay() {
    const s = this.timer.seconds;
    const hours = Math.floor(s / 3600);
    const minutes = Math.floor((s % 3600) / 60);
    const seconds = s % 60;
    const fmt = `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
    document.getElementById("timer-digital-display").textContent = fmt;
  }

  updateTimerUIState() {
    const btnStart = document.getElementById("btn-timer-start");
    const btnPause = document.getElementById("btn-timer-pause");
    const btnSave = document.getElementById("btn-timer-save");
    const btnDiscard = document.getElementById("btn-timer-discard");
    const jobSelect = document.getElementById("timer-job-select");
    const descInput = document.getElementById("timer-desc-input");

    if (this.timer.state === "running") {
      btnStart.classList.add("hidden");
      btnPause.classList.remove("hidden");
      btnSave.classList.remove("hidden");
      btnDiscard.classList.remove("hidden");
      jobSelect.disabled = true;
    } else if (this.timer.state === "paused") {
      btnStart.classList.remove("hidden");
      btnStart.querySelector(".label").textContent = "Reanudar";
      btnPause.classList.add("hidden");
      btnSave.classList.remove("hidden");
      btnDiscard.classList.remove("hidden");
      jobSelect.disabled = true;
    } else {
      btnStart.classList.remove("hidden");
      btnStart.querySelector(".label").textContent = "Iniciar";
      btnPause.classList.add("hidden");
      btnSave.classList.add("hidden");
      btnDiscard.classList.add("hidden");
      jobSelect.disabled = false;
    }
  }

  // ================= CARGA GENERAL DE DATOS =================

  async refreshAllData() {
    try {
      this.clients = await this.invoke("get_clients", { search: null, only_active: false });
      this.jobs = await this.invoke("get_jobs", { status: null, client_id: null, search: null });
      this.timeLogs = await this.invoke("get_time_logs", { job_id: null, client_id: null, from_date: null, to_date: null });
      this.metrics = await this.invoke("get_dashboard_metrics");

      this.populateSelects();

      if (this.currentView === "dashboard") this.loadDashboard();
      if (this.currentView === "jobs") this.loadJobs();
      if (this.currentView === "clients") this.loadClients();
      if (this.currentView === "timesheet") this.loadTimesheet();
      if (this.currentView === "reports") this.loadReports();
    } catch (err) {
      console.error("Error refreshing data:", err);
    }
  }

  populateSelects() {
    // 1. Selector del cronómetro
    const timerSelect = document.getElementById("timer-job-select");
    const currentVal = timerSelect.value;
    timerSelect.innerHTML = `<option value="">-- Seleccionar Trabajo --</option>`;
    this.jobs.filter((j) => j.status !== "Facturado" && j.status !== "Entregado").forEach((j) => {
      timerSelect.innerHTML += `<option value="${j.id}">[${this.escapeHtml(j.client_name)}] ${this.escapeHtml(j.title)}</option>`;
    });
    if (currentVal) timerSelect.value = currentVal;

    // 2. Selectores de clientes
    const clientSelects = ["jobs-client-filter", "timesheet-client-filter", "report-client-filter", "job-client-id"];
    clientSelects.forEach((id) => {
      const el = document.getElementById(id);
      if (!el) return;
      const val = el.value;
      const isFilter = id.includes("filter");
      el.innerHTML = isFilter ? `<option value="0">Todos los Clientes</option>` : `<option value="">-- Seleccionar Cliente --</option>`;
      this.clients.forEach((c) => {
        el.innerHTML += `<option value="${c.id}">${this.escapeHtml(c.name)} ${c.company ? "(" + this.escapeHtml(c.company) + ")" : ""}</option>`;
      });
      if (val) el.value = val;
    });

    // 3. Selectores de trabajos en filtros y modal
    const jobSelects = ["timesheet-job-filter", "report-job-filter", "time-job-id"];
    jobSelects.forEach((id) => {
      const el = document.getElementById(id);
      if (!el) return;
      const val = el.value;
      const isFilter = id.includes("filter");
      el.innerHTML = isFilter ? `<option value="0">Todos los Trabajos</option>` : `<option value="">-- Seleccionar Trabajo --</option>`;
      this.jobs.forEach((j) => {
        el.innerHTML += `<option value="${j.id}">[${this.escapeHtml(j.client_name)}] ${this.escapeHtml(j.title)}</option>`;
      });
      if (val) el.value = val;
    });
  }

  // ================= VISTA 1: DASHBOARD =================

  async loadDashboard() {
    try {
      this.metrics = await this.invoke("get_dashboard_metrics");
    } catch (e) {
      console.warn("Could not fetch dashboard metrics", e);
    }

    if (!this.metrics) return;

    document.getElementById("kpi-hours-today").textContent = `${this.metrics.hours_today.toFixed(1)} h`;
    document.getElementById("kpi-billable-today").textContent = `$${this.metrics.billable_today.toFixed(2)}`;
    document.getElementById("kpi-hours-month").textContent = `${this.metrics.hours_this_month.toFixed(1)} h`;
    document.getElementById("kpi-billable-month").textContent = `Facturable: $${this.metrics.billable_this_month.toFixed(2)}`;
    document.getElementById("kpi-active-jobs").textContent = this.metrics.active_jobs_count;
    document.getElementById("kpi-total-clients").textContent = `Clientes Activos: ${this.metrics.total_clients_count}`;

    // Gráfico de los últimos 7 días
    const chartContainer = document.getElementById("weekly-chart");
    chartContainer.innerHTML = "";
    const days = this.metrics.last_7_days || [];
    days.forEach((d) => {
      const col = document.createElement("div");
      col.className = "chart-bar-col";
      col.innerHTML = `
        <div class="chart-bar-wrapper">
          <div class="chart-bar" style="height: ${Math.max(8, d.bar_height)}px;">
            <span class="chart-bar-tooltip">${d.formatted_hours}</span>
          </div>
        </div>
        <span class="chart-day-label">${this.escapeHtml(d.day_name)}</span>
      `;
      chartContainer.appendChild(col);
    });

    // Últimas sesiones
    const tbody = document.getElementById("dashboard-recent-tbody");
    tbody.innerHTML = "";
    const recents = this.timeLogs.slice(0, 5);
    if (recents.length === 0) {
      tbody.innerHTML = `<tr><td colspan="6" style="text-align:center; color:#888;">No hay sesiones registradas aún.</td></tr>`;
      return;
    }

    recents.forEach((l) => {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td><strong>${this.escapeHtml(l.client_name)}</strong></td>
        <td>${this.escapeHtml(l.job_title)}</td>
        <td>${this.escapeHtml(l.start_time.replace("T", " "))}</td>
        <td>${this.escapeHtml(l.formatted_duration)}</td>
        <td><span class="badge ${l.is_billable ? "badge-delivered" : "badge-pending"}">${l.is_billable ? "Sí" : "No"}</span></td>
        <td><strong>$${l.amount.toFixed(2)}</strong></td>
      `;
      tbody.appendChild(tr);
    });
  }

  // ================= VISTA 2: TRABAJOS =================

  setupJobsEvents() {
    const searchInput = document.getElementById("jobs-search-input");
    const statusFilter = document.getElementById("jobs-status-filter");
    const clientFilter = document.getElementById("jobs-client-filter");
    const btnAdd = document.getElementById("btn-add-job");
    const formJob = document.getElementById("form-job");

    const refreshJobs = () => {
      this.loadJobs();
    };

    searchInput.addEventListener("input", refreshJobs);
    statusFilter.addEventListener("change", refreshJobs);
    clientFilter.addEventListener("change", refreshJobs);

    btnAdd.addEventListener("click", () => {
      document.getElementById("modal-job-title").textContent = "Nuevo Trabajo o Proyecto";
      formJob.reset();
      document.getElementById("job-id").value = "";
      this.openModal("modal-job");
    });

    formJob.addEventListener("submit", async (e) => {
      e.preventDefault();
      const id = document.getElementById("job-id").value;
      const clientId = parseInt(document.getElementById("job-client-id").value, 10);
      const title = document.getElementById("job-title-input").value.trim();
      const description = document.getElementById("job-desc").value.trim();
      const status = document.getElementById("job-status").value;
      const priority = document.getElementById("job-priority").value;
      const rateVal = document.getElementById("job-rate").value;
      const hourlyRate = rateVal ? parseFloat(rateVal) : null;
      const estimatedHours = parseFloat(document.getElementById("job-est-hours").value) || 0.0;
      const dueDate = document.getElementById("job-due-date").value || null;

      try {
        if (id) {
          await this.invoke("update_job", {
            input: {
              id: parseInt(id, 10),
              client_id: clientId,
              title,
              description: description || null,
              status,
              priority,
              hourly_rate: hourlyRate,
              estimated_hours: estimatedHours,
              due_date: dueDate,
            },
          });
        } else {
          await this.invoke("add_job", {
            input: {
              client_id: clientId,
              title,
              description: description || null,
              status,
              priority,
              hourly_rate: hourlyRate,
              estimated_hours: estimatedHours,
              due_date: dueDate,
            },
          });
        }
        this.closeModal("modal-job");
        await this.refreshAllData();
      } catch (err) {
        alert("Error guardando el trabajo: " + err);
      }
    });
  }

  async loadJobs() {
    const search = document.getElementById("jobs-search-input").value.trim();
    const status = document.getElementById("jobs-status-filter").value;
    const clientId = parseInt(document.getElementById("jobs-client-filter").value, 10);

    try {
      this.jobs = await this.invoke("get_jobs", {
        status: status === "Todos" ? null : status,
        client_id: clientId > 0 ? clientId : null,
        search: search || null,
      });
    } catch (e) {
      console.error(e);
    }

    const tbody = document.getElementById("jobs-tbody");
    tbody.innerHTML = "";

    if (this.jobs.length === 0) {
      tbody.innerHTML = `<tr><td colspan="8" style="text-align:center; color:#888;">No se encontraron trabajos coincidentes.</td></tr>`;
      return;
    }

    this.jobs.forEach((j) => {
      const tr = document.createElement("tr");

      const statusBadgeClass = {
        "Pendiente": "badge-pending",
        "En progreso": "badge-progress",
        "En revisión": "badge-review",
        "Entregado": "badge-delivered",
        "Facturado": "badge-billed",
      }[j.status] || "badge-pending";

      const priorityBadgeClass = {
        "Baja": "badge-priority-baja",
        "Media": "badge-priority-media",
        "Alta": "badge-priority-alta",
        "Urgente": "badge-priority-urgente",
      }[j.priority] || "badge-priority-media";

      tr.innerHTML = `
        <td>
          <strong>${this.escapeHtml(j.title)}</strong>
          ${j.description ? `<div style="font-size:11px; color:#6b7280; max-width:250px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;">${this.escapeHtml(j.description)}</div>` : ""}
        </td>
        <td>${this.escapeHtml(j.client_name)}</td>
        <td><span class="badge ${priorityBadgeClass}">${this.escapeHtml(j.priority)}</span></td>
        <td>
          <select class="form-control btn-sm status-changer" data-job-id="${j.id}" style="width: auto; padding: 2px 6px;">
            <option value="Pendiente" ${j.status === "Pendiente" ? "selected" : ""}>🟡 Pendiente</option>
            <option value="En progreso" ${j.status === "En progreso" ? "selected" : ""}>🟠 En progreso</option>
            <option value="En revisión" ${j.status === "En revisión" ? "selected" : ""}>🔵 En revisión</option>
            <option value="Entregado" ${j.status === "Entregado" ? "selected" : ""}>🟢 Entregado</option>
            <option value="Facturado" ${j.status === "Facturado" ? "selected" : ""}>🟣 Facturado</option>
          </select>
        </td>
        <td>
          <div class="progress-container">
            <div class="progress-bar-bg">
              <div class="progress-bar-fill" style="width: ${Math.min(100, j.progress_percentage)}%;"></div>
            </div>
            <span class="progress-text">${j.logged_hours.toFixed(1)} / ${j.estimated_hours.toFixed(1)} h (${j.progress_percentage.toFixed(0)}%)</span>
          </div>
        </td>
        <td>${j.hourly_rate ? `$${j.hourly_rate.toFixed(2)}/h` : `<span style="color:#9ca3af">Heredada</span>`}</td>
        <td><strong>$${j.total_earned.toFixed(2)}</strong></td>
        <td>
          <div style="display:flex; gap:6px;">
            <button class="btn btn-outline btn-sm btn-edit-job" data-id="${j.id}">Editar</button>
            <button class="btn btn-danger-outline btn-sm btn-del-job" data-id="${j.id}">Eliminar</button>
          </div>
        </td>
      `;

      // Evento de cambio rápido de estado
      tr.querySelector(".status-changer").addEventListener("change", async (e) => {
        const newStatus = e.target.value;
        try {
          await this.invoke("update_job_status", { id: j.id, status: newStatus });
          await this.refreshAllData();
        } catch (err) {
          alert("Error actualizando estado: " + err);
        }
      });

      // Evento Editar
      tr.querySelector(".btn-edit-job").addEventListener("click", () => this.editJob(j));

      // Evento Eliminar
      tr.querySelector(".btn-del-job").addEventListener("click", () => this.deleteJob(j.id, j.title));

      tbody.appendChild(tr);
    });
  }

  editJob(j) {
    document.getElementById("modal-job-title").textContent = "Editar Trabajo o Tarea";
    document.getElementById("job-id").value = j.id;
    document.getElementById("job-client-id").value = j.client_id;
    document.getElementById("job-title-input").value = j.title;
    document.getElementById("job-desc").value = j.description || "";
    document.getElementById("job-status").value = j.status;
    document.getElementById("job-priority").value = j.priority;
    document.getElementById("job-rate").value = j.hourly_rate ? j.hourly_rate : "";
    document.getElementById("job-est-hours").value = j.estimated_hours;
    document.getElementById("job-due-date").value = j.due_date ? j.due_date.substring(0, 10) : "";
    this.openModal("modal-job");
  }

  async deleteJob(id, title) {
    if (confirm(`¿Estás seguro de eliminar el trabajo "${title}"? Se eliminarán también todas sus entradas de tiempo asociadas.`)) {
      try {
        await this.invoke("delete_job", { id });
        await this.refreshAllData();
      } catch (err) {
        alert("Error eliminando trabajo: " + err);
      }
    }
  }

  // ================= VISTA 3: CLIENTES =================

  setupClientsEvents() {
    const searchInput = document.getElementById("clients-search-input");
    const activeCheck = document.getElementById("clients-active-only");
    const btnAdd = document.getElementById("btn-add-client");
    const formClient = document.getElementById("form-client");

    const refresh = () => this.loadClients();
    searchInput.addEventListener("input", refresh);
    activeCheck.addEventListener("change", refresh);

    btnAdd.addEventListener("click", () => {
      document.getElementById("modal-client-title").textContent = "Nuevo Cliente";
      formClient.reset();
      document.getElementById("client-id").value = "";
      document.getElementById("client-active").checked = true;
      document.getElementById("client-rate").value = "50.0";
      this.openModal("modal-client");
    });

    formClient.addEventListener("submit", async (e) => {
      e.preventDefault();
      const id = document.getElementById("client-id").value;
      const name = document.getElementById("client-name").value.trim();
      const company = document.getElementById("client-company").value.trim();
      const email = document.getElementById("client-email").value.trim();
      const phone = document.getElementById("client-phone").value.trim();
      const rate = parseFloat(document.getElementById("client-rate").value) || 0.0;
      const currency = document.getElementById("client-currency").value;
      const notes = document.getElementById("client-notes").value.trim();
      const isActive = document.getElementById("client-active").checked;

      try {
        if (id) {
          await this.invoke("update_client", {
            input: {
              id: parseInt(id, 10),
              name,
              company: company || null,
              email: email || null,
              phone: phone || null,
              hourly_rate: rate,
              currency: currency || null,
              notes: notes || null,
              is_active: isActive,
            },
          });
        } else {
          await this.invoke("add_client", {
            input: {
              name,
              company: company || null,
              email: email || null,
              phone: phone || null,
              hourly_rate: rate,
              currency: currency || null,
              notes: notes || null,
              is_active: isActive,
            },
          });
        }
        this.closeModal("modal-client");
        await this.refreshAllData();
      } catch (err) {
        alert("Error guardando cliente: " + err);
      }
    });
  }

  async loadClients() {
    const search = document.getElementById("clients-search-input").value.trim();
    const onlyActive = document.getElementById("clients-active-only").checked;

    try {
      this.clients = await this.invoke("get_clients", {
        search: search || null,
        only_active: onlyActive,
      });
    } catch (e) {
      console.error(e);
    }

    const tbody = document.getElementById("clients-tbody");
    tbody.innerHTML = "";

    if (this.clients.length === 0) {
      tbody.innerHTML = `<tr><td colspan="8" style="text-align:center; color:#888;">No se encontraron clientes.</td></tr>`;
      return;
    }

    this.clients.forEach((c) => {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td><strong>${this.escapeHtml(c.name)}</strong></td>
        <td>${this.escapeHtml(c.company || "-")}</td>
        <td>
          <div>${this.escapeHtml(c.email || "-")}</div>
          <div style="font-size:11px; color:#6b7280">${this.escapeHtml(c.phone || "")}</div>
        </td>
        <td>${this.escapeHtml(c.display_rate)}</td>
        <td>${c.active_jobs_count} activos</td>
        <td><strong>$${c.total_earned.toFixed(2)}</strong></td>
        <td><span class="badge ${c.is_active ? "badge-active" : "badge-inactive"}">${c.is_active ? "Activo" : "Inactivo"}</span></td>
        <td>
          <div style="display:flex; gap:6px;">
            <button class="btn btn-outline btn-sm btn-edit-client" data-id="${c.id}">Editar</button>
            <button class="btn btn-danger-outline btn-sm btn-del-client" data-id="${c.id}">Eliminar</button>
          </div>
        </td>
      `;

      tr.querySelector(".btn-edit-client").addEventListener("click", () => this.editClient(c));
      tr.querySelector(".btn-del-client").addEventListener("click", () => this.deleteClient(c.id, c.name));

      tbody.appendChild(tr);
    });
  }

  editClient(c) {
    document.getElementById("modal-client-title").textContent = "Editar Cliente";
    document.getElementById("client-id").value = c.id;
    document.getElementById("client-name").value = c.name;
    document.getElementById("client-company").value = c.company || "";
    document.getElementById("client-email").value = c.email || "";
    document.getElementById("client-phone").value = c.phone || "";
    document.getElementById("client-rate").value = c.hourly_rate;
    document.getElementById("client-currency").value = c.currency;
    document.getElementById("client-notes").value = c.notes || "";
    document.getElementById("client-active").checked = c.is_active;
    this.openModal("modal-client");
  }

  async deleteClient(id, name) {
    if (confirm(`¿Estás seguro de eliminar al cliente "${name}"? Esta acción borrará también todos sus proyectos y tiempos.`)) {
      try {
        await this.invoke("delete_client", { id });
        await this.refreshAllData();
      } catch (err) {
        alert("Error eliminando cliente: " + err);
      }
    }
  }

  // ================= VISTA 4: HOJA DE TIEMPOS =================

  setupTimesheetEvents() {
    const clientFilter = document.getElementById("timesheet-client-filter");
    const jobFilter = document.getElementById("timesheet-job-filter");
    const fromDate = document.getElementById("timesheet-from-date");
    const toDate = document.getElementById("timesheet-to-date");
    const btnClear = document.getElementById("btn-timesheet-clear-filters");
    const btnAdd = document.getElementById("btn-add-manual-time");
    const formTime = document.getElementById("form-time");

    const refresh = () => this.loadTimesheet();
    clientFilter.addEventListener("change", refresh);
    jobFilter.addEventListener("change", refresh);
    fromDate.addEventListener("change", refresh);
    toDate.addEventListener("change", refresh);

    btnClear.addEventListener("click", () => {
      clientFilter.value = "0";
      jobFilter.value = "0";
      fromDate.value = "";
      toDate.value = "";
      this.loadTimesheet();
    });

    btnAdd.addEventListener("click", () => {
      formTime.reset();
      const now = new Date();
      // Ajuste ISO local para datetime-local
      const tzOffset = now.getTimezoneOffset() * 60000;
      const localISOTime = new Date(now.getTime() - tzOffset).toISOString().slice(0, 16);
      document.getElementById("time-start").value = localISOTime;
      document.getElementById("time-hours").value = "1";
      document.getElementById("time-minutes").value = "0";
      document.getElementById("time-billable").checked = true;
      this.openModal("modal-time");
    });

    formTime.addEventListener("submit", async (e) => {
      e.preventDefault();
      const jobId = parseInt(document.getElementById("time-job-id").value, 10);
      const startTime = document.getElementById("time-start").value;
      const hours = parseInt(document.getElementById("time-hours").value, 10) || 0;
      const minutes = parseInt(document.getElementById("time-minutes").value, 10) || 0;
      const durationSeconds = (hours * 3600) + (minutes * 60);

      if (durationSeconds <= 0) {
        alert("La duración debe ser mayor a 0 minutos.");
        return;
      }

      const desc = document.getElementById("time-desc").value.trim();
      const rateVal = document.getElementById("time-rate").value;
      const rate = rateVal ? parseFloat(rateVal) : null;
      const isBillable = document.getElementById("time-billable").checked;

      try {
        await this.invoke("add_time_log", {
          input: {
            job_id: jobId,
            start_time: startTime ? startTime.replace("T", " ") : null,
            duration_seconds: durationSeconds,
            description: desc || null,
            is_billable: isBillable,
            hourly_rate: rate,
          },
        });
        this.closeModal("modal-time");
        await this.refreshAllData();
      } catch (err) {
        alert("Error registrando tiempo manual: " + err);
      }
    });
  }

  async loadTimesheet() {
    const clientId = parseInt(document.getElementById("timesheet-client-filter").value, 10);
    const jobId = parseInt(document.getElementById("timesheet-job-filter").value, 10);
    const fromDate = document.getElementById("timesheet-from-date").value;
    const toDate = document.getElementById("timesheet-to-date").value;

    try {
      this.timeLogs = await this.invoke("get_time_logs", {
        job_id: jobId > 0 ? jobId : null,
        client_id: clientId > 0 ? clientId : null,
        from_date: fromDate || null,
        to_date: toDate || null,
      });
    } catch (e) {
      console.error(e);
    }

    const tbody = document.getElementById("timesheet-tbody");
    tbody.innerHTML = "";

    if (this.timeLogs.length === 0) {
      tbody.innerHTML = `<tr><td colspan="10" style="text-align:center; color:#888;">No hay registros de tiempo para el filtro seleccionado.</td></tr>`;
      return;
    }

    this.timeLogs.forEach((l) => {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${this.escapeHtml(l.start_time.replace("T", " "))}</td>
        <td><strong>${this.escapeHtml(l.client_name)}</strong></td>
        <td>${this.escapeHtml(l.job_title)}</td>
        <td>${this.escapeHtml(l.description || "-")}</td>
        <td><code>${this.escapeHtml(l.formatted_duration)}</code></td>
        <td>${l.hours.toFixed(2)} h</td>
        <td>$${l.hourly_rate.toFixed(2)}/h</td>
        <td><span class="badge ${l.is_billable ? "badge-delivered" : "badge-pending"}">${l.is_billable ? "Sí" : "No"}</span></td>
        <td><strong>$${l.amount.toFixed(2)}</strong></td>
        <td>
          <button class="btn btn-danger-outline btn-sm btn-del-log" data-id="${l.id}">Eliminar</button>
        </td>
      `;

      tr.querySelector(".btn-del-log").addEventListener("click", () => this.deleteTimeLog(l.id));
      tbody.appendChild(tr);
    });
  }

  async deleteTimeLog(id) {
    if (confirm("¿Estás seguro de eliminar esta entrada de tiempo?")) {
      try {
        await this.invoke("delete_time_log", { id });
        await this.refreshAllData();
      } catch (err) {
        alert("Error eliminando registro: " + err);
      }
    }
  }

  // ================= VISTA 5: REPORTES =================

  setupReportsEvents() {
    const btnCsv = document.getElementById("btn-export-csv");
    const btnHtml = document.getElementById("btn-export-html");
    const btnOpenFolder = document.getElementById("btn-open-output-folder");

    btnCsv.addEventListener("click", async () => {
      const clientId = parseInt(document.getElementById("report-client-filter").value, 10);
      const jobId = parseInt(document.getElementById("report-job-filter").value, 10);
      const fromDate = document.getElementById("report-from-date").value;
      const toDate = document.getElementById("report-to-date").value;

      try {
        const filePath = await this.invoke("export_csv", {
          job_id: jobId > 0 ? jobId : null,
          client_id: clientId > 0 ? clientId : null,
          from_date: fromDate || null,
          to_date: toDate || null,
          prefix: "reporte_tiempos",
        });
        this.showReportBanner(`✅ Archivo CSV generado con éxito: <strong>${filePath}</strong>`, filePath);
      } catch (err) {
        alert("Error exportando a CSV: " + err);
      }
    });

    btnHtml.addEventListener("click", async () => {
      const clientId = parseInt(document.getElementById("report-client-filter").value, 10);
      const jobId = parseInt(document.getElementById("report-job-filter").value, 10);
      const fromDate = document.getElementById("report-from-date").value;
      const toDate = document.getElementById("report-to-date").value;

      try {
        const filePath = await this.invoke("export_html", {
          job_id: jobId > 0 ? jobId : null,
          client_id: clientId > 0 ? clientId : null,
          from_date: fromDate || null,
          to_date: toDate || null,
          period_name: "Período Seleccionado",
        });
        this.showReportBanner(`✅ Informe Ejecutivo HTML generado: <strong>${filePath}</strong>`, filePath);
      } catch (err) {
        alert("Error generando informe HTML: " + err);
      }
    });

    btnOpenFolder.addEventListener("click", async () => {
      try {
        await this.invoke("open_output_folder");
      } catch (err) {
        alert("Error abriendo carpeta: " + err);
      }
    });
  }

  showReportBanner(msg, filePath) {
    const banner = document.getElementById("report-status-banner");
    banner.classList.remove("hidden");
    banner.innerHTML = `
      <div>${msg}</div>
      <button class="btn btn-outline btn-sm" id="btn-open-report-file" style="margin-left: 14px;">Abrir Archivo</button>
    `;
    document.getElementById("btn-open-report-file").addEventListener("click", async () => {
      await this.invoke("open_file_path", { path: filePath });
    });
  }

  loadReports() {
    // La vista de reportes mantiene sus filtros listos
  }

  // ================= MODALES =================

  openModal(id) {
    document.getElementById(id).classList.remove("hidden");
  }

  closeModal(id) {
    document.getElementById(id).classList.add("hidden");
  }

  escapeHtml(str) {
    if (!str) return "";
    return String(str)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#039;");
  }

  // Fallback mock en caso de correr fuera de WebView2 Tauri (para testing)
  mockInvoke(cmd, args) {
    console.log("[Mock Tauri]", cmd, args);
    return Promise.resolve([]);
  }
}

// Inicializar la aplicación al cargar el DOM
window.addEventListener("DOMContentLoaded", () => {
  window.app = new TimeTrackerApp();
  window.app.init();
});

