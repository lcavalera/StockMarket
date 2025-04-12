import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import './i18n.ts';  // Cette ligne est importante pour charger la configuration d'i18n
import Layout from './Components/layout.tsx';
import Home from './Pages/Home.tsx';
import List from './Pages/List.tsx';
import Details from './Pages/Details.tsx';
import './Css/global.css';

// const maListeIndices = [
//     {
//       symbol: "AAPL",
//       name: "Apple Inc.",
//       regularMarketPreviousClose: 150.12,
//       regularMarketPrice: 152.23,
//       regularMarketChange: 2.11,
//       quoteType: "Equity",
//       datesExercicesFinancieres: ["2024-12-31", "2024-09-30"],
//       bourse: "XNAS",
//       label: true,
//       raccomandation: "Buy",
//       dateUpdated: "2025-04-04T15:00:00",
//       probability: 0.67
//     },
//     {
//       symbol: "GOOGL",
//       name: "Alphabet Inc.",
//       regularMarketPreviousClose: 2800.5,
//       regularMarketPrice: 2750.1,
//       regularMarketChange: -50.4,
//       quoteType: "Equity",
//       datesExercicesFinancieres: ["2025-03-31"],
//       bourse: "XNAS",
//       label: false,
//       raccomandation: "Hold",
//       dateUpdated: "2025-04-03T10:30:00",
//       probability: 0.45
//     },
//     {
//       symbol: "BNP.PA",
//       name: "BNP Paribas",
//       regularMarketPreviousClose: 60.5,
//       regularMarketPrice: 61.3,
//       regularMarketChange: 0.8,
//       quoteType: "Equity",
//       datesExercicesFinancieres: ["2025-06-30", "2024-06-30"],
//       bourse: "EPA",
//       label: true,
//       raccomandation: "Strong Buy",
//       dateUpdated: "2025-04-04T08:45:00",
//       probability: 0.85
//     }
//   ];

// const exchanges = {
//     XNAS: "NASDAQ",
//     XNYS: "NYSE",
//     EPA: "Euronext Paris"
// };

const root = ReactDOM.createRoot(document.getElementById('root') as HTMLElement);
root.render(
  <React.StrictMode>
   <Router>
      <Routes>   
        <Route path='/' element={<Layout />}>
          <Route index element={<Home />} />
          <Route path="/list" element={<List />} />
          <Route path="/details/:symbol" element={<Details />} />
          {/* <Route path="/list" element={<List indices={maListeIndices}
                                    exchanges={exchanges}/>} /> */}
        </Route>
      </Routes>
    </Router>
  </React.StrictMode>
);
