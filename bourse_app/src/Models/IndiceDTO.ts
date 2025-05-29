// src/Models/IndiceDTO.ts

export interface StockData {
    id: number;
    indiceId: number;
    currentPrice: number;
    prevPrice: number;
    open: number;
    high: number;
    low: number;
    date: Date;
    isIncreasing: boolean;
    change: number;
    changePercent: number;
    sma14: number;
    sma14Display: string;
    rsi14: number;
    rsi14Display: string;
    futurePrice: number;
    ema14: number;
    bollingerUpper: number;
    bollingerLower: number;
    macd: number;
    averageVolume: number;
  }
  
  export interface IndiceDTO {
    id: number;
    symbol: string;
    name: string;
    regularMarketPrice: number;
    regularMarketChange: number;
    regularMarketOpen: number;
    regularMarketPreviousClose: number;
    regularMarketDayHigh: number;
    regularMarketDayLow: number;
    regularMarketChangePercent: number;
    regularMarketVolume: number;
    quoteType: string;
    exchange: string;
    exchangeTimezoneName: string;
    exchangeTimezoneShortName: string;
    bourse: string;
    datesExercicesFinancieres: Date[];
    label: boolean;
    isIncreasing: boolean;
    probability: number;
    raccomandation: string;
    analysis: Record<string, number>;
    dateUpdated: Date;
    datePrevision: Date;
    trainingData: StockData[];
  }
  