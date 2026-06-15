import { useEffect, useMemo, useState } from 'react'
import './App.css'
import { api } from './api'
import type { Account, AccountPosition, Client, InstrumentDetails, InstrumentListItem } from './types'

type Page = 'portfolio' | 'instruments'

function formatMoney(value: number, currency: string) {
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency,
    maximumFractionDigits: 2,
  }).format(value)
}

function formatDate(value: string) {
  return new Date(value).toLocaleString('ru-RU')
}

function getPnlClass(value: number) {
  if (value > 0) return 'positive'
  if (value < 0) return 'negative'
  return 'neutral'
}

function App() {
  const [page, setPage] = useState<Page>('portfolio')
  const [clients, setClients] = useState<Client[]>([])
  const [accounts, setAccounts] = useState<Account[]>([])
  const [positions, setPositions] = useState<AccountPosition[]>([])
  const [instruments, setInstruments] = useState<InstrumentListItem[]>([])
  const [selectedClientId, setSelectedClientId] = useState('')
  const [selectedAccountId, setSelectedAccountId] = useState('')
  const [selectedInstrumentId, setSelectedInstrumentId] = useState('')
  const [instrumentDetails, setInstrumentDetails] = useState<InstrumentDetails | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([api.getClients(), api.getInstruments()])
      .then(([clientData, instrumentData]) => {
        setClients(clientData)
        setInstruments(instrumentData)

        if (clientData.length > 0) {
          setSelectedClientId(clientData[0].id)
        }
      })
      .catch(() => setError('Не удалось загрузить стартовые данные'))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    if (!selectedClientId) {
      setAccounts([])
      setSelectedAccountId('')
      return
    }

    api.getAccounts(selectedClientId)
      .then((accountData) => {
        setAccounts(accountData)
        setSelectedAccountId(accountData[0]?.id ?? '')
      })
      .catch(() => setError('Не удалось загрузить счета клиента'))
  }, [selectedClientId])

  useEffect(() => {
    if (!selectedAccountId) {
      setPositions([])
      return
    }

    api.getPositions(selectedAccountId)
      .then(setPositions)
      .catch(() => setError('Не удалось загрузить позиции по счёту'))
  }, [selectedAccountId])

  useEffect(() => {
    if (!selectedInstrumentId) {
      setInstrumentDetails(null)
      return
    }

    api.getInstrument(selectedInstrumentId)
      .then(setInstrumentDetails)
      .catch(() => setError('Не удалось загрузить карточку инструмента'))
  }, [selectedInstrumentId])

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      api.getInstruments().then(setInstruments).catch(() => undefined)

      if (selectedAccountId) {
        api.getPositions(selectedAccountId).then(setPositions).catch(() => undefined)
      }

      if (selectedInstrumentId) {
        api.getInstrument(selectedInstrumentId).then(setInstrumentDetails).catch(() => undefined)
      }
    }, 3000)

    return () => window.clearInterval(intervalId)
  }, [selectedAccountId, selectedInstrumentId])

  const selectedClient = useMemo(
    () => clients.find((client) => client.id === selectedClientId) ?? null,
    [clients, selectedClientId],
  )

  if (loading) {
    return <div className="shell"><p>Грузим MVP...</p></div>
  }

  return (
    <div className="shell">
      <header className="topbar">
        <div>
          <h1>market-mvp</h1>
          <p>Учебная витрина портфеля и рыночных цен</p>
        </div>
        <nav className="tabs">
          <button className={page === 'portfolio' ? 'active' : ''} onClick={() => setPage('portfolio')}>
            Портфель
          </button>
          <button className={page === 'instruments' ? 'active' : ''} onClick={() => setPage('instruments')}>
            Инструменты
          </button>
        </nav>
      </header>

      {error ? <div className="error">{error}</div> : null}

      {page === 'portfolio' ? (
        <section className="grid wide-grid">
          <div className="card controls">
            <h2>Выбор клиента и счёта</h2>
            <label>
              Клиент
              <select value={selectedClientId} onChange={(event) => setSelectedClientId(event.target.value)}>
                {clients.map((client) => (
                  <option key={client.id} value={client.id}>
                    {client.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Счёт
              <select value={selectedAccountId} onChange={(event) => setSelectedAccountId(event.target.value)}>
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.accountNumber}
                  </option>
                ))}
              </select>
            </label>

            <div className="summary">
              <div>
                <span>Выбранный клиент</span>
                <strong>{selectedClient?.name ?? 'Не выбран'}</strong>
              </div>
              <div>
                <span>Позиций</span>
                <strong>{positions.length}</strong>
              </div>
            </div>
          </div>

          <div className="card positions-card">
            <h2>Позиции по счёту</h2>
            <table>
              <thead>
                <tr>
                  <th>Тикер</th>
                  <th>Инструмент</th>
                  <th>Количество</th>
                  <th>Средняя цена</th>
                  <th>Дата покупки</th>
                  <th>Рынок</th>
                  <th>Value</th>
                  <th>PnL</th>
                  <th>PnL %</th>
                </tr>
              </thead>
              <tbody>
                {positions.map((position) => (
                  <tr key={`${position.instrumentId}-${position.purchaseDate}`} onClick={() => setSelectedInstrumentId(position.instrumentId)}>
                    <td>{position.ticker}</td>
                    <td>{position.instrumentName}</td>
                    <td>{position.quantity}</td>
                    <td>{position.averagePrice}</td>
                    <td>{position.purchaseDate}</td>
                    <td>{position.marketPrice}</td>
                    <td>{position.marketValue.toFixed(2)}</td>
                    <td className={getPnlClass(position.unrealizedPnl)}>{position.unrealizedPnl.toFixed(2)}</td>
                    <td className={getPnlClass(position.unrealizedPnlPercent)}>{position.unrealizedPnlPercent.toFixed(2)}%</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="card instrument-card">
            <h2>Карточка инструмента</h2>
            {instrumentDetails ? (
              <div className="instrument-details">
                <div><span>Тикер</span><strong>{instrumentDetails.ticker}</strong></div>
                <div><span>Название</span><strong>{instrumentDetails.name}</strong></div>
                <div><span>Тип</span><strong>{instrumentDetails.type}</strong></div>
                <div><span>Валюта</span><strong>{instrumentDetails.currency}</strong></div>
                <div><span>Биржа</span><strong>{instrumentDetails.exchange}</strong></div>
                <div><span>ISIN</span><strong>{instrumentDetails.isin}</strong></div>
                <div><span>Рыночная цена</span><strong>{formatMoney(instrumentDetails.marketPrice, instrumentDetails.currency)}</strong></div>
                <div><span>Последнее обновление</span><strong>{formatDate(instrumentDetails.lastUpdatedAtUtc)}</strong></div>
              </div>
            ) : (
              <p>Нажми на инструмент в таблице слева.</p>
            )}
          </div>
        </section>
      ) : (
        <section className="card instruments-list-card">
          <h2>Список инструментов</h2>
          <table>
            <thead>
              <tr>
                <th>Тикер</th>
                <th>Название</th>
                <th>Тип</th>
                <th>Валюта</th>
                <th>Рыночная цена</th>
                <th>Последнее обновление</th>
              </tr>
            </thead>
            <tbody>
              {instruments.map((instrument) => (
                <tr key={instrument.instrumentId} onClick={() => { setSelectedInstrumentId(instrument.instrumentId); setPage('portfolio') }}>
                  <td>{instrument.ticker}</td>
                  <td>{instrument.name}</td>
                  <td>{instrument.type}</td>
                  <td>{instrument.currency}</td>
                  <td>{formatMoney(instrument.marketPrice, instrument.currency)}</td>
                  <td>{formatDate(instrument.lastUpdatedAtUtc)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}
    </div>
  )
}

export default App
