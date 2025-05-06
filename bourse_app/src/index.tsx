import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import './i18n.ts';  // Cette ligne est importante pour charger la configuration d'i18n
import Layout from './Components/layout';
import Home from './Pages/Home';
import List from './Pages/List';
import Forecasts from './Pages/Forecasts';
import Details from './Pages/Details';
import './Css/global.css';
import RegisterForm from './Pages/RegisterForm'
import Login from './Pages/Login';
import AbonnementsPage from "./Pages/AbonnementsPage";
import { clearAllCache } from './Data/dataCache';
import { AuthProvider } from './Auth/autoContext';  // <-- Assure-toi que c'est bien le bon chemin
import Payment from './Pages/Payment';


const SESSION_FLAG = 'session-active';

if (!sessionStorage.getItem(SESSION_FLAG)) {
  // Supposé être un nouveau lancement => on nettoie le cache
  clearAllCache(); // <- ta fonction de nettoyage
}

sessionStorage.setItem(SESSION_FLAG, 'true');

const root = ReactDOM.createRoot(document.getElementById('root') as HTMLElement);
root.render(
  <React.StrictMode>
    <AuthProvider> {/* ✅ Fournit le contexte à toute l'app */}
      <Router>
        <Routes>
          <Route path='/' element={<Layout />}>
            <Route index element={<Home />} />
            <Route path="/list" element={<List />} />
            <Route path="/forecasts" element={<Forecasts />} />
            <Route path="/details/:symbol" element={<Details />} />
            <Route path="/registerForm" element={<RegisterForm />} />
            <Route path="/login" element={<Login />} />
            <Route path="/abonnements" element={<AbonnementsPage />} />
            <Route path="/payment" element={<Payment />} />
          </Route>
        </Routes>
      </Router>
    </AuthProvider>
  </React.StrictMode>
);
