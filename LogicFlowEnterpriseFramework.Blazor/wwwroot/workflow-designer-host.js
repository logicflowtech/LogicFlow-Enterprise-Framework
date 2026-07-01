let workflowDesignerModule = null;
let workflowDesignerAssetVersion = '';
const workflowDesignerStylesheetId = 'logicflow-workflow-designer-styles';
const workflowLibraryFixStylesId = 'logicflow-workflow-library-fixes';
let workflowLibraryClickShield = null;
let workflowLibrarySyncTimers = [];
let workflowLibraryMenuHandler = null;
let workflowLibraryOverlayHandler = null;
let workflowLibraryActiveMenu = null;
let workflowLibraryActiveModal = null;
const workflowLibraryMenuHandlerOptions = { capture: true };

export function setSession(value) {
  workflowDesignerAssetVersion = value?.assetVersion ? String(value.assetVersion) : '';
  window.logicFlowWorkflowHost = window.logicFlowWorkflowHost || {};
  window.logicFlowWorkflowHost.session = value ?? null;
}

function getStylesheetUrl() {
  return workflowDesignerAssetVersion
    ? `/workflow-designer/assets/workflow-designer.css?v=${encodeURIComponent(workflowDesignerAssetVersion)}`
    : '/workflow-designer/assets/workflow-designer.css';
}

function getModuleUrl() {
  return workflowDesignerAssetVersion
    ? `/workflow-designer/assets/workflow-designer.js?v=${encodeURIComponent(workflowDesignerAssetVersion)}`
    : '/workflow-designer/assets/workflow-designer.js';
}

async function ensureWorkflowDesignerStylesheet() {
  const href = getStylesheetUrl();
  let link = document.getElementById(workflowDesignerStylesheetId);

  if (!(link instanceof HTMLLinkElement)) {
    link = Array.from(document.querySelectorAll('link[rel="stylesheet"]'))
      .find((candidate) => candidate instanceof HTMLLinkElement && candidate.href.endsWith('/workflow-designer/assets/workflow-designer.css'))
      ?? null;
  }

  if (link instanceof HTMLLinkElement) {
    link.id = workflowDesignerStylesheetId;

    if (link.href.endsWith(href)) {
      return;
    }

    link.href = href;
  } else {
    link = document.createElement('link');
    link.id = workflowDesignerStylesheetId;
    link.rel = 'stylesheet';
    link.href = href;
    document.head.appendChild(link);
  }

  await new Promise((resolve, reject) => {
    const cleanup = () => {
      link.removeEventListener('load', handleLoad);
      link.removeEventListener('error', handleError);
    };

    const handleLoad = () => {
      cleanup();
      resolve();
    };

    const handleError = () => {
      cleanup();
      reject(new Error(`Unable to load workflow designer stylesheet: ${href}`));
    };

    link.addEventListener('load', handleLoad, { once: true });
    link.addEventListener('error', handleError, { once: true });
  });
}

function applyWorkflowLibraryFixes() {
  const session = window.logicFlowWorkflowHost?.session;
  if (session?.hostMode !== 'library') {
    removeWorkflowLibraryFixes();
    return;
  }

  ensureWorkflowLibraryFixStyles();
  syncWorkflowLibrarySummary();

  if (!workflowLibraryClickShield) {
    workflowLibraryClickShield = (event) => {
      const target = event.target;
      if (target instanceof Element && target.closest('.workflow-library-host-modal')) {
        event.stopPropagation();
      }
    };

    document.addEventListener('click', workflowLibraryClickShield);
  }

  if (!workflowLibraryMenuHandler) {
    workflowLibraryMenuHandler = (event) => {
      const target = event.target;
      if (!(target instanceof Element)) {
        return;
      }

      const overflowButton = target.closest('.overflow-action-button');
      if (overflowButton instanceof HTMLElement) {
        event.preventDefault();
        event.stopPropagation();
        toggleWorkflowLibraryMenu(overflowButton);
        return;
      }

      if (!target.closest('.workflow-library-host-menu')) {
        closeWorkflowLibraryMenu();
      }
    };

    document.addEventListener('click', workflowLibraryMenuHandler, workflowLibraryMenuHandlerOptions);
  }

  clearWorkflowLibrarySyncTimers();
  workflowLibrarySyncTimers = [
    window.setTimeout(syncWorkflowLibrarySummary, 0),
    window.setTimeout(syncWorkflowLibrarySummary, 250),
    window.setTimeout(syncWorkflowLibrarySummary, 1000),
  ];
}

