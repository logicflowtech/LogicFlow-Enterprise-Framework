import { StrictMode } from 'react'
import { createRoot, type Root } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

let root: Root | null = null

function renderApp(host: Element) {
  root = createRoot(host)
  root.render(
    <StrictMode>
      <App />
    </StrictMode>,
  )
}

function mountWorkflowDesigner(host: Element) {
  if (root) {
    root.unmount()
    root = null
  }

  renderApp(host)
}

function unmountWorkflowDesigner() {
  if (!root) {
    return
  }

  root.unmount()
  root = null
}

;(window as Window & {
  logicFlowWorkflowDesigner?: {
    mount: (host: Element) => void
    unmount: () => void
  }
}).logicFlowWorkflowDesigner = {
  mount: mountWorkflowDesigner,
  unmount: unmountWorkflowDesigner,
}

const standaloneHost = document.getElementById('root')
if (standaloneHost) {
  renderApp(standaloneHost)
}
