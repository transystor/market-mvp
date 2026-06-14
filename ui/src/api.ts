import type { Account, AccountPosition, Client, InstrumentDetails, InstrumentListItem } from './types'

const API_BASE = 'http://localhost:5032'

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`)

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`)
  }

  return response.json() as Promise<T>
}

export const api = {
  getClients: () => getJson<Client[]>('/ui/clients'),
  getAccounts: (clientId: string) => getJson<Account[]>(`/ui/clients/${clientId}/accounts`),
  getPositions: (accountId: string) => getJson<AccountPosition[]>(`/ui/accounts/${accountId}/positions`),
  getInstruments: () => getJson<InstrumentListItem[]>('/ui/instruments'),
  getInstrument: (instrumentId: string) => getJson<InstrumentDetails>(`/ui/instruments/${instrumentId}`),
}