function removeWorkflowLibraryFixes() {
  clearWorkflowLibrarySyncTimers();
  closeWorkflowLibraryMenu();
  closeWorkflowLibraryModal();

  if (workflowLibraryClickShield) {
    document.removeEventListener('click', workflowLibraryClickShield);
    workflowLibraryClickShield = null;
  }

  if (workflowLibraryMenuHandler) {
    document.removeEventListener('click', workflowLibraryMenuHandler, workflowLibraryMenuHandlerOptions);
    workflowLibraryMenuHandler = null;
  }
}

function clearWorkflowLibrarySyncTimers() {
  workflowLibrarySyncTimers.forEach((timer) => window.clearTimeout(timer));
  workflowLibrarySyncTimers = [];
}

function ensureWorkflowLibraryFixStyles() {
  if (document.getElementById(workflowLibraryFixStylesId)) {
    return;
  }

  const style = document.createElement('style');
  style.id = workflowLibraryFixStylesId;
  style.textContent = `
    .work-area.workflow-list-area {
      padding-top: 14px !important;
      gap: 12px !important;
    }

    .page-heading.workflow-library-heading {
      min-height: 0 !important;
      gap: 14px !important;
      margin-bottom: 0 !important;
    }

    .workflow-library-panel-header {
      min-height: 0 !important;
      padding-top: 12px !important;
      padding-bottom: 12px !important;
    }

    .list-toolbar.workflow-list-toolbar {
      padding-top: 10px !important;
      padding-bottom: 10px !important;
    }

    .workflow-table-row {
      min-height: 58px !important;
      padding-top: 8px !important;
      padding-bottom: 8px !important;
    }

    .workflow-name-cell,
    .version-summary-cell,
    .workflow-time-cell,
    .workflow-status-cell {
      gap: 2px !important;
    }

    .workflow-library-panel,
    .workflow-table,
    .workflow-table-row,
    .workflow-library-action-row,
    .table-action-menu-shell,
    .workflow-action-cluster {
      overflow: visible !important;
    }

    .workflow-action-cluster,
    .table-action-menu-shell {
      position: relative !important;
    }

    .workflow-library-action-row .table-action-menu {
      z-index: 80 !important;
    }

    .workflow-library-host-menu {
      position: absolute;
      top: calc(100% + 8px);
      right: 0;
      min-width: 150px;
      display: grid;
      gap: 4px;
      padding: 6px;
      border: 1px solid #bcc9d6;
      border-radius: 8px;
      background: #ffffff;
      box-shadow: 0 12px 30px rgba(31, 44, 68, 0.16);
      z-index: 120;
    }

    .workflow-library-host-menu button {
      min-height: 34px;
      justify-content: flex-start;
      padding: 0 10px;
      border: 1px solid transparent;
      background: transparent;
      color: #33475d;
      font-size: 11px;
      font-weight: 800;
      letter-spacing: 0.03em;
      text-transform: uppercase;
    }

    .workflow-library-host-menu button:hover:not(:disabled) {
      background: #eef3f8;
      border-color: #d6e0ea;
    }

    .workflow-library-host-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(15, 23, 42, 0.18);
      z-index: 160;
      display: grid;
      place-items: center;
      padding: 24px;
    }

    .workflow-library-host-modal {
      width: min(720px, calc(100vw - 32px));
      max-height: calc(100vh - 32px);
      overflow: auto;
      padding: 20px;
      border: 1px solid #d5deea;
      border-radius: 12px;
      background: #ffffff;
      box-shadow: 0 24px 60px rgba(31, 44, 68, 0.2);
      display: grid;
      gap: 16px;
    }

    .workflow-library-host-modal h3,
    .workflow-library-host-modal p {
      margin: 0;
    }

    .workflow-library-host-modal .host-form-grid {
      display: grid;
      gap: 12px;
    }

    .workflow-library-host-modal label {
      display: grid;
      gap: 6px;
      color: #5f7187;
      font-size: 12px;
      font-weight: 700;
    }

    .workflow-library-host-modal input,
    .workflow-library-host-modal textarea {
      min-height: 38px;
      padding: 8px 10px;
      border: 1px solid #d7dfeb;
      border-radius: 8px;
      font: inherit;
    }

    .workflow-library-host-modal textarea {
      min-height: 110px;
      resize: vertical;
    }

    .workflow-library-host-modal .host-modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
    }

    .workflow-library-host-modal .host-version-list {
      display: grid;
      gap: 10px;
    }

    .workflow-library-host-modal .host-version-item {
      display: grid;
      gap: 6px;
      padding: 12px 14px;
      border: 1px solid #dce3ec;
      border-radius: 10px;
      background: #fbfcfe;
    }
  `;

  document.head.appendChild(style);
}

