export type Client = {
  id: string
  name: string
}

export type Account = {
  id: string
  accountNumber: string
}

export type AccountPosition = {
  instrumentId: string
  ticker: string
  instrumentName: string
  quantity: number
  averagePrice: number
  purchaseDate: string
  marketPrice: number
  marketValue: number
  unrealizedPnl: number
  unrealizedPnlPercent: number
  lastUpdatedAtUtc: string
}

export type InstrumentListItem = {
  instrumentId: string
  ticker: string
  name: string
  type: string
  currency: string
  marketPrice: number
  lastUpdatedAtUtc: string
}

export type InstrumentDetails = InstrumentListItem & {
  exchange: string
  isin: string
}
