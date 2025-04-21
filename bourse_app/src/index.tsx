import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import './i18n.ts';  // Cette ligne est importante pour charger la configuration d'i18n
import Layout from './Components/layout.tsx';
import Home from './Pages/Home.tsx';
import List from './Pages/List.tsx';
import Forecasts from './Pages/Forecasts.tsx';
import Details from './Pages/Details.tsx';
import './Css/global.css';
import RegisterForm from './Pages/RegisterForm.tsx'

const root = ReactDOM.createRoot(document.getElementById('root') as HTMLElement);
root.render(
  <React.StrictMode>
   <Router>
      <Routes>   
        <Route path='/' element={<Layout />}>
          <Route index element={<Home />} />
          <Route path="/list" element={<List />} />
          <Route path="/forecasts" element={<Forecasts />} />
          <Route path="/details/:symbol" element={<Details />} />
          <Route path="/registerForm" element={<RegisterForm />} />
          {/* <Route path="/list" element={<List indices={maListeIndices}
                                    exchanges={exchanges}/>} /> */}
        </Route>
      </Routes>
    </Router>
  </React.StrictMode>
);
