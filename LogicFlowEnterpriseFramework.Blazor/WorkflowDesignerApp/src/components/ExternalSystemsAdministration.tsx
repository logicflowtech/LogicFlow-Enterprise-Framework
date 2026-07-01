import { useEffect, useMemo, useState } from 'react'
import type {
  ExternalApiEndpoint,
  ExternalApiEndpointTestResult,
  ExternalApiEndpointTestInput,
  ExternalApiTestRun,
  ExternalDatabaseConnection,
  ExternalDatabaseIntegration,
  ExternalDatabaseConnectionTestResult,
  ExternalDatabaseObject,
  ExternalDatabaseObjectDetail,
  ExternalDatabaseSchema,
  ExternalDatabaseIntegrationPreviewResult,
  ExternalSystem,
  UpsertExternalApiEndpointInput,
  UpsertExternalDatabaseConnectionInput,
  UpsertExternalDatabaseIntegrationInput,
  UpsertExternalSystemInput,
} from '../api'
import { formatDate } from '../format'
import { ToastStack } from './ToastStack'

type ProcedureParameterField = {
  parameterName: string
  dataType: string
  hasDefaultValue: boolean
  isOutput: boolean
  value: string
}

type OutputMappingRow = {
  sourceColumn: string
  targetVariable: string
}

type DatabaseAccessDetailsForm = {
  host: string
  port: string
  databaseName: string
  userName: string
  password: string
  authMode: 'SqlLogin' | 'Integrated'
  encrypt: boolean
  trustServerCertificate: boolean
  filePath: string
  raw: string
}

type ExternalSystemsAdministrationProps = {
  busy: boolean
  databaseConnections: ExternalDatabaseConnection[]
  databaseIntegrations: ExternalDatabaseIntegration[]
  endpoints: ExternalApiEndpoint[]
  systems: ExternalSystem[]
  onRefresh: () => void
  onSaveSystem: (payload: UpsertExternalSystemInput) => Promise<void>
  onSaveEndpoint: (payload: UpsertExternalApiEndpointInput) => Promise<void>
  onSaveDatabaseConnection: (payload: UpsertExternalDatabaseConnectionInput) => Promise<void>
  onSaveDatabaseIntegration: (payload: UpsertExternalDatabaseIntegrationInput) => Promise<void>
  onTestDatabaseConnection: (connectionId: string) => Promise<void>
  onLoadDatabaseSchemas: (connectionId: string) => Promise<ExternalDatabaseSchema[]>
  onLoadDatabaseObjects: (connectionId: string, schema?: string, type?: string) => Promise<ExternalDatabaseObject[]>
  onLoadDatabaseObjectDetail: (connectionId: string, schema: string, objectName: string) => Promise<ExternalDatabaseObjectDetail | null>
  databaseTestResult: ExternalDatabaseConnectionTestResult | null
  onPreviewDatabaseIntegration: (integrationId: string) => Promise<void>
  databasePreviewResult: ExternalDatabaseIntegrationPreviewResult | null
  onTestEndpoint: (endpointId: string, payload: ExternalApiEndpointTestInput) => Promise<void>
  testResult: ExternalApiEndpointTestResult | null
  testHistory: ExternalApiTestRun[]
}

const testTokenDefaults: Record<string, string> = {
  'workflow.id': 'WF-20260628-001',
  'variables.employeeId': 'EMP-1001',
  'variables.amount': '12500.50',
  'currentUser.id': 'USR-ADMIN-01',
  'requester.id': 'REQ-7788',
  'utcNow()': '2026-06-28T10:30:00Z',
}

const emptySystem: UpsertExternalSystemInput = {
  id: '',
  code: '',
  name: '',
  systemType: 'HR',
  baseUrl: '',
  status: 'Active',
}

const emptyEndpoint: UpsertExternalApiEndpointInput = {
  id: '',
  externalSystemId: '',
  code: '',
  name: '',
  relativeUrl: '',
  httpMethod: 'GET',
  authMode: 'None',
  authHeaderName: '',
  authSecretProvider: 'ProtectedDatabase',
  authSecretReference: '',
  authSecret: '',
  headersTemplate: '',
  requestBodyTemplate: '',
  timeoutSeconds: 30,
  status: 'Active',
}

const emptyDatabaseConnection: UpsertExternalDatabaseConnectionInput = {
  id: '',
  externalSystemId: '',
  code: '',
  name: '',
  databaseProvider: 'SqlServer',
  connectionStringSecretProvider: 'ProtectedDatabase',
  connectionStringSecretReference: '',
  connectionString: '',
  notes: '',
  timeoutSeconds: 30,
  status: 'Active',
}

const emptyDatabaseIntegration: UpsertExternalDatabaseIntegrationInput = {
  id: '',
  externalDatabaseConnectionId: '',
  code: '',
  name: '',
  purpose: 'Lookup',
  sourceType: 'Table',
  schemaName: 'dbo',
  objectName: '',
  primaryKeyColumn: '',
  displayColumn: '',
  lastUpdatedColumn: '',
  softDeleteColumn: '',
  allowReadOne: true,
  allowReadMany: true,
  allowInsert: false,
  allowUpdate: false,
  allowUpsert: false,
  allowExecute: false,
  inputMappingJson: '',
  outputMappingJson: '',
  filterTemplate: '',
  auditAccess: true,
  maskedColumnsJson: '',
  restrictedColumnsJson: '',
  notes: '',
  status: 'Active',
}

const emptyDatabaseAccessDetails: DatabaseAccessDetailsForm = {
  host: '',
  port: '',
  databaseName: '',
  userName: '',
  password: '',
  authMode: 'SqlLogin',
  encrypt: true,
  trustServerCertificate: true,
  filePath: '',
  raw: '',
}

