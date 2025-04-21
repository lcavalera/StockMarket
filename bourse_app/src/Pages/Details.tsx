import React from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { getIndiceDetails } from '../Data/dataApi.ts';
import { IndiceDTO } from '../types.ts';
import { Button } from '../Components/ui/button.tsx';
import { useTranslation } from 'react-i18next';

const Details = () => {
  const { symbol } = useParams(); // symbol
  const navigate = useNavigate();
  const location = useLocation();

  const [indice, setIndice] = useState<IndiceDTO | null>(null);

  const returnUrl = new URLSearchParams(location.search).get('returnUrl') || 'list';

  // const getRecommendationClass = (rec: string): string => {
  //   switch (rec) {
  //     case 'Strong Buy':
  //       return 'text-strong-buy';
  //     case 'Buy':
  //       return 'text-buy';
  //     case 'Hold':
  //       return 'text-hold';
  //     case 'Sell':
  //       return 'text-sell';
  //     case 'Strong Sell':
  //       return 'text-strong-sell';
  //     default:
  //       return '';
  //   }
  // };

  //  Configuration for using translation with json file
  const { t } = useTranslation();
  
  useEffect(() => {
    console.log(symbol)
    if (symbol) {
      getIndiceDetails(symbol).then(setIndice);
    }
  }, [symbol]);

  if (!indice) return <div>{t('details.loading')}</div>;

  const getRecommendationClass = (rec: string): string => {
    switch (rec) {
      case 'Strong Buy': return 'text-strong-buy';
      case 'Buy': return 'text-buy';
      case 'Hold': return 'text-hold';
      case 'Sell': return 'text-sell';
      case 'Strong Sell': return 'text-strong-sell';
      default: return '';
    }
  };

  return (
    <div className='list-container'>
      <h1 className="text-2xl font-bold mb-4">
        {indice.symbol}: {indice.name}
      </h1>

      <div className="mb-4">
      <Button onClick={() => navigate(`/${returnUrl}`)}>
      {t('details.back')} {returnUrl}
      </Button>
      </div>

      <h2 className="text-xl font-semibold mb-2">{t('details.history')}</h2>
      <table className="table">
        <thead className="bg-gray-200">
          <tr>
            <th>{t('details.table.price')}</th>
            <th>{t('details.table.change')}</th>
            <th>{t('details.table.open')}</th>
            <th>{t('details.table.%change')}</th>
            <th>{t('details.table.prevClose')}</th>
            <th>{t('details.table.high')}</th>
            <th>{t('details.table.low')}</th>
            <th>{t('details.table.volume')}</th>
            <th>{t('details.table.type')}</th>
            <th>{t('details.table.stockmarket')}</th>
            <th>{t('details.table.exchange')}</th>
            <th>Tendance</th>
            <th>Analyses</th>
            <th>Probabilité</th>
            <th>{t('details.table.date')}</th>
          </tr>
        </thead>
        <tbody>
          {indice.trainingData
            .slice()
            .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()) // tri décroissant
            .slice(0, 20) // prends les 20 plus récents
            .map((item, index) => (
              <tr key={index}>
                <td>{item.currentPrice.toFixed(3)}</td>
                <td className={item.change > 0 ? "color-positive" : "color-negative"}>{item.change.toFixed(3)}</td>
                <td>{item.open.toFixed(3)}</td>
                <td className={item.changePercent > 0 ? "color-positive" : "color-negative"}>
                  ({item.changePercent.toFixed(3)}%)
                </td>
                <td>{item.prevPrice.toFixed(3)}</td>
                <td>{item.high.toFixed(3)}</td>
                <td>{item.low.toFixed(3)}</td>
                <td>{indice.regularMarketVolume}</td>
                <td>{indice.quoteType}</td>
                <td>{indice.bourse}</td>
                <td>{indice.exchange}</td>
                <td className={item.label ? "color-positive" : "color-negative"}>{item.label ? "UP" : "DOWN"}</td>
                <td className={`rec-cell ${getRecommendationClass(item.raccomandation)}`}>{item.raccomandation}</td>
                <td className={item.probability > 0.49 ? "color-positive" : "color-negative"}>{(item.probability * 100).toFixed(1)}%</td>
                <td>
                    {new Date(item.date).toLocaleDateString('en-EN', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        weekday: 'short',
                    })}
                </td>
              </tr>
            ))}
        </tbody>
      </table>
    </div>
  );
};

export default Details;