function syncWorkflowLibrarySummary() {
  const summary = document.querySelector('.workflow-library-panel-copy p');
  if (!(summary instanceof HTMLElement)) {
    return;
  }

  const rows = Array.from(document.querySelectorAll('.workflow-table .workflow-table-row'))
    .filter((row) =>
      !row.classList.contains('workflow-skeleton-row')
      && row instanceof HTMLElement
      && window.getComputedStyle(row).display !== 'none');

  if (rows.length === 0) {
    return;
  }

  const listedCount = rows.length;
  summary.textContent = `${listedCount} of ${listedCount} workflow${listedCount === 1 ? '' : 's'} visible in the current view.`;
}

function toggleWorkflowLibraryMenu(button) {
  const shell = button.closest('.table-action-menu-shell') ?? button.closest('.workflow-action-cluster');
  if (!(shell instanceof HTMLElement)) {
    return;
  }

  if (workflowLibraryActiveMenu?.parentElement === shell) {
    closeWorkflowLibraryMenu();
    return;
  }

  closeWorkflowLibraryMenu();

  const row = button.closest('.workflow-table-row');
  const definitionId = row?.querySelector('.workflow-id-text')?.textContent?.trim();
  const definitionName = row?.querySelector('.workflow-name-cell strong')?.textContent?.trim() ?? 'Workflow';
  if (!definitionId) {
    return;
  }

  const menu = document.createElement('div');
  menu.className = 'workflow-library-host-menu';
  menu.innerHTML = `
    <button type="button" data-action="history">History</button>
    <button type="button" data-action="publish">Publish</button>
  `;

  menu.addEventListener('click', async (event) => {
    const target = event.target;
    if (!(target instanceof HTMLButtonElement)) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    closeWorkflowLibraryMenu();

    if (target.dataset.action === 'history') {
      await openWorkflowHistoryModal(definitionId, definitionName);
    }

    if (target.dataset.action === 'publish') {
      await openWorkflowPublishModal(definitionId, definitionName);
    }
  });

  shell.appendChild(menu);
  workflowLibraryActiveMenu = menu;
}

function closeWorkflowLibraryMenu() {
  workflowLibraryActiveMenu?.remove();
  workflowLibraryActiveMenu = null;
}

async function openWorkflowHistoryModal(definitionId, definitionName) {
  try {
    const versions = await workflowLibraryRequest(`/api/workflow-definitions/${definitionId}/versions`);
    const items = Array.isArray(versions) ? versions : [];
    openWorkflowLibraryModal(`
      <h3>${escapeHtml(definitionName)} history</h3>
      <p>Published versions for this workflow definition.</p>
      <div class="host-version-list">
        ${items.length === 0
          ? '<div class="host-version-item"><strong>No published versions yet.</strong><span>This workflow currently exists only as a draft.</span></div>'
          : items.map((item) => `
              <article class="host-version-item">
                <strong>v${escapeHtml(String(item.versionNumber ?? '-'))} ${escapeHtml(String(item.status ?? ''))}</strong>
                <span>Published: ${escapeHtml(formatHostDate(item.publishedAtUtc || item.effectiveFromUtc))}</span>
                <span>${escapeHtml(item.publishMessage || 'No publish message recorded.')}</span>
              </article>
            `).join('')}
      </div>
      <div class="host-modal-actions">
        <button type="button" data-close-modal="true">Close</button>
      </div>
    `);
  } catch (error) {
    openWorkflowLibraryModal(`
      <h3>${escapeHtml(definitionName)} history</h3>
      <p>${escapeHtml(error instanceof Error ? error.message : 'Unable to load workflow history.')}</p>
      <div class="host-modal-actions">
        <button type="button" data-close-modal="true">Close</button>
      </div>
    `);
  }
}