export function ExternalSystemsAdministration({
  busy,
  databaseConnections,
  databaseIntegrations,
  endpoints,
  systems,
  onRefresh,
  onSaveSystem,
  onSaveEndpoint,
  onSaveDatabaseConnection,
  onSaveDatabaseIntegration,
  onTestDatabaseConnection,
  onLoadDatabaseSchemas,
  onLoadDatabaseObjects,
  onLoadDatabaseObjectDetail,
  databaseTestResult,
  onPreviewDatabaseIntegration,
  databasePreviewResult,
  onTestEndpoint,
  testResult,
  testHistory,
}: ExternalSystemsAdministrationProps) {
  const [search, setSearch] = useState('')
  const [systemFilter, setSystemFilter] = useState('All')
  const [systemSearch, setSystemSearch] = useState('')
  const [isSystemModalOpen, setIsSystemModalOpen] = useState(false)
  const [isEndpointModalOpen, setIsEndpointModalOpen] = useState(false)
  const [isDatabaseModalOpen, setIsDatabaseModalOpen] = useState(false)
  const [isDatabaseIntegrationModalOpen, setIsDatabaseIntegrationModalOpen] = useState(false)
  const [isDatabaseObjectBrowserOpen, setIsDatabaseObjectBrowserOpen] = useState(false)
  const [isDatabasePreviewOpen, setIsDatabasePreviewOpen] = useState(false)
  const [systemForm, setSystemForm] = useState<UpsertExternalSystemInput>(emptySystem)
  const [endpointForm, setEndpointForm] = useState<UpsertExternalApiEndpointInput>(emptyEndpoint)
  const [databaseForm, setDatabaseForm] = useState<UpsertExternalDatabaseConnectionInput>(emptyDatabaseConnection)
  const [databaseAccessDetails, setDatabaseAccessDetails] = useState<DatabaseAccessDetailsForm>(emptyDatabaseAccessDetails)
  const [databaseIntegrationForm, setDatabaseIntegrationForm] = useState<UpsertExternalDatabaseIntegrationInput>(emptyDatabaseIntegration)
  const [endpointHasStoredSecret, setEndpointHasStoredSecret] = useState(false)
  const [databaseHasStoredSecret, setDatabaseHasStoredSecret] = useState(false)
  const [showAuthSecret, setShowAuthSecret] = useState(false)
  const [showDatabaseSecret, setShowDatabaseSecret] = useState(false)
  const [showAdvancedDatabaseAccess, setShowAdvancedDatabaseAccess] = useState(false)
  const [databaseObjectTypeFilter, setDatabaseObjectTypeFilter] = useState('All')
  const [databaseObjectSearch, setDatabaseObjectSearch] = useState('')
  const [databaseSchemas, setDatabaseSchemas] = useState<ExternalDatabaseSchema[]>([])
  const [databaseObjects, setDatabaseObjects] = useState<ExternalDatabaseObject[]>([])
  const [selectedDatabaseObjectDetail, setSelectedDatabaseObjectDetail] = useState<ExternalDatabaseObjectDetail | null>(null)
  const [procedureParameters, setProcedureParameters] = useState<ProcedureParameterField[]>([])
  const [showAdvancedIntegrationSettings, setShowAdvancedIntegrationSettings] = useState(false)
  const [testTarget, setTestTarget] = useState<ExternalApiEndpoint | null>(null)
  const [isTestResultOpen, setIsTestResultOpen] = useState(false)
  const [testRelativeUrl, setTestRelativeUrl] = useState('')
  const [testHeaders, setTestHeaders] = useState('')
  const [testBody, setTestBody] = useState('')
  const [testTokens, setTestTokens] = useState<Record<string, string>>(testTokenDefaults)
  const [isDatabaseTestToastDismissed, setIsDatabaseTestToastDismissed] = useState(false)
  const usesEnvironmentDatabaseSecret = databaseForm.connectionStringSecretProvider === 'EnvironmentVariable'

  useEffect(() => {
    if (!isSystemModalOpen) {
      setSystemForm(emptySystem)
    }
  }, [isSystemModalOpen])

  useEffect(() => {
    if (!isEndpointModalOpen) {
      setEndpointForm({
        ...emptyEndpoint,
        externalSystemId: systems[0]?.id ?? '',
      })
      setEndpointHasStoredSecret(false)
      setShowAuthSecret(false)
    }
  }, [isEndpointModalOpen, systems])

  useEffect(() => {
    if (!isDatabaseModalOpen) {
      setDatabaseForm({
        ...emptyDatabaseConnection,
        externalSystemId: systems[0]?.id ?? '',
      })
      setDatabaseAccessDetails(emptyDatabaseAccessDetails)
      setDatabaseHasStoredSecret(false)
      setShowDatabaseSecret(false)
      setShowAdvancedDatabaseAccess(false)
    }
  }, [isDatabaseModalOpen, systems])

  useEffect(() => {
    setIsDatabaseTestToastDismissed(false)
  }, [databaseTestResult?.isSuccess, databaseTestResult?.message, databaseTestResult?.databaseName, databaseTestResult?.databaseProvider])

  useEffect(() => {
    if (!isDatabaseIntegrationModalOpen) {
      setDatabaseIntegrationForm({
        ...emptyDatabaseIntegration,
        externalDatabaseConnectionId: databaseConnections[0]?.id ?? '',
      })
      setProcedureParameters([])
      setSelectedDatabaseObjectDetail(null)
      setShowAdvancedIntegrationSettings(false)
    }
  }, [databaseConnections, isDatabaseIntegrationModalOpen])

  useEffect(() => {
    if (!isDatabaseIntegrationModalOpen) {
      return
    }

    if (
      !databaseIntegrationForm.externalDatabaseConnectionId
      || !databaseIntegrationForm.schemaName
      || !databaseIntegrationForm.objectName
    ) {
      setSelectedDatabaseObjectDetail(null)
      setProcedureParameters([])
      return
    }

    let isCancelled = false

    void onLoadDatabaseObjectDetail(
      databaseIntegrationForm.externalDatabaseConnectionId,
      databaseIntegrationForm.schemaName,
      databaseIntegrationForm.objectName,
    ).then((detail) => {
      if (isCancelled || !detail) return
      setSelectedDatabaseObjectDetail(detail)
      if (databaseIntegrationForm.sourceType === 'StoredProcedure') {
        setProcedureParameters(buildProcedureParameterFields(detail, databaseIntegrationForm.inputMappingJson ?? ''))
      }
    })

    return () => {
      isCancelled = true
    }
  }, [
    databaseIntegrationForm.externalDatabaseConnectionId,
    databaseIntegrationForm.objectName,
    databaseIntegrationForm.schemaName,
    databaseIntegrationForm.sourceType,
    isDatabaseIntegrationModalOpen,
    onLoadDatabaseObjectDetail,
  ])

  useEffect(() => {
    if (databaseIntegrationForm.sourceType !== 'StoredProcedure' || !selectedDatabaseObjectDetail) {
      setProcedureParameters([])
      return
    }

    setProcedureParameters(buildProcedureParameterFields(selectedDatabaseObjectDetail, databaseIntegrationForm.inputMappingJson ?? ''))
  }, [databaseIntegrationForm.inputMappingJson, databaseIntegrationForm.sourceType, selectedDatabaseObjectDetail])

  const filteredSystems = useMemo(
    () => systems.filter((system) => {
      const term = systemSearch.trim().toLowerCase()
      return !term
        || system.code.toLowerCase().includes(term)
        || system.name.toLowerCase().includes(term)
        || system.systemType.toLowerCase().includes(term)
    }),
    [systemSearch, systems],
  )

  const filteredEndpoints = useMemo(
    () => endpoints.filter((endpoint) => {
      const term = search.trim().toLowerCase()
      const matchesSystem = systemFilter === 'All' || endpoint.externalSystemId === systemFilter
      const matchesSearch = !term
        || endpoint.name.toLowerCase().includes(term)
        || endpoint.code.toLowerCase().includes(term)
        || endpoint.relativeUrl.toLowerCase().includes(term)
        || endpoint.httpMethod.toLowerCase().includes(term)

      return matchesSystem && matchesSearch
    }),
    [endpoints, search, systemFilter],
  )

  const filteredDatabaseConnections = useMemo(
    () => databaseConnections.filter((connection) => {
      const term = search.trim().toLowerCase()
      const matchesSystem = systemFilter === 'All' || connection.externalSystemId === systemFilter
      const matchesSearch = !term
        || connection.name.toLowerCase().includes(term)
        || connection.code.toLowerCase().includes(term)
        || connection.databaseProvider.toLowerCase().includes(term)

      return matchesSystem && matchesSearch
    }),
    [databaseConnections, search, systemFilter],
  )

  const filteredDatabaseIntegrations = useMemo(
    () => databaseIntegrations.filter((integration) => {
      const term = search.trim().toLowerCase()
      const connection = databaseConnections.find((item) => item.id === integration.externalDatabaseConnectionId)
      const matchesSystem = systemFilter === 'All' || connection?.externalSystemId === systemFilter
      const matchesSearch = !term
        || integration.name.toLowerCase().includes(term)
        || integration.code.toLowerCase().includes(term)
        || integration.sourceType.toLowerCase().includes(term)
        || (integration.objectName ?? '').toLowerCase().includes(term)

      return matchesSystem && matchesSearch
    }),
    [databaseConnections, databaseIntegrations, search, systemFilter],
  )

  const visibleDatabaseObjects = useMemo(
    () => databaseObjects.filter((item) => {
      const term = databaseObjectSearch.trim().toLowerCase()
      return !term
        || item.objectName.toLowerCase().includes(term)
        || item.schemaName.toLowerCase().includes(term)
        || item.objectType.toLowerCase().includes(term)
    }),
    [databaseObjectSearch, databaseObjects],
  )

  function openCreateSystem() {
    setSystemForm(emptySystem)
    setIsSystemModalOpen(true)
  }

  function openEditSystem(system: ExternalSystem) {
    setSystemForm({
      id: system.id,
      code: system.code,
      name: system.name,
      systemType: system.systemType,
      baseUrl: system.baseUrl ?? '',
      status: system.status,
    })
    setIsSystemModalOpen(true)
  }

  function openCreateEndpoint(systemId?: string) {
    setEndpointForm({
      ...emptyEndpoint,
      externalSystemId: systemId ?? systems[0]?.id ?? '',
    })
    setEndpointHasStoredSecret(false)
    setIsEndpointModalOpen(true)
  }

  function openEditEndpoint(endpoint: ExternalApiEndpoint) {
    setEndpointForm({
      id: endpoint.id,
      externalSystemId: endpoint.externalSystemId,
      code: endpoint.code,
      name: endpoint.name,
      relativeUrl: endpoint.relativeUrl,
      httpMethod: endpoint.httpMethod,
      authMode: endpoint.authMode,
      authHeaderName: endpoint.authHeaderName ?? '',
      authSecretProvider: endpoint.authSecretProvider || 'ProtectedDatabase',
      authSecretReference: endpoint.authSecretReference ?? '',
      authSecret: '',
      headersTemplate: endpoint.headersTemplate ?? '',
      requestBodyTemplate: endpoint.requestBodyTemplate ?? '',
      timeoutSeconds: endpoint.timeoutSeconds,
      status: endpoint.status,
    })
    setEndpointHasStoredSecret(endpoint.hasAuthSecret)
    setIsEndpointModalOpen(true)
  }

  async function handleSubmitSystem() {
    await onSaveSystem(systemForm)
    setIsSystemModalOpen(false)
  }

  async function handleSubmitEndpoint() {
    await onSaveEndpoint(endpointForm)
    setIsEndpointModalOpen(false)
  }

  function openCreateDatabaseConnection(systemId?: string) {
    setDatabaseForm({
      ...emptyDatabaseConnection,
      externalSystemId: systemId ?? systems[0]?.id ?? '',
    })
    setDatabaseAccessDetails(emptyDatabaseAccessDetails)
    setDatabaseHasStoredSecret(false)
    setShowAdvancedDatabaseAccess(false)
    setIsDatabaseModalOpen(true)
  }

  function openEditDatabaseConnection(connection: ExternalDatabaseConnection) {
    setDatabaseForm({
      id: connection.id,
      externalSystemId: connection.externalSystemId,
      code: connection.code,
      name: connection.name,
      databaseProvider: connection.databaseProvider,
      connectionStringSecretProvider: connection.connectionStringSecretProvider || 'ProtectedDatabase',
      connectionStringSecretReference: connection.connectionStringSecretReference ?? '',
      connectionString: '',
      notes: connection.notes ?? '',
      timeoutSeconds: connection.timeoutSeconds,
      status: connection.status,
    })
    setDatabaseAccessDetails(emptyDatabaseAccessDetails)
    setDatabaseHasStoredSecret(connection.hasConnectionString)
    setShowAdvancedDatabaseAccess(false)
    setIsDatabaseModalOpen(true)
  }

  async function handleSubmitDatabaseConnection() {
    await onSaveDatabaseConnection({
      ...databaseForm,
      connectionString: buildDatabaseAccessDetailsValue(databaseForm.databaseProvider, databaseAccessDetails),
    })
    setIsDatabaseModalOpen(false)
  }

  function handleDatabaseAccessDetailChange<K extends keyof DatabaseAccessDetailsForm>(
    key: K,
    value: DatabaseAccessDetailsForm[K],
  ) {
    setDatabaseAccessDetails((current) => ({ ...current, [key]: value }))
  }

  function handleAdvancedDatabaseAccessChange(value: string) {
    setDatabaseAccessDetails({
      ...parseDatabaseAccessDetails(value, databaseForm.databaseProvider),
      raw: value,
    })
  }

  async function handleTestDatabaseConnection(connection: ExternalDatabaseConnection) {
    await onTestDatabaseConnection(connection.id)
  }

  function openCreateDatabaseIntegration(connectionId?: string) {
    setDatabaseIntegrationForm({
      ...emptyDatabaseIntegration,
      externalDatabaseConnectionId: connectionId ?? databaseConnections[0]?.id ?? '',
    })
    setIsDatabaseIntegrationModalOpen(true)
  }

  function openEditDatabaseIntegration(integration: ExternalDatabaseIntegration) {
    setDatabaseIntegrationForm({
      id: integration.id,
      externalDatabaseConnectionId: integration.externalDatabaseConnectionId,
      code: integration.code,
      name: integration.name,
      purpose: integration.purpose,
      sourceType: integration.sourceType,
      schemaName: integration.schemaName ?? '',
      objectName: integration.objectName ?? '',
      primaryKeyColumn: integration.primaryKeyColumn ?? '',
      displayColumn: integration.displayColumn ?? '',
      lastUpdatedColumn: integration.lastUpdatedColumn ?? '',
      softDeleteColumn: integration.softDeleteColumn ?? '',
      allowReadOne: integration.allowReadOne,
      allowReadMany: integration.allowReadMany,
      allowInsert: integration.allowInsert,
      allowUpdate: integration.allowUpdate,
      allowUpsert: integration.allowUpsert,
      allowExecute: integration.allowExecute,
      inputMappingJson: integration.inputMappingJson ?? '',
      outputMappingJson: integration.outputMappingJson ?? '',
      filterTemplate: integration.filterTemplate ?? '',
      auditAccess: integration.auditAccess,
      maskedColumnsJson: integration.maskedColumnsJson ?? '',
      restrictedColumnsJson: integration.restrictedColumnsJson ?? '',
      notes: integration.notes ?? '',
      status: integration.status,
    })
    setIsDatabaseIntegrationModalOpen(true)
  }

  async function handleSubmitDatabaseIntegration() {
    await onSaveDatabaseIntegration(databaseIntegrationForm)
    setIsDatabaseIntegrationModalOpen(false)
  }

  async function openDatabaseObjectBrowser() {
    if (!databaseIntegrationForm.externalDatabaseConnectionId) return
    const [schemas, objects] = await Promise.all([
      onLoadDatabaseSchemas(databaseIntegrationForm.externalDatabaseConnectionId),
      onLoadDatabaseObjects(databaseIntegrationForm.externalDatabaseConnectionId, databaseIntegrationForm.schemaName || undefined, databaseObjectTypeFilter),
    ])
    setDatabaseSchemas(schemas)
    setDatabaseObjects(objects)
    setSelectedDatabaseObjectDetail(null)
    setIsDatabaseObjectBrowserOpen(true)
  }

  async function refreshDatabaseObjects(schema?: string, type = databaseObjectTypeFilter) {
    if (!databaseIntegrationForm.externalDatabaseConnectionId) return
    const objects = await onLoadDatabaseObjects(databaseIntegrationForm.externalDatabaseConnectionId, schema, type)
    setDatabaseObjects(objects)
  }

  async function selectDatabaseObject(item: ExternalDatabaseObject) {
    if (!databaseIntegrationForm.externalDatabaseConnectionId) return
    setDatabaseIntegrationForm((current) => ({
      ...current,
      schemaName: item.schemaName,
      objectName: item.objectName,
      sourceType: item.objectType === 'StoredProcedure' ? 'StoredProcedure' : item.objectType,
    }))
    const detail = await onLoadDatabaseObjectDetail(databaseIntegrationForm.externalDatabaseConnectionId, item.schemaName, item.objectName)
    setSelectedDatabaseObjectDetail(detail)
    setProcedureParameters(detail ? buildProcedureParameterFields(detail, databaseIntegrationForm.inputMappingJson ?? '') : [])
  }

  function applySelectedDatabaseObject() {
    if (!selectedDatabaseObjectDetail) return
    const primaryKey = selectedDatabaseObjectDetail.columns.find((column) => column.isPrimaryKey)?.columnName ?? ''
    const displayColumn = selectedDatabaseObjectDetail.columns.find((column) =>
      ['name', 'title', 'description', 'fullname'].includes(column.columnName.toLowerCase()))?.columnName ?? ''

    setDatabaseIntegrationForm((current) => ({
      ...current,
      schemaName: selectedDatabaseObjectDetail.schemaName,
      objectName: selectedDatabaseObjectDetail.objectName,
      sourceType: selectedDatabaseObjectDetail.objectType,
      primaryKeyColumn: current.primaryKeyColumn || primaryKey,
      displayColumn: current.displayColumn || displayColumn,
      allowExecute: selectedDatabaseObjectDetail.objectType === 'StoredProcedure' ? true : current.allowExecute,
      inputMappingJson: selectedDatabaseObjectDetail.objectType === 'StoredProcedure'
        ? buildProcedureInputMappingJson(procedureParameters)
        : current.inputMappingJson,
    }))
    setIsDatabaseObjectBrowserOpen(false)
  }

  function handleProcedureParameterChange(parameterName: string, value: string) {
    setProcedureParameters((current) => {
      const next = current.map((item) => (
        item.parameterName === parameterName
          ? { ...item, value }
          : item
      ))
      setDatabaseIntegrationForm((form) => ({
        ...form,
        inputMappingJson: buildProcedureInputMappingJson(next),
      }))
      return next
    })
  }

  function handleInputMappingJsonChange(value: string) {
    setDatabaseIntegrationForm((current) => ({ ...current, inputMappingJson: value }))
    if (databaseIntegrationForm.sourceType === 'StoredProcedure' && selectedDatabaseObjectDetail) {
      setProcedureParameters(buildProcedureParameterFields(selectedDatabaseObjectDetail, value))
    }
  }

  function handleOutputMappingChange(index: number, field: keyof OutputMappingRow, value: string) {
    const next = [...outputMappingRows]
    next[index] = { ...next[index], [field]: value }
    setDatabaseIntegrationForm((current) => ({ ...current, outputMappingJson: buildStringMapJson(next, 'sourceColumn', 'targetVariable') }))
  }

  function addOutputMappingRow() {
    setDatabaseIntegrationForm((current) => ({
      ...current,
      outputMappingJson: buildStringMapJson(
        [...outputMappingRows, { sourceColumn: availableDatabaseColumns[0] ?? '', targetVariable: '' }],
        'sourceColumn',
        'targetVariable',
      ),
    }))
  }

  function removeOutputMappingRow(index: number) {
    const next = outputMappingRows.filter((_, rowIndex) => rowIndex !== index)
    setDatabaseIntegrationForm((current) => ({ ...current, outputMappingJson: buildStringMapJson(next, 'sourceColumn', 'targetVariable') }))
  }

  function toggleColumnSelection(field: 'maskedColumnsJson' | 'restrictedColumnsJson', columnName: string) {
    const selected = new Set(parseStringArrayJson(databaseIntegrationForm[field] ?? ''))
    if (selected.has(columnName)) {
      selected.delete(columnName)
    } else {
      selected.add(columnName)
    }

    setDatabaseIntegrationForm((current) => ({
      ...current,
      [field]: JSON.stringify([...selected], null, 2),
    }))
  }

  async function handlePreviewDatabaseIntegration(integration: ExternalDatabaseIntegration) {
    await onPreviewDatabaseIntegration(integration.id)
    setIsDatabasePreviewOpen(true)
  }

  async function handleTestEndpoint(endpoint: ExternalApiEndpoint) {
    setTestTarget(endpoint)
    setTestRelativeUrl(endpoint.relativeUrl)
    setTestHeaders(endpoint.headersTemplate ?? '')
    setTestBody(endpoint.requestBodyTemplate ?? '')
    await onTestEndpoint(endpoint.id, {
      overrideRelativeUrl: endpoint.relativeUrl,
      overrideHeadersJson: endpoint.headersTemplate ?? '',
      overrideRequestBody: endpoint.requestBodyTemplate ?? '',
      tokenValues: testTokens,
    })
    setIsTestResultOpen(true)
  }

  async function handleRunTestFromModal() {
    if (!testTarget) return

    await onTestEndpoint(testTarget.id, {
      overrideRelativeUrl: testRelativeUrl,
      overrideHeadersJson: testHeaders,
      overrideRequestBody: testBody,
      tokenValues: testTokens,
    })
  }

  function updateTestToken(key: string, value: string) {
    setTestTokens((current) => ({ ...current, [key]: value }))
  }

  function resolvePreview(template: string) {
    let output = template
    Object.entries(testTokens).forEach(([key, value]) => {
      output = output.replaceAll(`{{${key}}}`, value)
    })
    return output
  }

  const showHeaderName = endpointForm.authMode === 'ApiKey'
  const showSecret = endpointForm.authMode !== 'None'
  const usesEnvironmentSecret = endpointForm.authSecretProvider === 'EnvironmentVariable'
  const authSecretLabel = endpointForm.authMode === 'Bearer'
    ? 'Bearer token'
    : endpointForm.authMode === 'ApiKey'
      ? 'API key value'
      : 'Auth secret'
  const availableDatabaseColumns = selectedDatabaseObjectDetail?.columns.map((column) => column.columnName) ?? []
  const outputMappingRows = parseOutputMappingRows(databaseIntegrationForm.outputMappingJson ?? '')
  const maskedColumns = parseStringArrayJson(databaseIntegrationForm.maskedColumnsJson ?? '')
  const restrictedColumns = parseStringArrayJson(databaseIntegrationForm.restrictedColumnsJson ?? '')
  const databaseNameLabel = getDatabaseNameLabel(databaseForm.databaseProvider)
  const databaseHostLabel = getDatabaseHostLabel(databaseForm.databaseProvider)
  const supportsFriendlyDatabaseFields = databaseForm.databaseProvider !== 'Odbc'
  const usesFileDatabase = databaseForm.databaseProvider === 'Sqlite'

  return (
    <>
      {!isDatabaseTestToastDismissed && databaseTestResult && (
        <ToastStack
          notice={databaseTestResult.isSuccess ? `DB test succeeded for ${databaseTestResult.databaseProvider}${databaseTestResult.databaseName ? ` on ${databaseTestResult.databaseName}` : ''}.` : ''}
          error={databaseTestResult.isSuccess ? '' : `DB test failed: ${databaseTestResult.message}`}
          onDismissError={() => setIsDatabaseTestToastDismissed(true)}
          onDismissNotice={() => setIsDatabaseTestToastDismissed(true)}
        />
      )}
      <section className="work-area workflow-list-area">
        <header className="page-heading">
          <div className="workflow-library-heading-copy">
            <div className="page-eyebrow">Configuration</div>
            <h2><span className="title-icon subtle" aria-hidden="true">EX</span>External Systems</h2>
            <p>Register the source system once, then attach API and database integration setups under that shared system record.</p>
          </div>
          <div className="page-heading-meta">
            <div className="heading-stat">
              <strong>{systems.length}</strong>
              <span>Systems</span>
            </div>
            <div className="heading-stat">
              <strong>{endpoints.length}</strong>
              <span>API setups</span>
            </div>
            <div className="heading-stat">
              <strong>{databaseConnections.length}</strong>
              <span>DB setups</span>
            </div>
            <button className="primary-action workflow-create-button" type="button" onClick={openCreateSystem} disabled={busy}>
              <span className="button-label"><span className="button-icon" aria-hidden="true"><CreateIcon /></span>New system</span>
            </button>
          </div>
        </header>

        <section className="panel external-config-hero-panel">
          <div className="external-config-hero">
            <div className="external-config-hero-copy">
              <span>Integration design</span>
              <strong>Separate the system registry from endpoint setups.</strong>
              <p>
                Keep codes short, names clean, and credentials centralized so workflow nodes only select approved integrations
                instead of hardcoding URLs, tokens, or connection strings in the designer.
              </p>
            </div>
            <div className="external-config-hero-metrics">
              <div className="external-config-metric-card">
                <span>Registry pattern</span>
                <strong>{systems.filter((item) => item.status === 'Active').length} active systems</strong>
                <small>Reusable parent records for API, database, identity, and mapping configuration.</small>
              </div>
              <div className="external-config-metric-card">
                <span>API coverage</span>
                <strong>{filteredEndpoints.length} visible setups</strong>
                <small>HTTP integrations carry auth mode, headers, and payload templates.</small>
              </div>
              <div className="external-config-metric-card">
                <span>Database coverage</span>
                <strong>{filteredDatabaseConnections.length} visible setups</strong>
                <small>Database integrations store provider, timeout, and protected connection details.</small>
              </div>
            </div>
          </div>
        </section>

        <section className="panel workflow-table-panel user-directory-panel">
          <div className="panel-header workflow-library-panel-header">
            <div className="workflow-library-panel-copy">
              <h2><span className="title-icon subtle" aria-hidden="true">SY</span>System Registry</h2>
              <p>Keep external system code short and stable. Use the system record as the parent for API setups, database connections, and future integrations.</p>
            </div>
            <div className="workflow-library-panel-meta">
              <span className="workflow-panel-pill">Suggested code: 3 to 12 chars</span>
              <span className="workflow-panel-pill">Suggested name: under 60 chars</span>
              <span className="workflow-panel-pill">Use one record per upstream platform</span>
            </div>
          </div>

          <div className="list-toolbar workflow-list-toolbar workflow-library-toolbar external-systems-toolbar">
            <label className="toolbar-search-field">
              <span>Search systems</span>
              <input value={systemSearch} onChange={(event) => setSystemSearch(event.target.value)} placeholder="Search by code, name, or type" />
            </label>
            <div className="external-systems-toolbar-actions">
              <button className="secondary-action" type="button" onClick={onRefresh} disabled={busy}>
                <span className="button-label"><span className="button-icon" aria-hidden="true"><RefreshInlineIcon /></span>Refresh</span>
              </button>
              <button className="primary-action" type="button" onClick={openCreateSystem} disabled={busy}>
                <span className="button-label"><span className="button-icon" aria-hidden="true"><CreateIcon /></span>Add system</span>
              </button>
            </div>
          </div>

          <div className="workflow-table">
            <div className="workflow-table-header workflow-table-grid external-systems-grid">
              <span>Status</span>
              <span>System</span>
              <span>Domain</span>
              <span>Default API host</span>
              <span>Actions</span>
            </div>
            {filteredSystems.length === 0 && (
              <div className="empty-state-card compact">
                <div className="empty-state-icon">SY</div>
                <strong>No external systems</strong>
                <p>Create a system first so API setups and identity mappings can target a reusable integration source.</p>
              </div>
            )}
            {filteredSystems.map((system) => (
              <div key={system.id} className="workflow-table-row workflow-table-grid external-systems-grid">
                <div className="workflow-status-cell">
                  <span className={`status-pill ${system.status.toLowerCase()}`}>{system.status}</span>
                  <small>{system.code}</small>
                </div>
                <div className="row-stack workflow-name-cell">
                  <strong>{system.name}</strong>
                  <small>{system.id}</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{system.systemType}</strong>
                  <small>{endpoints.filter((item) => item.externalSystemId === system.id).length} API, {databaseConnections.filter((item) => item.externalSystemId === system.id).length} DB</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{system.baseUrl || 'Not set'}</strong>
                  <small>
                    {system.baseUrl
                      ? 'Used as the default host for API setups under this system'
                      : 'Optional parent host; API setups can still define relative routes later'}
                  </small>
                </div>
                <div className="table-action-row user-action-row">
                  <button className="table-action-button secondary-action" type="button" onClick={() => openCreateEndpoint(system.id)} disabled={busy}>
                    <span className="button-label"><span className="button-icon" aria-hidden="true"><CreateIcon /></span>Add API Setup</span>
                  </button>
                  <button className="table-action-button secondary-action" type="button" onClick={() => openCreateDatabaseConnection(system.id)} disabled={busy}>
                    <span className="button-label"><span className="button-icon" aria-hidden="true"><DatabaseIcon /></span>Add DB Setup</span>
                  </button>
                  <button className="table-action-button designer-action-button" type="button" onClick={() => openEditSystem(system)} disabled={busy}>
                    <span className="button-label"><span className="button-icon" aria-hidden="true"><EditIcon /></span>Edit</span>
                  </button>
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="panel workflow-table-panel user-directory-panel">
          <div className="panel-header workflow-library-panel-header">
            <div className="workflow-library-panel-copy">
              <h2><span className="title-icon subtle" aria-hidden="true">AP</span>API Setup Library</h2>
              <p>Each setup defines a reusable relative URL, method, auth mode, headers, and payload template for workflow integration.</p>
            </div>
            <div className="workflow-library-panel-meta">
              <span className="workflow-panel-pill">Process Task can select these setups directly</span>
              <span className="workflow-panel-pill">Bearer and API key auth can be stored here</span>
              <span className="workflow-panel-pill">Keep URLs relative to the selected system</span>
              <button className="table-action-button integration-create-button integration-create-button--api" type="button" onClick={() => openCreateEndpoint()} disabled={busy || systems.length === 0}>
                <span className="button-label"><span className="button-icon" aria-hidden="true"><CreateIcon /></span>New API Setup</span>
              </button>
            </div>
          </div>

          <div className="list-toolbar workflow-list-toolbar workflow-library-toolbar external-api-toolbar">
            <label className="toolbar-search-field">
              <span>Search API setups</span>
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search by code, name, URL, or method" />
            </label>
            <label className="toolbar-filter-field">
              <span>System</span>
              <select value={systemFilter} onChange={(event) => setSystemFilter(event.target.value)}>
                <option value="All">All</option>
                {systems.map((system) => (
                  <option key={system.id} value={system.id}>{system.code}</option>
                ))}
              </select>
            </label>
          </div>

          <div className="workflow-table">
            <div className="workflow-table-header workflow-table-grid external-api-grid">
              <span>Status</span>
              <span>API setup</span>
              <span>System</span>
              <span>Request</span>
              <span>Auth</span>
              <span>Updated</span>
              <span>Actions</span>
            </div>
            {filteredEndpoints.length === 0 && (
              <div className="empty-state-card compact">
                <div className="empty-state-icon">AP</div>
                <strong>No API setups</strong>
                <p>Create a reusable API configuration here, then select it in a workflow process task.</p>
              </div>
            )}
            {filteredEndpoints.map((endpoint) => (
              <div key={endpoint.id} className="workflow-table-row workflow-table-grid external-api-grid">
                <div className="workflow-status-cell">
                  <span className={`status-pill ${endpoint.status.toLowerCase()}`}>{endpoint.status}</span>
                  <small>{endpoint.httpMethod}</small>
                </div>
                <div className="row-stack workflow-name-cell">
                  <strong>{endpoint.name}</strong>
                  <small>{endpoint.code}</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{resolveExternalSystemName(endpoint.externalSystemId, systems)}</strong>
                  <small>{endpoint.externalSystemId}</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{endpoint.relativeUrl}</strong>
                  <small>{endpoint.timeoutSeconds}s timeout</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{endpoint.authMode}</strong>
                  <small>
                    {endpoint.hasAuthSecret
                      ? endpoint.authSecretUpdatedAtUtc
                        ? `Rotated ${formatDate(endpoint.authSecretUpdatedAtUtc)}`
                        : 'Credential stored'
                      : 'No credential stored'}
                  </small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{formatDate(endpoint.updatedAtUtc || endpoint.createdAtUtc)}</strong>
                  <small>
                    {endpoint.hasAuthSecret && endpoint.authSecretUpdatedBy
                      ? `By ${endpoint.authSecretUpdatedBy}`
                      : endpoint.authHeaderName || 'Default header'}
                  </small>
                </div>
                <div className="table-action-row user-action-row">
                  <div className="external-api-row-actions">
                    <button className="table-action-button secondary-action external-test-button" type="button" onClick={() => void handleTestEndpoint(endpoint)} disabled={busy}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><PulseIcon /></span>Test API</span>
                    </button>
                    <button className="table-action-button designer-action-button" type="button" onClick={() => openEditEndpoint(endpoint)} disabled={busy}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><EditIcon /></span>Edit</span>
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="panel workflow-table-panel user-directory-panel">
          <div className="panel-header workflow-library-panel-header">
            <div className="workflow-library-panel-copy">
              <h2><span className="title-icon subtle" aria-hidden="true">DB</span>Database Connection Library</h2>
              <p>Attach reusable database connection profiles under each external system so the integration pattern stays consistent with API setups.</p>
            </div>
            <div className="workflow-library-panel-meta">
              <span className="workflow-panel-pill">One system can have multiple database setups</span>
              <span className="workflow-panel-pill">Connection strings stay in protected configuration</span>
              <span className="workflow-panel-pill">Pick provider and timeout once</span>
              <button className="table-action-button integration-create-button integration-create-button--db" type="button" onClick={() => openCreateDatabaseConnection()} disabled={busy || systems.length === 0}>
                <span className="button-label"><span className="button-icon" aria-hidden="true"><DatabaseIcon /></span>New DB Setup</span>
              </button>
            </div>
          </div>
          <div className="workflow-table">
            <div className="workflow-table-header workflow-table-grid external-api-grid">
              <span>Status</span>
              <span>DB setup</span>
              <span>System</span>
              <span>Provider</span>
              <span>Credential</span>
              <span>Updated</span>
              <span>Actions</span>
            </div>
            {filteredDatabaseConnections.length === 0 && (
              <div className="empty-state-card compact">
                <div className="empty-state-icon">DB</div>
                <strong>No database connections</strong>
                <p>Create a reusable database access profile under a system like HR or ERP, then reference it from integration work later.</p>
              </div>
            )}
            {filteredDatabaseConnections.map((connection) => (
              <div key={connection.id} className="workflow-table-row workflow-table-grid external-api-grid">
                <div className="workflow-status-cell">
                  <span className={`status-pill ${connection.status.toLowerCase()}`}>{connection.status}</span>
                  <small>{connection.databaseProvider}</small>
                </div>
                <div className="row-stack workflow-name-cell">
                  <strong>{connection.name}</strong>
                  <small>{connection.code}</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{resolveExternalSystemName(connection.externalSystemId, systems)}</strong>
                  <small>{connection.externalSystemId}</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{connection.databaseProvider}</strong>
                  <small>{connection.timeoutSeconds}s timeout</small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{connection.hasConnectionString ? 'Managed' : 'Not stored'}</strong>
                  <small>
                    {connection.connectionStringUpdatedAtUtc
                      ? `Rotated ${formatDate(connection.connectionStringUpdatedAtUtc)}`
                      : connection.connectionStringSecretProvider || 'ProtectedDatabase'}
                  </small>
                </div>
                <div className="row-stack workflow-time-cell">
                  <strong>{formatDate(connection.updatedAtUtc || connection.createdAtUtc)}</strong>
                  <small>{connection.connectionStringUpdatedBy ? `By ${connection.connectionStringUpdatedBy}` : connection.notes || 'No notes'}</small>
                </div>
                <div className="table-action-row user-action-row">
                  <button className="table-action-button secondary-action" type="button" onClick={() => openCreateDatabaseIntegration(connection.id)} disabled={busy}>
                    <span className="button-label"><span className="button-icon" aria-hidden="true"><GridIcon /></span>Add Integration</span>
                  </button>
                  <button className="table-action-button secondary-action external-test-button" type="button" onClick={() => void handleTestDatabaseConnection(connection)} disabled={busy}>
                    <span className="button-label"><span className="button-icon" aria-hidden="true"><PulseIcon /></span>Test DB</span>
                  </button>
                  <button className="table-action-button designer-action-button" type="button" onClick={() => openEditDatabaseConnection(connection)} disabled={busy}>
                    <span className="button-label"><span className="button-icon" aria-hidden="true"><EditIcon /></span>Edit</span>
                  </button>
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="panel workflow-table-panel user-directory-panel">
          <div className="panel-header workflow-library-panel-header">
            <div className="workflow-library-panel-copy">
              <h2><span className="title-icon subtle" aria-hidden="true">DI</span>Database Integration Library</h2>
              <p>Bind database objects, allowed operations, and workflow mappings under each database connection.</p>
            </div>
            <div className="workflow-library-panel-meta">
              <span className="workflow-panel-pill">One connection can have many integrations</span>
              <span className="workflow-panel-pill">Support table, view, procedure, and query sources</span>
              <span className="workflow-panel-pill">Store workflow mappings here</span>
              <button className="table-action-button integration-create-button integration-create-button--db" type="button" onClick={() => openCreateDatabaseIntegration()} disabled={busy || databaseConnections.length === 0}>
                <span className="button-label"><span className="button-icon" aria-hidden="true"><GridIcon /></span>New DB Integration</span>
              </button>
            </div>
          </div>

          <div className="workflow-table">
            <div className="workflow-table-header workflow-table-grid external-api-grid">
              <span>Status</span>
              <span>Integration</span>
              <span>Connection</span>
              <span>Source</span>
              <span>Operations</span>
              <span>Updated</span>
              <span>Actions</span>
            </div>
            {filteredDatabaseIntegrations.length === 0 && (
              <div className="empty-state-card compact">
                <div className="empty-state-icon">DI</div>
                <strong>No database integrations</strong>
                <p>Create a database integration after a connection exists so workflows can bind to a specific table, view, procedure, or query source.</p>
              </div>
            )}
            {filteredDatabaseIntegrations.map((integration) => {
              const connection = databaseConnections.find((item) => item.id === integration.externalDatabaseConnectionId)
              const operationLabels = [
                integration.allowReadOne ? 'R1' : null,
                integration.allowReadMany ? 'RM' : null,
                integration.allowInsert ? 'I' : null,
                integration.allowUpdate ? 'U' : null,
                integration.allowUpsert ? 'UPS' : null,
                integration.allowExecute ? 'EXE' : null,
              ].filter(Boolean).join(' / ')

              return (
                <div key={integration.id} className="workflow-table-row workflow-table-grid external-api-grid">
                  <div className="workflow-status-cell">
                    <span className={`status-pill ${integration.status.toLowerCase()}`}>{integration.status}</span>
                    <small>{integration.purpose}</small>
                  </div>
                  <div className="row-stack workflow-name-cell">
                    <strong>{integration.name}</strong>
                    <small>{integration.code}</small>
                  </div>
                  <div className="row-stack workflow-time-cell">
                    <strong>{connection?.name || 'Unknown connection'}</strong>
                    <small>{connection ? resolveExternalSystemName(connection.externalSystemId, systems) : 'System unavailable'}</small>
                  </div>
                  <div className="row-stack workflow-time-cell">
                    <strong>{integration.objectName || 'Not selected'}</strong>
                    <small>{`${integration.sourceType}${integration.schemaName ? ` • ${integration.schemaName}` : ''}`}</small>
                  </div>
                  <div className="row-stack workflow-time-cell">
                    <strong>{operationLabels || 'None'}</strong>
                    <small>{integration.auditAccess ? 'Audited access' : 'Audit disabled'}</small>
                  </div>
                  <div className="row-stack workflow-time-cell">
                    <strong>{formatDate(integration.updatedAtUtc || integration.createdAtUtc)}</strong>
                    <small>{integration.filterTemplate || integration.notes || 'No mapping notes'}</small>
                  </div>
                  <div className="table-action-row user-action-row">
                    <button className="table-action-button secondary-action external-test-button" type="button" onClick={() => void handlePreviewDatabaseIntegration(integration)} disabled={busy}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><PulseIcon /></span>Preview Data</span>
                    </button>
                    <button className="table-action-button designer-action-button" type="button" onClick={() => openEditDatabaseIntegration(integration)} disabled={busy}>
                      <span className="button-label"><span className="button-icon" aria-hidden="true"><EditIcon /></span>Edit</span>
                    </button>
                  </div>
                </div>
              )
            })}
          </div>
        </section>
      </section>

      {isSystemModalOpen && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--large" role="dialog" aria-modal="true" aria-labelledby="external-system-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="external-system-title"><span className="title-icon subtle" aria-hidden="true">SY</span>External system</h3>
                <p>Define the reusable parent system first. API setups, database connections, user sync, role sync, and field mappings can all reference this entry.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsSystemModalOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="form-grid modal-section-card">
                <label className="field">
                  <span>Code</span>
                  <input
                    value={systemForm.code}
                    onChange={(event) => setSystemForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))}
                    placeholder="HRMS"
                    maxLength={20}
                  />
                  <small className="field-hint">Keep this short and stable. Suggested: 3 to 12 characters.</small>
                </label>
                <label className="field">
                  <span>Name</span>
                  <input
                    value={systemForm.name}
                    onChange={(event) => setSystemForm((current) => ({ ...current, name: event.target.value }))}
                    placeholder="Human Resource System"
                    maxLength={80}
                  />
                  <small className="field-hint">Use a concise business name. Suggested: under 60 characters.</small>
                </label>
                <label className="field">
                  <span>System type</span>
                  <select value={systemForm.systemType} onChange={(event) => setSystemForm((current) => ({ ...current, systemType: event.target.value }))}>
                    <option value="Internal">Internal</option>
                    <option value="HR">HR</option>
                    <option value="ERP">ERP</option>
                    <option value="Finance">Finance</option>
                    <option value="CRM">CRM</option>
                  </select>
                </label>
                <label className="field">
                  <span>Status</span>
                  <select value={systemForm.status} onChange={(event) => setSystemForm((current) => ({ ...current, status: event.target.value }))}>
                    <option value="Active">Active</option>
                    <option value="Inactive">Inactive</option>
                  </select>
                </label>
                <label className="field">
                  <span>Default API host</span>
                  <input
                    value={systemForm.baseUrl ?? ''}
                    onChange={(event) => setSystemForm((current) => ({ ...current, baseUrl: event.target.value }))}
                    placeholder="https://api.company.com"
                  />
                  <small className="field-hint">
                    Optional parent host for API setups created under this system. Database connections are configured separately below.
                  </small>
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => setIsSystemModalOpen(false)} disabled={busy}>Cancel</button>
                <button className="primary-action" type="button" onClick={() => void handleSubmitSystem()} disabled={busy || !systemForm.code.trim() || !systemForm.name.trim()}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><SaveIcon /></span>Save system</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}

      {isEndpointModalOpen && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--large" role="dialog" aria-modal="true" aria-labelledby="external-api-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="external-api-title"><span className="title-icon subtle" aria-hidden="true">AP</span>API setup</h3>
                <p>Store the request definition once, then let workflow process tasks reference this setup by selection.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsEndpointModalOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="modal-intro-strip external-config-modal-strip">
                <div className="modal-intro-copy">
                  <strong>Recommended setup</strong>
                  <span>
                    Put the host in the external system, keep endpoint URL relative, and store secrets here so workflow
                    designers only choose the approved API setup under the selected system.
                  </span>
                </div>
                <div className="modal-intro-metrics">
                  <div className="modal-intro-metric">
                    <span>Auth mode</span>
                    <strong>{endpointForm.authMode}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Credential</span>
                    <strong>{showSecret ? (endpointHasStoredSecret ? 'Managed' : 'Required') : 'Not used'}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Provider</span>
                    <strong>{showSecret ? (endpointForm.authSecretProvider || 'ProtectedDatabase') : 'None'}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Rotation</span>
                    <strong>
                      {endpointHasStoredSecret && endpointForm.id
                        ? resolveSecretRotationLabel(endpointForm.id, endpoints)
                        : 'Not stored'}
                    </strong>
                  </div>
                </div>
              </div>
              <div className="form-grid modal-section-card">
                <label className="field">
                  <span>External system</span>
                  <select value={endpointForm.externalSystemId} onChange={(event) => setEndpointForm((current) => ({ ...current, externalSystemId: event.target.value }))}>
                    {systems.map((system) => (
                      <option key={system.id} value={system.id}>{system.code} - {system.name}</option>
                    ))}
                  </select>
                  <small className="field-hint">Choose the parent system this API setup belongs to.</small>
                </label>
                <label className="field">
                  <span>Code</span>
                  <input
                    value={endpointForm.code}
                    onChange={(event) => setEndpointForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))}
                    maxLength={30}
                    placeholder="GET_EMPLOYEE"
                  />
                </label>
                <label className="field">
                  <span>Name</span>
                  <input
                    value={endpointForm.name}
                    onChange={(event) => setEndpointForm((current) => ({ ...current, name: event.target.value }))}
                    maxLength={100}
                    placeholder="Get employee profile"
                  />
                </label>
                <label className="field">
                  <span>Relative URL</span>
                  <input value={endpointForm.relativeUrl} onChange={(event) => setEndpointForm((current) => ({ ...current, relativeUrl: event.target.value }))} placeholder="/employees/{employeeId}" />
                </label>
                <label className="field">
                  <span>HTTP method</span>
                  <select value={endpointForm.httpMethod} onChange={(event) => setEndpointForm((current) => ({ ...current, httpMethod: event.target.value }))}>
                    <option>GET</option>
                    <option>POST</option>
                    <option>PUT</option>
                    <option>PATCH</option>
                    <option>DELETE</option>
                  </select>
                </label>
                <label className="field">
                  <span>Auth mode</span>
                  <select
                    value={endpointForm.authMode}
                    onChange={(event) => setEndpointForm((current) => ({
                      ...current,
                      authMode: event.target.value,
                      authHeaderName: event.target.value === 'ApiKey' ? (current.authHeaderName || 'X-API-Key') : '',
                    }))}
                  >
                    <option>None</option>
                    <option>Bearer</option>
                    <option>Basic</option>
                    <option>ApiKey</option>
                  </select>
                </label>
                <label className="field">
                  <span>Timeout seconds</span>
                  <input type="number" min="1" max="300" value={endpointForm.timeoutSeconds} onChange={(event) => setEndpointForm((current) => ({ ...current, timeoutSeconds: Number(event.target.value) || 30 }))} />
                </label>
                <label className="field">
                  <span>Status</span>
                  <select value={endpointForm.status} onChange={(event) => setEndpointForm((current) => ({ ...current, status: event.target.value }))}>
                    <option>Active</option>
                    <option>Inactive</option>
                  </select>
                </label>
                {showSecret && (
                  <label className="field">
                    <span>Secret provider</span>
                    <select
                      value={endpointForm.authSecretProvider ?? 'ProtectedDatabase'}
                      onChange={(event) => setEndpointForm((current) => ({ ...current, authSecretProvider: event.target.value }))}
                    >
                      <option value="ProtectedDatabase">Protected database</option>
                      <option value="EnvironmentVariable">Environment variable</option>
                    </select>
                    <small className="field-hint">Use environment variable when the secret should stay outside the application tables.</small>
                  </label>
                )}
                {showSecret && usesEnvironmentSecret && (
                  <label className="field">
                    <span>Secret reference</span>
                    <input
                      value={endpointForm.authSecretReference ?? ''}
                      onChange={(event) => setEndpointForm((current) => ({ ...current, authSecretReference: event.target.value }))}
                      placeholder="LOGICFLOW_HRMS_API_TOKEN"
                    />
                    <small className="field-hint">This should match the environment variable name on the API host.</small>
                  </label>
                )}
                {showHeaderName && (
                  <label className="field">
                    <span>Header name</span>
                    <input
                      value={endpointForm.authHeaderName ?? ''}
                      onChange={(event) => setEndpointForm((current) => ({ ...current, authHeaderName: event.target.value }))}
                      placeholder="X-API-Key"
                    />
                  </label>
                )}
                {showSecret && (
                  <div className="field">
                    <span>{authSecretLabel}</span>
                    <div className="external-secret-input">
                      <input
                        type={showAuthSecret ? 'text' : 'password'}
                        value={endpointForm.authSecret ?? ''}
                        onChange={(event) => setEndpointForm((current) => ({ ...current, authSecret: event.target.value }))}
                        placeholder={endpointHasStoredSecret ? 'Leave blank to keep existing secret' : 'Enter credential'}
                      />
                      <button className="secondary-action external-secret-toggle" type="button" onClick={() => setShowAuthSecret((current) => !current)}>
                        {showAuthSecret ? 'Hide' : 'Show'}
                      </button>
                    </div>
                    <small className="field-hint">
                      {endpointHasStoredSecret
                        ? usesEnvironmentSecret
                          ? 'The secret is resolved from the referenced environment variable. Leave this blank unless you are moving back to protected database storage.'
                          : 'A credential is already stored. Leave blank to keep it, or enter a new value to replace it.'
                        : usesEnvironmentSecret
                          ? 'Optional. Only fill this if you want to switch from database storage and initialize a new secret value now.'
                          : endpointForm.authMode === 'Bearer'
                            ? 'Store the bearer token here for this API setup.'
                            : endpointForm.authMode === 'ApiKey'
                              ? 'Store the API key value here.'
                              : 'Store the secret used by this auth mode.'}
                    </small>
                  </div>
                )}
                <label className="field">
                  <span>Headers template</span>
                  <textarea className="code-editor small publish-message-input" rows={4} value={endpointForm.headersTemplate ?? ''} onChange={(event) => setEndpointForm((current) => ({ ...current, headersTemplate: event.target.value }))} placeholder='{"X-Correlation-Id":"{{workflow.id}}"}' />
                </label>
                <label className="field">
                  <span>Request body template</span>
                  <textarea className="code-editor small publish-message-input" rows={4} value={endpointForm.requestBodyTemplate ?? ''} onChange={(event) => setEndpointForm((current) => ({ ...current, requestBodyTemplate: event.target.value }))} placeholder='{"employeeId":"{{variables.employeeId}}"}' />
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => setIsEndpointModalOpen(false)} disabled={busy}>Cancel</button>
                <button
                  className="primary-action"
                  type="button"
                  onClick={() => void handleSubmitEndpoint()}
                  disabled={
                    busy
                    || !endpointForm.externalSystemId
                    || !endpointForm.code.trim()
                    || !endpointForm.name.trim()
                    || !endpointForm.relativeUrl.trim()
                    || (showSecret && usesEnvironmentSecret && !String(endpointForm.authSecretReference ?? '').trim())
                  }
                >
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><SaveIcon /></span>Save API setup</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}

      {isDatabaseModalOpen && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--large" role="dialog" aria-modal="true" aria-labelledby="external-db-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="external-db-title"><span className="title-icon subtle" aria-hidden="true">DB</span>Database connection</h3>
                <p>Store database access details under a parent external system so the setup stays consistent across API and database integrations.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsDatabaseModalOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="modal-intro-strip external-config-modal-strip">
                <div className="modal-intro-copy">
                  <strong>Recommended setup</strong>
                  <span>
                    Keep the business system at the parent level, then register one or more database access profiles here for reporting,
                    sync, or direct integration work.
                  </span>
                </div>
                <div className="modal-intro-metrics">
                  <div className="modal-intro-metric">
                    <span>Provider</span>
                    <strong>{databaseForm.databaseProvider}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Access details</span>
                    <strong>{databaseHasStoredSecret ? 'Managed' : 'Required'}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Storage</span>
                    <strong>{databaseForm.connectionStringSecretProvider || 'ProtectedDatabase'}</strong>
                  </div>
                </div>
              </div>
              <div className="form-grid modal-section-card">
                <label className="field">
                  <span>External system</span>
                  <select value={databaseForm.externalSystemId} onChange={(event) => setDatabaseForm((current) => ({ ...current, externalSystemId: event.target.value }))}>
                    {systems.map((system) => (
                      <option key={system.id} value={system.id}>{system.code} - {system.name}</option>
                    ))}
                  </select>
                </label>
                <label className="field">
                  <span>Code</span>
                  <input
                    value={databaseForm.code}
                    onChange={(event) => setDatabaseForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))}
                    maxLength={30}
                    placeholder="HR_READONLY"
                  />
                </label>
                <label className="field">
                  <span>Name</span>
                  <input
                    value={databaseForm.name}
                    onChange={(event) => setDatabaseForm((current) => ({ ...current, name: event.target.value }))}
                    maxLength={100}
                    placeholder="HR reporting database"
                  />
                </label>
                <label className="field">
                  <span>Database provider</span>
                  <select value={databaseForm.databaseProvider} onChange={(event) => setDatabaseForm((current) => ({ ...current, databaseProvider: event.target.value }))}>
                    <option value="SqlServer">SQL Server</option>
                    <option value="PostgreSql">PostgreSQL</option>
                    <option value="MySql">MySQL</option>
                    <option value="Oracle">Oracle</option>
                    <option value="Sqlite">SQLite</option>
                    <option value="Odbc">ODBC</option>
                  </select>
                </label>
                <label className="field">
                  <span>Timeout seconds</span>
                  <input type="number" min="1" max="300" value={databaseForm.timeoutSeconds} onChange={(event) => setDatabaseForm((current) => ({ ...current, timeoutSeconds: Number(event.target.value) || 30 }))} />
                </label>
                <label className="field">
                  <span>Status</span>
                  <select value={databaseForm.status} onChange={(event) => setDatabaseForm((current) => ({ ...current, status: event.target.value }))}>
                    <option>Active</option>
                    <option>Inactive</option>
                  </select>
                </label>
                {supportsFriendlyDatabaseFields && (
                  <>
                    {usesFileDatabase ? (
                      <label className="field">
                        <span>Database file path</span>
                        <input
                          type={showDatabaseSecret ? 'text' : 'password'}
                          value={databaseAccessDetails.filePath}
                          onChange={(event) => handleDatabaseAccessDetailChange('filePath', event.target.value)}
                          placeholder="C:\\data\\hr.db"
                        />
                        <small className="field-hint">Use the local or mounted path where the SQLite database file is stored.</small>
                      </label>
                    ) : (
                      <>
                        <label className="field">
                          <span>{databaseHostLabel}</span>
                          <input
                            value={databaseAccessDetails.host}
                            onChange={(event) => handleDatabaseAccessDetailChange('host', event.target.value)}
                            placeholder={databaseForm.databaseProvider === 'Oracle' ? 'hr-db.company.local' : 'sql.company.local'}
                          />
                        </label>
                        <label className="field">
                          <span>Port</span>
                          <input
                            value={databaseAccessDetails.port}
                            onChange={(event) => handleDatabaseAccessDetailChange('port', event.target.value)}
                            placeholder={getDatabasePortPlaceholder(databaseForm.databaseProvider)}
                          />
                        </label>
                        <label className="field">
                          <span>{databaseNameLabel}</span>
                          <input
                            value={databaseAccessDetails.databaseName}
                            onChange={(event) => handleDatabaseAccessDetailChange('databaseName', event.target.value)}
                            placeholder={databaseForm.databaseProvider === 'Oracle' ? 'HRPRD' : 'HR'}
                          />
                        </label>
                        {databaseForm.databaseProvider === 'SqlServer' && (
                          <label className="field">
                            <span>Sign-in method</span>
                            <select
                              value={databaseAccessDetails.authMode}
                              onChange={(event) => handleDatabaseAccessDetailChange('authMode', event.target.value as DatabaseAccessDetailsForm['authMode'])}
                            >
                              <option value="SqlLogin">Database account</option>
                              <option value="Integrated">Integrated security</option>
                            </select>
                          </label>
                        )}
                        {databaseAccessDetails.authMode !== 'Integrated' && (
                          <>
                            <label className="field">
                              <span>User name</span>
                              <input
                                value={databaseAccessDetails.userName}
                                onChange={(event) => handleDatabaseAccessDetailChange('userName', event.target.value)}
                                placeholder="logicflow_app"
                              />
                            </label>
                            <label className="field">
                              <span>Password</span>
                              <input
                                type={showDatabaseSecret ? 'text' : 'password'}
                                value={databaseAccessDetails.password}
                                onChange={(event) => handleDatabaseAccessDetailChange('password', event.target.value)}
                                placeholder={databaseHasStoredSecret ? 'Leave blank to keep existing password' : 'Enter password'}
                              />
                            </label>
                          </>
                        )}
                        <div className="field">
                          <span>Connection options</span>
                          <div className="checkbox-grid">
                            <label>
                              <input
                                type="checkbox"
                                checked={databaseAccessDetails.encrypt}
                                onChange={(event) => handleDatabaseAccessDetailChange('encrypt', event.target.checked)}
                              />
                              Encrypt traffic
                            </label>
                            <label>
                              <input
                                type="checkbox"
                                checked={databaseAccessDetails.trustServerCertificate}
                                onChange={(event) => handleDatabaseAccessDetailChange('trustServerCertificate', event.target.checked)}
                              />
                              Trust server certificate
                            </label>
                          </div>
                        </div>
                      </>
                    )}
                  </>
                )}
                <label className="field">
                  <span>Where to keep access details</span>
                  <select
                    value={databaseForm.connectionStringSecretProvider ?? 'ProtectedDatabase'}
                    onChange={(event) => setDatabaseForm((current) => ({ ...current, connectionStringSecretProvider: event.target.value }))}
                  >
                    <option value="ProtectedDatabase">Protected database</option>
                    <option value="EnvironmentVariable">Environment variable</option>
                  </select>
                  <small className="field-hint">Use environment variable when the database access details should stay on the host server instead of application tables.</small>
                </label>
                {usesEnvironmentDatabaseSecret && (
                  <label className="field">
                    <span>Host setting name</span>
                    <input
                      value={databaseForm.connectionStringSecretReference ?? ''}
                      onChange={(event) => setDatabaseForm((current) => ({ ...current, connectionStringSecretReference: event.target.value }))}
                      placeholder="LOGICFLOW_HR_DB"
                    />
                    <small className="field-hint">This should match the environment variable name configured on the API host.</small>
                  </label>
                )}
                <div className="field">
                  <span>Advanced access details</span>
                  <div className="external-secret-input">
                    <input
                      type={showDatabaseSecret ? 'text' : 'password'}
                      value={databaseAccessDetails.raw}
                      onChange={(event) => handleAdvancedDatabaseAccessChange(event.target.value)}
                      placeholder={databaseHasStoredSecret ? 'Leave blank to keep existing access details' : 'Server=...;Database=...;'}
                    />
                    <button className="secondary-action external-secret-toggle" type="button" onClick={() => setShowDatabaseSecret((current) => !current)}>
                      {showDatabaseSecret ? 'Hide' : 'Show'}
                    </button>
                    <button className="secondary-action external-secret-toggle" type="button" onClick={() => setShowAdvancedDatabaseAccess((current) => !current)}>
                      {showAdvancedDatabaseAccess ? 'Hide advanced' : 'Show advanced'}
                    </button>
                  </div>
                  <small className="field-hint">
                    {databaseHasStoredSecret
                      ? usesEnvironmentDatabaseSecret
                        ? 'A host setting is already configured. Leave this blank unless you are rotating the value or changing where it is stored.'
                        : 'Database access details are already stored. Leave this blank to keep them, or enter a new value to replace them.'
                      : usesEnvironmentDatabaseSecret
                        ? 'Optional. Fill this only if you want to initialize the host setting value now.'
                        : 'Use this only when you need to paste the full advanced value directly.'}
                  </small>
                  {!showAdvancedDatabaseAccess && supportsFriendlyDatabaseFields && (
                    <small className="field-hint">The system will build the advanced access value from the fields above when you save.</small>
                  )}
                </div>
                {showAdvancedDatabaseAccess && (
                  <label className="field">
                    <span>Advanced value preview</span>
                    <textarea
                      className="code-editor small publish-message-input"
                      rows={4}
                      value={databaseAccessDetails.raw || buildDatabaseAccessDetailsValue(databaseForm.databaseProvider, databaseAccessDetails)}
                      onChange={(event) => handleAdvancedDatabaseAccessChange(event.target.value)}
                      placeholder="Server=sql.company.local;Database=HR;User Id=logicflow_app;Password=***;"
                    />
                  </label>
                )}
                <label className="field">
                  <span>Notes</span>
                  <textarea className="code-editor small publish-message-input" rows={4} value={databaseForm.notes ?? ''} onChange={(event) => setDatabaseForm((current) => ({ ...current, notes: event.target.value }))} placeholder="Read-only reporting replica for HR dashboards" />
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => setIsDatabaseModalOpen(false)} disabled={busy}>Cancel</button>
                <button
                  className="primary-action"
                  type="button"
                  onClick={() => void handleSubmitDatabaseConnection()}
                  disabled={
                    busy
                    || !databaseForm.externalSystemId
                    || !databaseForm.code.trim()
                    || !databaseForm.name.trim()
                    || (usesEnvironmentDatabaseSecret && !String(databaseForm.connectionStringSecretReference ?? '').trim())
                  }
                >
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><SaveIcon /></span>Save DB setup</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}

      {isDatabaseIntegrationModalOpen && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--large" role="dialog" aria-modal="true" aria-labelledby="external-db-integration-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="external-db-integration-title"><span className="title-icon subtle" aria-hidden="true">DI</span>Database integration</h3>
                <p>Bind a database object and approved operations to a managed database connection so workflow designers only choose vetted integrations.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsDatabaseIntegrationModalOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="modal-intro-strip external-config-modal-strip">
                <div className="modal-intro-copy">
                  <strong>Recommended setup</strong>
                  <span>
                    Keep object selection, operation permissions, and workflow mappings together here rather than scattering them across workflow nodes.
                  </span>
                </div>
                <div className="modal-intro-metrics">
                  <div className="modal-intro-metric">
                    <span>Purpose</span>
                    <strong>{databaseIntegrationForm.purpose}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Source</span>
                    <strong>{databaseIntegrationForm.sourceType}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Audit</span>
                    <strong>{databaseIntegrationForm.auditAccess ? 'Enabled' : 'Disabled'}</strong>
                  </div>
                </div>
              </div>
              <div className="form-grid modal-section-card">
                <label className="field">
                  <span>Database connection</span>
                  <select value={databaseIntegrationForm.externalDatabaseConnectionId} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, externalDatabaseConnectionId: event.target.value }))}>
                    {databaseConnections.map((connection) => (
                      <option key={connection.id} value={connection.id}>{connection.code} - {connection.name}</option>
                    ))}
                  </select>
                </label>
                <label className="field">
                  <span>Integration code</span>
                  <input value={databaseIntegrationForm.code} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} maxLength={30} placeholder="EMPLOYEE_MASTER" />
                </label>
                <label className="field">
                  <span>Integration name</span>
                  <input value={databaseIntegrationForm.name} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, name: event.target.value }))} maxLength={100} placeholder="Employee master lookup" />
                </label>
                <label className="field">
                  <span>Purpose</span>
                  <select value={databaseIntegrationForm.purpose} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, purpose: event.target.value }))}>
                    <option>Lookup</option>
                    <option>ReferenceData</option>
                    <option>TransactionRead</option>
                    <option>Writeback</option>
                    <option>Sync</option>
                  </select>
                </label>
                <label className="field">
                  <span>Source type</span>
                  <select value={databaseIntegrationForm.sourceType} onChange={(event) => {
                    const nextSourceType = event.target.value
                    setDatabaseIntegrationForm((current) => ({
                      ...current,
                      sourceType: nextSourceType,
                      allowExecute: nextSourceType === 'StoredProcedure' ? true : current.allowExecute,
                    }))
                    if (nextSourceType !== 'StoredProcedure') {
                      setProcedureParameters([])
                    }
                  }}>
                    <option>Table</option>
                    <option>View</option>
                    <option>StoredProcedure</option>
                    <option>SqlQuery</option>
                  </select>
                </label>
                <label className="field">
                  <span>Status</span>
                  <select value={databaseIntegrationForm.status} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, status: event.target.value }))}>
                    <option>Active</option>
                    <option>Inactive</option>
                  </select>
                </label>
                <label className="field">
                  <span>Schema</span>
                  <input value={databaseIntegrationForm.schemaName ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, schemaName: event.target.value }))} placeholder="dbo" />
                </label>
                <label className="field">
                  <span>Database object</span>
                  <div className="external-secret-input">
                    <input value={databaseIntegrationForm.objectName ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, objectName: event.target.value }))} placeholder="Employees" />
                    <button className="secondary-action external-secret-toggle" type="button" onClick={() => void openDatabaseObjectBrowser()} disabled={busy || !databaseIntegrationForm.externalDatabaseConnectionId}>
                      Select
                    </button>
                  </div>
                </label>
                <label className="field">
                  <span>Record ID column</span>
                  {availableDatabaseColumns.length > 0 ? (
                    <select value={databaseIntegrationForm.primaryKeyColumn ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, primaryKeyColumn: event.target.value }))}>
                      <option value="">Select column</option>
                      {availableDatabaseColumns.map((columnName) => (
                        <option key={columnName} value={columnName}>{columnName}</option>
                      ))}
                    </select>
                  ) : (
                    <input value={databaseIntegrationForm.primaryKeyColumn ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, primaryKeyColumn: event.target.value }))} placeholder="EmployeeId" />
                  )}
                </label>
                <label className="field">
                  <span>Display name column</span>
                  {availableDatabaseColumns.length > 0 ? (
                    <select value={databaseIntegrationForm.displayColumn ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, displayColumn: event.target.value }))}>
                      <option value="">Select column</option>
                      {availableDatabaseColumns.map((columnName) => (
                        <option key={columnName} value={columnName}>{columnName}</option>
                      ))}
                    </select>
                  ) : (
                    <input value={databaseIntegrationForm.displayColumn ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, displayColumn: event.target.value }))} placeholder="FullName" />
                  )}
                </label>
                <label className="field">
                  <span>Last updated column</span>
                  {availableDatabaseColumns.length > 0 ? (
                    <select value={databaseIntegrationForm.lastUpdatedColumn ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, lastUpdatedColumn: event.target.value }))}>
                      <option value="">Select column</option>
                      {availableDatabaseColumns.map((columnName) => (
                        <option key={columnName} value={columnName}>{columnName}</option>
                      ))}
                    </select>
                  ) : (
                    <input value={databaseIntegrationForm.lastUpdatedColumn ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, lastUpdatedColumn: event.target.value }))} placeholder="UpdatedAtUtc" />
                  )}
                </label>
                <label className="field">
                  <span>Inactive flag column</span>
                  {availableDatabaseColumns.length > 0 ? (
                    <select value={databaseIntegrationForm.softDeleteColumn ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, softDeleteColumn: event.target.value }))}>
                      <option value="">Select column</option>
                      {availableDatabaseColumns.map((columnName) => (
                        <option key={columnName} value={columnName}>{columnName}</option>
                      ))}
                    </select>
                  ) : (
                    <input value={databaseIntegrationForm.softDeleteColumn ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, softDeleteColumn: event.target.value }))} placeholder="IsDeleted" />
                  )}
                </label>
                <div className="field">
                  <span>Allowed operations</span>
                  <div className="checkbox-grid">
                    <label><input type="checkbox" checked={databaseIntegrationForm.allowReadOne} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, allowReadOne: event.target.checked }))} />Read One</label>
                    <label><input type="checkbox" checked={databaseIntegrationForm.allowReadMany} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, allowReadMany: event.target.checked }))} />Read Many</label>
                    <label><input type="checkbox" checked={databaseIntegrationForm.allowInsert} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, allowInsert: event.target.checked }))} />Insert</label>
                    <label><input type="checkbox" checked={databaseIntegrationForm.allowUpdate} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, allowUpdate: event.target.checked }))} />Update</label>
                    <label><input type="checkbox" checked={databaseIntegrationForm.allowUpsert} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, allowUpsert: event.target.checked }))} />Upsert</label>
                    <label><input type="checkbox" checked={databaseIntegrationForm.allowExecute} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, allowExecute: event.target.checked }))} />Execute</label>
                  </div>
                </div>
                <div className="field">
                  <span>Governance</span>
                  <div className="checkbox-grid">
                    <label><input type="checkbox" checked={databaseIntegrationForm.auditAccess} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, auditAccess: event.target.checked }))} />Audit Access</label>
                  </div>
                </div>
                <label className="field">
                  <span>Default filter</span>
                  <textarea className="code-editor small publish-message-input" rows={3} value={databaseIntegrationForm.filterTemplate ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, filterTemplate: event.target.value }))} placeholder="EmployeeId = {{variables.employeeId}}" />
                  <small className="field-hint">Use simple business rules here, for example EmployeeId = {'{{variables.employeeId}}'} or IsActive = 1.</small>
                </label>
                {databaseIntegrationForm.sourceType === 'StoredProcedure' && (
                  <div className="field procedure-parameter-field">
                    <span>Procedure parameters</span>
                    {procedureParameters.length === 0 ? (
                      <div className="empty-state compact">Select a stored procedure to load its parameter metadata.</div>
                    ) : (
                      <div className="procedure-parameter-grid">
                        {procedureParameters.map((parameter) => (
                          <label key={parameter.parameterName} className="field">
                            <span>{parameter.parameterName}</span>
                            <input
                              value={parameter.value}
                              onChange={(event) => handleProcedureParameterChange(parameter.parameterName, event.target.value)}
                              placeholder={parameter.isOutput ? 'Output parameter' : `Value for ${parameter.parameterName}`}
                              disabled={parameter.isOutput}
                            />
                            <small className="field-hint">
                              {parameter.dataType}
                              {parameter.hasDefaultValue ? ' • default available' : ''}
                              {parameter.isOutput ? ' • output only' : ''}
                            </small>
                          </label>
                        ))}
                      </div>
                    )}
                    <small className="field-hint">The generated parameter payload stays aligned with SQL Server procedure metadata and can still be adjusted as JSON below.</small>
                  </div>
                )}
                <label className="field">
                  <span>{databaseIntegrationForm.sourceType === 'StoredProcedure' ? 'Input mapping JSON override' : 'Input mapping JSON'}</span>
                  <textarea className="code-editor small publish-message-input" rows={4} value={databaseIntegrationForm.inputMappingJson ?? ''} onChange={(event) => handleInputMappingJsonChange(event.target.value)} placeholder='{"employeeId":"{{variables.employeeId}}"}' />
                  <small className="field-hint">For stored procedure preview, this JSON is used as the parameter payload. Keys are treated as parameter names.</small>
                </label>
                <div className="field procedure-parameter-field">
                  <span>Result mapping</span>
                  {availableDatabaseColumns.length === 0 ? (
                    <div className="empty-state compact">Select a table, view, or procedure first to map result columns.</div>
                  ) : (
                    <div className="mapping-grid">
                      {outputMappingRows.length === 0 ? (
                        <div className="empty-state compact">No result mappings yet. Add the workflow variables you want populated from this source.</div>
                      ) : (
                        outputMappingRows.map((row, index) => (
                          <div key={`${row.sourceColumn}-${index}`} className="mapping-row">
                            <select value={row.sourceColumn} onChange={(event) => handleOutputMappingChange(index, 'sourceColumn', event.target.value)}>
                              <option value="">Select column</option>
                              {availableDatabaseColumns.map((columnName) => (
                                <option key={columnName} value={columnName}>{columnName}</option>
                              ))}
                            </select>
                            <input
                              value={row.targetVariable}
                              onChange={(event) => handleOutputMappingChange(index, 'targetVariable', event.target.value)}
                              placeholder="workflow.employeeName"
                            />
                            <button className="secondary-action mapping-row-remove" type="button" onClick={() => removeOutputMappingRow(index)}>
                              Remove
                            </button>
                          </div>
                        ))
                      )}
                      <div className="mapping-actions">
                        <button className="secondary-action" type="button" onClick={addOutputMappingRow}>
                          Add result mapping
                        </button>
                      </div>
                    </div>
                  )}
                  <small className="field-hint">Map database result columns to workflow variable names without editing JSON directly.</small>
                </div>
                <div className="field procedure-parameter-field">
                  <span>Columns to mask</span>
                  {availableDatabaseColumns.length === 0 ? (
                    <div className="empty-state compact">Select an object first to choose sensitive columns.</div>
                  ) : (
                    <div className="checkbox-grid">
                      {availableDatabaseColumns.map((columnName) => (
                        <label key={`mask-${columnName}`}>
                          <input
                            type="checkbox"
                            checked={maskedColumns.includes(columnName)}
                            onChange={() => toggleColumnSelection('maskedColumnsJson', columnName)}
                          />
                          {columnName}
                        </label>
                      ))}
                    </div>
                  )}
                  <small className="field-hint">Use masking for sensitive data such as salary, national ID, or bank details.</small>
                </div>
                <div className="field procedure-parameter-field">
                  <span>Columns to hide</span>
                  {availableDatabaseColumns.length === 0 ? (
                    <div className="empty-state compact">Select an object first to choose hidden columns.</div>
                  ) : (
                    <div className="checkbox-grid">
                      {availableDatabaseColumns.map((columnName) => (
                        <label key={`restrict-${columnName}`}>
                          <input
                            type="checkbox"
                            checked={restrictedColumns.includes(columnName)}
                            onChange={() => toggleColumnSelection('restrictedColumnsJson', columnName)}
                          />
                          {columnName}
                        </label>
                      ))}
                    </div>
                  )}
                  <small className="field-hint">Hidden columns are blocked from normal workflow use, for example password hashes or internal tokens.</small>
                </div>
                <div className="field procedure-parameter-field">
                  <span>Advanced settings</span>
                  <button className="secondary-action advanced-toggle-button" type="button" onClick={() => setShowAdvancedIntegrationSettings((current) => !current)}>
                    {showAdvancedIntegrationSettings ? 'Hide advanced JSON' : 'Show advanced JSON'}
                  </button>
                </div>
                {showAdvancedIntegrationSettings && (
                  <>
                    <label className="field">
                      <span>Output mapping JSON</span>
                      <textarea className="code-editor small publish-message-input" rows={4} value={databaseIntegrationForm.outputMappingJson ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, outputMappingJson: event.target.value }))} placeholder='{"workflow.employeeName":"FullName"}' />
                    </label>
                    <label className="field">
                      <span>Masked columns JSON</span>
                      <textarea className="code-editor small publish-message-input" rows={3} value={databaseIntegrationForm.maskedColumnsJson ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, maskedColumnsJson: event.target.value }))} placeholder='["Salary","NationalId"]' />
                    </label>
                    <label className="field">
                      <span>Restricted columns JSON</span>
                      <textarea className="code-editor small publish-message-input" rows={3} value={databaseIntegrationForm.restrictedColumnsJson ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, restrictedColumnsJson: event.target.value }))} placeholder='["PasswordHash"]' />
                    </label>
                  </>
                )}
                <label className="field">
                  <span>Notes</span>
                  <textarea className="code-editor small publish-message-input" rows={4} value={databaseIntegrationForm.notes ?? ''} onChange={(event) => setDatabaseIntegrationForm((current) => ({ ...current, notes: event.target.value }))} placeholder="Primary employee master read model for approval routing" />
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => setIsDatabaseIntegrationModalOpen(false)} disabled={busy}>Cancel</button>
                <button
                  className="primary-action"
                  type="button"
                  onClick={() => void handleSubmitDatabaseIntegration()}
                  disabled={busy || !databaseIntegrationForm.externalDatabaseConnectionId || !databaseIntegrationForm.code.trim() || !databaseIntegrationForm.name.trim()}
                >
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><SaveIcon /></span>Save DB Integration</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}

      {isDatabaseObjectBrowserOpen && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--large" role="dialog" aria-modal="true" aria-labelledby="database-object-browser-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="database-object-browser-title"><span className="title-icon subtle" aria-hidden="true">OB</span>Select Database Object</h3>
                <p>Browse schemas and choose a table, view, or procedure from the selected connection.</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsDatabaseObjectBrowserOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="list-toolbar workflow-list-toolbar workflow-library-toolbar external-api-toolbar">
                <label className="toolbar-filter-field">
                  <span>Schema</span>
                  <select
                    value={databaseIntegrationForm.schemaName ?? ''}
                    onChange={(event) => {
                      const schema = event.target.value
                      setDatabaseIntegrationForm((current) => ({ ...current, schemaName: schema }))
                      void refreshDatabaseObjects(schema || undefined, databaseObjectTypeFilter)
                    }}
                  >
                    <option value="">All</option>
                    {databaseSchemas.map((schema) => (
                      <option key={schema.schemaName} value={schema.schemaName}>{schema.schemaName}</option>
                    ))}
                  </select>
                </label>
                <label className="toolbar-filter-field">
                  <span>Object type</span>
                  <select
                    value={databaseObjectTypeFilter}
                    onChange={(event) => {
                      const nextType = event.target.value
                      setDatabaseObjectTypeFilter(nextType)
                      void refreshDatabaseObjects(databaseIntegrationForm.schemaName || undefined, nextType)
                    }}
                  >
                    <option value="All">All</option>
                    <option value="Table">Table</option>
                    <option value="View">View</option>
                    <option value="StoredProcedure">Stored Procedure</option>
                  </select>
                </label>
                <label className="toolbar-search-field">
                  <span>Search objects</span>
                  <input value={databaseObjectSearch} onChange={(event) => setDatabaseObjectSearch(event.target.value)} placeholder="Search by schema, object, or type" />
                </label>
              </div>

              <div className="external-test-config-grid">
                <div className="modal-section-card">
                  <div className="section-heading">
                    <h3>Available objects</h3>
                    <span>{visibleDatabaseObjects.length} visible</span>
                  </div>
                  {visibleDatabaseObjects.length === 0 ? (
                    <div className="empty-state compact">No objects found for this connection and filter.</div>
                  ) : (
                    <div className="external-test-history-list">
                      {visibleDatabaseObjects.map((item) => (
                        <button
                          key={`${item.schemaName}.${item.objectName}.${item.objectType}`}
                          className={`workflow-table-row workflow-table-grid external-api-grid ${selectedDatabaseObjectDetail?.schemaName === item.schemaName && selectedDatabaseObjectDetail?.objectName === item.objectName ? 'active' : ''}`}
                          type="button"
                          onClick={() => void selectDatabaseObject(item)}
                        >
                          <div className="row-stack workflow-name-cell">
                            <strong>{item.objectName}</strong>
                            <small>{item.schemaName}</small>
                          </div>
                          <div className="row-stack workflow-time-cell">
                            <strong>{item.objectType}</strong>
                            <small>{item.schemaName}.{item.objectName}</small>
                          </div>
                        </button>
                      ))}
                    </div>
                  )}
                </div>
                <div className="modal-section-card">
                  <div className="section-heading">
                    <h3>Object detail</h3>
                    <span>{selectedDatabaseObjectDetail ? selectedDatabaseObjectDetail.objectType : 'No selection'}</span>
                  </div>
                  {!selectedDatabaseObjectDetail ? (
                    <div className="empty-state compact">Select an object to inspect its metadata.</div>
                  ) : (
                    <>
                      {selectedDatabaseObjectDetail.columns.length > 0 && (
                        <div className="external-test-history-list">
                          {selectedDatabaseObjectDetail.columns.map((column) => (
                            <div key={column.columnName} className="external-test-history-row">
                              <div className="row-stack">
                                <strong>{column.columnName}</strong>
                                <small>{column.dataType}</small>
                              </div>
                              <div className="row-stack">
                                <strong>{column.isPrimaryKey ? 'PK' : column.isNullable ? 'Nullable' : 'Required'}</strong>
                                <small>{selectedDatabaseObjectDetail.schemaName}.{selectedDatabaseObjectDetail.objectName}</small>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                      {selectedDatabaseObjectDetail.parameters.length > 0 && (
                        <div className="external-test-history-list procedure-parameter-list">
                          {selectedDatabaseObjectDetail.parameters.map((parameter) => (
                            <div key={parameter.parameterName} className="external-test-history-row">
                              <div className="row-stack">
                                <strong>{parameter.parameterName}</strong>
                                <small>{parameter.dataType}</small>
                              </div>
                              <div className="row-stack">
                                <strong>{parameter.isOutput ? 'Output' : parameter.hasDefaultValue ? 'Optional' : 'Input'}</strong>
                                <small>{selectedDatabaseObjectDetail.schemaName}.{selectedDatabaseObjectDetail.objectName}</small>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </>
                  )}
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="secondary-action" type="button" onClick={() => void refreshDatabaseObjects(databaseIntegrationForm.schemaName || undefined, databaseObjectTypeFilter)} disabled={busy}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><RefreshInlineIcon /></span>Refresh Metadata</span>
                </button>
                <button className="primary-action" type="button" onClick={applySelectedDatabaseObject} disabled={!selectedDatabaseObjectDetail}>
                  <span className="button-label"><span className="button-icon" aria-hidden="true"><GridIcon /></span>Use Selected Object</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}

      {isDatabasePreviewOpen && databasePreviewResult && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--large" role="dialog" aria-modal="true" aria-labelledby="database-preview-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="database-preview-title"><span className="title-icon subtle" aria-hidden="true">PV</span>Database Preview</h3>
                <p>{`${databasePreviewResult.schemaName ?? ''}${databasePreviewResult.schemaName && databasePreviewResult.objectName ? '.' : ''}${databasePreviewResult.objectName ?? 'Selected source'}`}</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsDatabasePreviewOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="modal-intro-strip external-config-modal-strip">
                <div className="modal-intro-copy">
                  <strong>{databasePreviewResult.isSuccess ? 'Preview loaded' : 'Preview failed'}</strong>
                  <span>{databasePreviewResult.message}</span>
                </div>
                <div className="modal-intro-metrics">
                  <div className="modal-intro-metric">
                    <span>Source</span>
                    <strong>{databasePreviewResult.sourceType}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Rows</span>
                    <strong>{databasePreviewResult.rows.length}</strong>
                  </div>
                </div>
              </div>
              {!databasePreviewResult.isSuccess ? (
                <div className="empty-state compact">{databasePreviewResult.message}</div>
              ) : (
                <div className="modal-section-card">
                  <div className="section-heading">
                    <h3>Preview rows</h3>
                    <span>{formatDate(databasePreviewResult.previewedAtUtc)}</span>
                  </div>
                  <div className="database-preview-shell">
                    <div className="database-preview-toolbar">
                      <span>Showing sample data from the selected source</span>
                      <strong>{`${databasePreviewResult.rows.length} row${databasePreviewResult.rows.length === 1 ? '' : 's'} x ${databasePreviewResult.columns.length} column${databasePreviewResult.columns.length === 1 ? '' : 's'}`}</strong>
                    </div>
                    <div className="database-preview-grid">
                      <table className="database-preview-table">
                      <thead>
                        <tr>
                          {databasePreviewResult.columns.map((column) => (
                            <th key={column.columnName}>{column.columnName}</th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {databasePreviewResult.rows.map((row, index) => (
                          <tr key={index}>
                            {databasePreviewResult.columns.map((column) => (
                              <td key={column.columnName} title={row.values[column.columnName] ?? ''}>{row.values[column.columnName] ?? ''}</td>
                            ))}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                    </div>
                  </div>
                </div>
              )}
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                <button className="primary-action" type="button" onClick={() => setIsDatabasePreviewOpen(false)}>
                  <span className="button-label"><span className="button-icon">OK</span>Done</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}

      {isTestResultOpen && (
        <section className="modal-backdrop" role="presentation">
          <div className="modal-dialog modal-dialog--large" role="dialog" aria-modal="true" aria-labelledby="external-api-test-title" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3 id="external-api-test-title"><span className="title-icon subtle" aria-hidden="true">TS</span>API test result</h3>
                <p>{testTarget ? `${testTarget.name} (${testTarget.code})` : 'Configured API setup test'}</p>
              </div>
              <button className="modal-close-button" type="button" onClick={() => setIsTestResultOpen(false)} disabled={busy}>Close</button>
            </div>
            <div className="modal-body">
              <div className="modal-intro-strip external-config-modal-strip">
                <div className="modal-intro-copy">
                  <strong>{testResult?.isSuccess ? 'Request succeeded' : 'Request failed'}</strong>
                  <span>
                    The test can override the saved endpoint URL, headers, and request body before sending, while still using the configured system base URL and auth.
                  </span>
                </div>
                <div className="modal-intro-metrics">
                  <div className="modal-intro-metric">
                    <span>Status</span>
                    <strong>{testResult?.statusCode ?? '-'}</strong>
                  </div>
                  <div className="modal-intro-metric">
                    <span>Method</span>
                    <strong>{testResult?.method ?? '-'}</strong>
                  </div>
                </div>
              </div>
              <div className="external-test-config-grid">
                <div className="modal-section-card">
                  <div className="section-heading">
                    <h3>Test request</h3>
                    <span>Override before sending</span>
                  </div>
                  <div className="form-grid one-column">
                    <label className="field">
                      <span>Relative URL override</span>
                      <input value={testRelativeUrl} onChange={(event) => setTestRelativeUrl(event.target.value)} />
                    </label>
                    <label className="field">
                      <span>Headers JSON override</span>
                      <textarea className="code-editor small external-test-editor" rows={5} value={testHeaders} onChange={(event) => setTestHeaders(event.target.value)} />
                    </label>
                    <label className="field">
                      <span>Request body override</span>
                      <textarea className="code-editor small external-test-editor" rows={6} value={testBody} onChange={(event) => setTestBody(event.target.value)} />
                    </label>
                  </div>
                </div>
                <div className="modal-section-card">
                  <div className="section-heading">
                    <h3>Token preview</h3>
                    <span>Resolved during test</span>
                  </div>
                  <div className="external-token-grid">
                    {Object.entries(testTokens).map(([key, value]) => (
                      <label key={key} className="field">
                        <span>{`{{${key}}}`}</span>
                        <input value={value} onChange={(event) => updateTestToken(key, event.target.value)} />
                      </label>
                    ))}
                  </div>
                </div>
              </div>
              {testResult && (
                <div className="external-test-result-grid">
                  <div className="modal-section-card">
                    <div className="section-heading">
                      <h3>Request</h3>
                      <span>{formatDate(testResult.testedAtUtc)}</span>
                    </div>
                    <div className="external-test-meta-list">
                      <div className="external-test-meta-item">
                        <span>URL</span>
                        <strong>{testResult.requestUrl}</strong>
                      </div>
                      <div className="external-test-meta-item">
                        <span>Response</span>
                        <strong>{testResult.reasonPhrase || 'No reason phrase'}</strong>
                      </div>
                      <div className="external-test-meta-item">
                        <span>Resolved URL preview</span>
                        <strong>{resolvePreview(testRelativeUrl || '')}</strong>
                      </div>
                    </div>
                  </div>
                  <div className="modal-section-card">
                    <div className="section-heading">
                      <h3>Body</h3>
                      <span>{testResult.responseBody ? 'Captured' : 'Empty'}</span>
                    </div>
                    <textarea className="code-editor small external-test-output" readOnly value={testResult.responseBody || 'No response body returned.'} />
                  </div>
                </div>
              )}
              <div className="external-test-config-grid">
                <div className="modal-section-card">
                  <div className="section-heading">
                    <h3>Resolved headers preview</h3>
                    <span>After token substitution</span>
                  </div>
                  <textarea className="code-editor small external-test-output preview" readOnly value={resolvePreview(testHeaders || '{}')} />
                </div>
                <div className="modal-section-card">
                  <div className="section-heading">
                    <h3>Resolved body preview</h3>
                    <span>After token substitution</span>
                  </div>
                  <textarea className="code-editor small external-test-output preview" readOnly value={resolvePreview(testBody || '') || 'No request body'} />
                </div>
              </div>
              <div className="modal-section-card">
                <div className="section-heading">
                  <h3>Recent test history</h3>
                  <span>Latest 10 runs</span>
                </div>
                {testHistory.length === 0 ? (
                  <div className="empty-state compact">No recent tests for this API setup.</div>
                ) : (
                  <div className="external-test-history-list">
                    {testHistory.map((item) => (
                      <div key={item.id} className="external-test-history-row">
                        <div className="row-stack">
                          <strong>{item.statusCode ?? '-'}</strong>
                          <small>{item.reasonPhrase || 'No reason phrase'}</small>
                        </div>
                        <div className="row-stack">
                          <strong>{item.method}</strong>
                          <small>{item.requestUrl}</small>
                        </div>
                        <div className="row-stack">
                          <strong>{item.isSuccess ? 'Success' : 'Failed'}</strong>
                          <small>{formatDate(item.testedAtUtc)}</small>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
            <div className="modal-footer">
              <div className="modal-footer-actions">
                {testTarget && (
                  <button className="secondary-action" type="button" onClick={() => void handleRunTestFromModal()} disabled={busy}>
                    <span className="button-label"><span className="button-icon" aria-hidden="true"><RefreshInlineIcon /></span>Run again</span>
                  </button>
                )}
                <button className="primary-action" type="button" onClick={() => setIsTestResultOpen(false)}>
                  <span className="button-label"><span className="button-icon">OK</span>Done</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}
    </>
  )
}

function resolveExternalSystemName(systemId: string, systems: ExternalSystem[]) {
  return systems.find((system) => system.id === systemId)?.name ?? 'Unknown system'
}

function parseProcedureInputMappingJson(inputMappingJson: string): Record<string, string> {
  if (!inputMappingJson.trim()) {
    return {}
  }

  try {
    const parsed = JSON.parse(inputMappingJson)
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      return {}
    }

    return Object.fromEntries(
      Object.entries(parsed).map(([key, value]) => [key, value == null ? '' : String(value)]),
    )
  } catch {
    return {}
  }
}

function parseOutputMappingRows(outputMappingJson: string): OutputMappingRow[] {
  const mapping = parseStringMapJson(outputMappingJson)
  return Object.entries(mapping).map(([targetVariable, sourceColumn]) => ({
    sourceColumn,
    targetVariable,
  }))
}

function parseStringMapJson(value: string): Record<string, string> {
  if (!value.trim()) {
    return {}
  }

  try {
    const parsed = JSON.parse(value)
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      return {}
    }

    return Object.fromEntries(
      Object.entries(parsed).map(([key, item]) => [key, item == null ? '' : String(item)]),
    )
  } catch {
    return {}
  }
}

function buildStringMapJson<T extends Record<string, string>>(
  items: T[],
  valueKey: keyof T,
  keyKey: keyof T,
): string {
  const payload = Object.fromEntries(
    items
      .filter((item) => String(item[keyKey] ?? '').trim() && String(item[valueKey] ?? '').trim())
      .map((item) => [String(item[keyKey]).trim(), String(item[valueKey]).trim()]),
  )

  return JSON.stringify(payload, null, 2)
}

function parseStringArrayJson(value: string): string[] {
  if (!value.trim()) {
    return []
  }

  try {
    const parsed = JSON.parse(value)
    if (!Array.isArray(parsed)) {
      return []
    }

    return parsed.map((item) => String(item))
  } catch {
    return []
  }
}

function buildProcedureParameterFields(
  detail: ExternalDatabaseObjectDetail,
  inputMappingJson: string,
): ProcedureParameterField[] {
  const mapping = parseProcedureInputMappingJson(inputMappingJson)

  return detail.parameters.map((parameter) => {
    const normalizedName = parameter.parameterName.startsWith('@')
      ? parameter.parameterName.slice(1)
      : parameter.parameterName

    return {
      ...parameter,
      value: mapping[parameter.parameterName] ?? mapping[normalizedName] ?? '',
    }
  })
}

function buildProcedureInputMappingJson(parameters: ProcedureParameterField[]): string {
  const payload = Object.fromEntries(
    parameters
      .filter((parameter) => !parameter.isOutput)
      .map((parameter) => [parameter.parameterName, parameter.value]),
  )

  return JSON.stringify(payload, null, 2)
}

function parseDatabaseAccessDetails(
  value: string,
  provider: string,
): DatabaseAccessDetailsForm {
  if (!value.trim()) {
    return emptyDatabaseAccessDetails
  }

  const pairs = value
    .split(';')
    .map((part) => part.trim())
    .filter(Boolean)
    .map((part) => {
      const separatorIndex = part.indexOf('=')
      if (separatorIndex < 0) {
        return ['', ''] as const
      }

      return [
        part.slice(0, separatorIndex).trim().toLowerCase(),
        part.slice(separatorIndex + 1).trim(),
      ] as const
    })
    .filter(([key]) => Boolean(key))

  const map = Object.fromEntries(pairs)
  const details = { ...emptyDatabaseAccessDetails, raw: value }

  if (provider === 'Sqlite') {
    details.filePath = map['data source'] ?? ''
    return details
  }

  const hostValue = map.server ?? map['data source'] ?? map.host ?? ''
  if (hostValue.includes(',') && provider === 'SqlServer') {
    const [host, port] = hostValue.split(',', 2)
    details.host = host.trim()
    details.port = port?.trim() ?? ''
  } else if (hostValue.includes(':') && (provider === 'PostgreSql' || provider === 'MySql' || provider === 'Oracle')) {
    const [host, port] = hostValue.split(':', 2)
    details.host = host.trim()
    details.port = port?.trim() ?? ''
  } else {
    details.host = hostValue
  }

  details.port = details.port || map.port || ''
  details.databaseName = map.database ?? map['initial catalog'] ?? map.service ?? ''
  details.userName = map['user id'] ?? map.uid ?? map.username ?? map.user ?? ''
  details.password = map.password ?? map.pwd ?? ''
  details.encrypt = parseBooleanSetting(map.encrypt, true)
  details.trustServerCertificate = parseBooleanSetting(map.trustservercertificate, true)
  details.authMode = parseBooleanSetting(map['integrated security'] ?? map['trusted_connection'], false)
    ? 'Integrated'
    : 'SqlLogin'

  return details
}

function buildDatabaseAccessDetailsValue(
  provider: string,
  details: DatabaseAccessDetailsForm,
): string {
  if (details.raw.trim()) {
    return details.raw.trim()
  }

  if (provider === 'Sqlite') {
    return details.filePath.trim() ? `Data Source=${details.filePath.trim()};` : ''
  }

  if (!details.host.trim() && !details.databaseName.trim() && !details.userName.trim() && !details.password.trim()) {
    return ''
  }

  const parts: string[] = []

  switch (provider) {
    case 'SqlServer':
      parts.push(`Server=${details.host.trim()}${details.port.trim() ? `,${details.port.trim()}` : ''}`)
      if (details.databaseName.trim()) parts.push(`Database=${details.databaseName.trim()}`)
      if (details.authMode === 'Integrated') {
        parts.push('Integrated Security=True')
      } else {
        if (details.userName.trim()) parts.push(`User Id=${details.userName.trim()}`)
        if (details.password) parts.push(`Password=${details.password}`)
      }
      parts.push(`Encrypt=${details.encrypt ? 'True' : 'False'}`)
      parts.push(`TrustServerCertificate=${details.trustServerCertificate ? 'True' : 'False'}`)
      break
    case 'PostgreSql':
      parts.push(`Host=${details.host.trim()}`)
      if (details.port.trim()) parts.push(`Port=${details.port.trim()}`)
      if (details.databaseName.trim()) parts.push(`Database=${details.databaseName.trim()}`)
      if (details.userName.trim()) parts.push(`Username=${details.userName.trim()}`)
      if (details.password) parts.push(`Password=${details.password}`)
      parts.push(`SSL Mode=${details.encrypt ? 'Require' : 'Disable'}`)
      break
    case 'MySql':
      parts.push(`Server=${details.host.trim()}`)
      if (details.port.trim()) parts.push(`Port=${details.port.trim()}`)
      if (details.databaseName.trim()) parts.push(`Database=${details.databaseName.trim()}`)
      if (details.userName.trim()) parts.push(`User Id=${details.userName.trim()}`)
      if (details.password) parts.push(`Password=${details.password}`)
      parts.push(`SslMode=${details.encrypt ? 'Required' : 'None'}`)
      break
    case 'Oracle':
      parts.push(`Data Source=${details.host.trim()}${details.port.trim() ? `:${details.port.trim()}` : ''}/${details.databaseName.trim()}`)
      if (details.userName.trim()) parts.push(`User Id=${details.userName.trim()}`)
      if (details.password) parts.push(`Password=${details.password}`)
      break
    default:
      parts.push(`Server=${details.host.trim()}`)
      if (details.port.trim()) parts.push(`Port=${details.port.trim()}`)
      if (details.databaseName.trim()) parts.push(`Database=${details.databaseName.trim()}`)
      if (details.userName.trim()) parts.push(`User Id=${details.userName.trim()}`)
      if (details.password) parts.push(`Password=${details.password}`)
      break
  }

  return parts.filter(Boolean).join(';') + ';'
}

function parseBooleanSetting(value: string | undefined, fallback: boolean): boolean {
  if (!value) {
    return fallback
  }

  return ['true', 'yes', '1', 'sspi'].includes(value.trim().toLowerCase())
}

function getDatabaseNameLabel(provider: string) {
  return provider === 'Oracle' ? 'Service name' : 'Database name'
}

function getDatabaseHostLabel(provider: string) {
  return provider === 'Oracle' ? 'Host name' : 'Server or host'
}

function getDatabasePortPlaceholder(provider: string) {
  switch (provider) {
    case 'SqlServer':
      return '1433'
    case 'PostgreSql':
      return '5432'
    case 'MySql':
      return '3306'
    case 'Oracle':
      return '1521'
    default:
      return 'Optional'
  }
}

function resolveSecretRotationLabel(endpointId: string, endpoints: ExternalApiEndpoint[]) {
  const endpoint = endpoints.find((item) => item.id === endpointId)
  if (!endpoint?.hasAuthSecret) {
    return 'Not stored'
  }

  if (endpoint.authSecretUpdatedAtUtc) {
    return formatDate(endpoint.authSecretUpdatedAtUtc)
  }

  return 'Stored'
}

function RefreshInlineIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M13 8a5 5 0 1 1-1.4-3.5" />
      <path d="M13 3.5v3.3H9.7" />
    </svg>
  )
}

function EditIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="m11.8 2.9 1.3 1.3" />
      <path d="m4.1 10.6 6.8-6.8 1.3 1.3-6.8 6.8-2 .6.7-1.9Z" />
    </svg>
  )
}

function SaveIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3.5 3.5h7l2 2V12.5h-9z" />
      <path d="M5 3.5v3h5v-3" />
      <path d="M5.3 12.5V9.4h5.4v3.1" />
    </svg>
  )
}

function CreateIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <path d="M8 3.2v9.6" />
      <path d="M3.2 8h9.6" />
    </svg>
  )
}

function DatabaseIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <ellipse cx="8" cy="3.8" rx="4.8" ry="1.8" />
      <path d="M3.2 3.8v5.5c0 1 2.1 1.8 4.8 1.8s4.8-.8 4.8-1.8V3.8" />
      <path d="M3.2 6.6c0 1 2.1 1.8 4.8 1.8s4.8-.8 4.8-1.8" />
    </svg>
  )
}

function GridIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2.5" y="2.5" width="4.2" height="4.2" />
      <rect x="9.3" y="2.5" width="4.2" height="4.2" />
      <rect x="2.5" y="9.3" width="4.2" height="4.2" />
      <rect x="9.3" y="9.3" width="4.2" height="4.2" />
    </svg>
  )
}

function PulseIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M2.5 8h2.1l1.2-2.4L8 10.8l1.6-4 1.2 1.2h2.7" />
    </svg>
  )
}
