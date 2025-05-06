import React, { useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { createPayPalOrder } from '../Data/dataApi'; // ajuste le chemin si besoin
import { registerUser } from '../Data/dataApi'; // Importer la fonction d'enregistrement


declare global {
  interface Window {
    paypal: any;
  }
}

const Payment: React.FC = () => {
  const paypalRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const { t } = useTranslation();

  useEffect(() => {
    let isMounted = true;
    const paypalElement = paypalRef.current; // capture de la ref au début de l'effet
  
    const loadPayPalScript = (): Promise<void> => {
      return new Promise((resolve) => {
        const existingScript = document.querySelector(`script[src*="paypal.com/sdk/js"]`);
        if (existingScript) {
          if ((existingScript as any).loaded) {
            resolve();
          } else {
            existingScript.addEventListener('load', () => resolve());
          }
          return;
        }
  
        const script = document.createElement('script');
        script.src = 'https://www.paypal.com/sdk/js?client-id=ATSO1nSk356PSP7ADBaPURlCmBeXmrNYjm9vN2bUh-HzTI3Md0qwa7Y-an3XLvHhDX4xomVBSkhONj_b&currency=USD';
        script.async = true;
        script.onload = () => {
          (script as any).loaded = true;
          resolve();
        };
        document.body.appendChild(script);
      });
    };
  
    loadPayPalScript().then(() => {
      if (!isMounted || !window.paypal || !paypalElement) return;
  
      paypalElement.innerHTML = '';
  
      window.paypal.Buttons({
        style: {
          layout: 'vertical',
          color: 'blue',
          shape: 'pill',
          label: 'paypal',
        },
        createOrder: async () => {
          const data = await createPayPalOrder('Abonnement premium', '39.90');
          return data.orderId;
        },
        
        onApprove: async (data: any, actions: any) => {
          const details = await actions.order.capture();
          console.log('Transaction:', details);
        
          // Récupère les données de l'inscription depuis sessionStorage
          const pendingData = sessionStorage.getItem('pendingRegistration');
          if (!pendingData) {
            alert('Erreur : Données utilisateur manquantes.');
            return;
          }
        
          const formData = JSON.parse(pendingData);
        
          try {
            const result = await registerUser(formData);
            if (result.success) {
              sessionStorage.removeItem('pendingRegistration');
              alert('Paiement et inscription réussis !');
              navigate('/login');
            } else {
              alert('Inscription échouée : ' + result.message);
            }
          } catch (err) {
            console.error(err);
            alert("Une erreur est survenue lors de l'enregistrement.");
          }
        },
        onError: (err: any) => {
          console.error('Erreur PayPal :', err);
          alert('Une erreur est survenue pendant le paiement.');
        },
      }).render(paypalElement);
    });
  
    return () => {
      isMounted = false;
      if (paypalElement) {
        paypalElement.innerHTML = '';
      }
    };
  }, [navigate]);
  

  return (
    <div className="payment-container">
    <h2 className="payment-title">{t('payment.title')}</h2>
    <p>{t('payment.subtitle')}</p>
    <h3>{t('payment.total')}: 39.90 $</h3>
    <div ref={paypalRef} style={{ marginTop: '20px' }}></div>
  </div>
  );
};

export default Payment;
