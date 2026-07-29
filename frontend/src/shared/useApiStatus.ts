import { useEffect, useState } from 'react'

export type ApiStatus = 'checking' | 'online' | 'offline'

const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()

export const API_BASE_URL = configuredApiBaseUrl
  ? configuredApiBaseUrl.replace(/\/$/, '')
  : ''

export function useApiStatus(): ApiStatus {
  const [status, setStatus] = useState<ApiStatus>('checking')

  useEffect(() => {
    const controller = new AbortController()

    async function checkHealth() {
      try {
        const response = await fetch(`${API_BASE_URL}/health/live`, {
          signal: controller.signal,
        })

        setStatus(response.ok ? 'online' : 'offline')
      } catch (error) {
        if (error instanceof DOMException && error.name === 'AbortError') {
          return
        }

        setStatus('offline')
      }
    }

    void checkHealth()

    return () => controller.abort()
  }, [])

  return status
}