async function openWorkflowPublishModal(definitionId, definitionName) {
  const definitions = await workflowLibraryRequest('/api/workflow-definitions');
  const definition = Array.isArray(definitions)
    ? definitions.find((item) => String(item.id) === definitionId)
    : null;

  if (!definition) {
    openWorkflowLibraryModal(`
      <h3>Publish workflow</h3>
      <p>Unable to load the latest workflow draft metadata for publishing.</p>
      <div class="host-modal-actions">
        <button type="button" data-close-modal="true">Close</button>
      </div>
    `);
    return;
  }

  openWorkflowLibraryModal(`
    <h3>Publish ${escapeHtml(definitionName)}</h3>
    <p>Set the effective window and add a release note for this version.</p>
    <div class="host-form-grid">
      <label>
        <span>Effective from</span>
        <input type="datetime-local" data-publish-from="true" />
      </label>
      <label>
        <span>Effective to</span>
        <input type="datetime-local" data-publish-to="true" />
      </label>
      <label>
        <span>Publish message</span>
        <textarea data-publish-message="true" placeholder="Summarize what changed in this release."></textarea>
      </label>
    </div>
    <div class="host-modal-actions">
      <button type="button" data-close-modal="true">Cancel</button>
      <button type="button" data-submit-publish="true">Publish</button>
    </div>
  `);

  const submitButton = workflowLibraryActiveModal?.querySelector('[data-submit-publish="true"]');
  if (submitButton instanceof HTMLButtonElement) {
    submitButton.addEventListener('click', async () => {
      submitButton.disabled = true;
      try {
        const fromInput = workflowLibraryActiveModal?.querySelector('[data-publish-from="true"]');
        const toInput = workflowLibraryActiveModal?.querySelector('[data-publish-to="true"]');
        const messageInput = workflowLibraryActiveModal?.querySelector('[data-publish-message="true"]');
        const payload = {
          effectiveFromUtc: fromInput instanceof HTMLInputElement && fromInput.value ? new Date(fromInput.value).toISOString() : null,
          effectiveToUtc: toInput instanceof HTMLInputElement && toInput.value ? new Date(toInput.value).toISOString() : null,
          publishMessage: messageInput instanceof HTMLTextAreaElement && messageInput.value.trim() ? messageInput.value.trim() : null,
          definitionRowVersion: definition.definitionRowVersion ?? null,
          draftRowVersion: definition.draftRowVersion ?? null,
        };

        await workflowLibraryRequest(`/api/workflow-definitions/${definitionId}/publish`, {
          method: 'POST',
          body: JSON.stringify(payload),
        });

        closeWorkflowLibraryModal();
        window.location.reload();
      } catch (error) {
        submitButton.disabled = false;
        alert(error instanceof Error ? error.message : 'Unable to publish workflow.');
      }
    });
  }
}

function openWorkflowLibraryModal(content) {
  closeWorkflowLibraryModal();

  const backdrop = document.createElement('div');
  backdrop.className = 'workflow-library-host-backdrop';
  backdrop.innerHTML = `<div class="workflow-library-host-modal">${content}</div>`;

  workflowLibraryActiveModal = backdrop;
  document.body.appendChild(backdrop);

  const closeButtons = backdrop.querySelectorAll('[data-close-modal="true"]');
  closeButtons.forEach((button) => {
    button.addEventListener('click', () => closeWorkflowLibraryModal());
  });

  workflowLibraryOverlayHandler = (event) => {
    if (event.target === backdrop) {
      closeWorkflowLibraryModal();
    }
  };

  backdrop.addEventListener('click', workflowLibraryOverlayHandler);
}

function closeWorkflowLibraryModal() {
  if (workflowLibraryActiveModal && workflowLibraryOverlayHandler) {
    workflowLibraryActiveModal.removeEventListener('click', workflowLibraryOverlayHandler);
  }

  workflowLibraryOverlayHandler = null;
  workflowLibraryActiveModal?.remove();
  workflowLibraryActiveModal = null;
}

async function workflowLibraryRequest(path, options = {}) {
  const session = window.logicFlowWorkflowHost?.session;
  if (!session?.accessToken) {
    throw new Error('Workflow session is not available.');
  }

  const baseUrl = session.apiBaseUrl ?? '';
  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${session.accessToken}`,
      ...(options.headers ?? {}),
    },
  });

  const payload = await response.json();
  if (!response.ok || !payload?.succeeded) {
    throw new Error(payload?.message || `Request failed with ${response.status}.`);
  }

  return payload.data;
}

function escapeHtml(value) {
  return String(value ?? '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

function formatHostDate(value) {
  if (!value) {
    return '-';
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '-' : date.toLocaleString();
}

export async function mount(hostElement) {
  await ensureWorkflowDesignerStylesheet();
  const moduleUrl = getModuleUrl();
  workflowDesignerModule ??= await import(moduleUrl);
  window.logicFlowWorkflowDesigner?.mount(hostElement);
  applyWorkflowLibraryFixes();
}

export function unmount() {
  removeWorkflowLibraryFixes();
  window.logicFlowWorkflowDesigner?.unmount();
}
