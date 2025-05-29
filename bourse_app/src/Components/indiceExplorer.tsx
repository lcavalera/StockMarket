
import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { IndiceDTO } from '../Models/IndiceDTO';
import { Button } from '../Components/ui/button';
import { useTranslation } from 'react-i18next';
import { useAuth } from "../Auth/autoContext";
import { useNavigate } from "react-router-dom";

type Props = {
  fetchData: (
    search: string,
    exchange: string,
    sort: string,
    page: number,
    pageSize: number
  ) => Promise<any>;
  returnUrl: string; // Ou tu peux typer selon la structure retournée
  saveCache: (data: { items: any[]; totalPages: number }, search: string, exchange: string, sort: string, page: number) => Promise<void>;
  getCachedData: (search: string, exchange: string, sort: string, page: number) => Promise<{ items: any[]; totalPages: number } | null>;
};

const IndiceExplorer: React.FC<Props> = ({ fetchData, returnUrl, getCachedData, saveCache }) => {
  const [indices, setIndices] = useState<IndiceDTO[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [exchangeFilter, setExchangeFilter] = useState('');
  const [sortOrder, setSortOrder] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [pageSize] = useState(50);
  const [isLoading, setIsLoading] = useState(false);
  const { t } = useTranslation();
  const { role } = useAuth();
  const navigate = useNavigate();
  const [doSearch, setDoSearch] = useState(false);
  
  useEffect(() => {
    const shouldFetch = doSearch || (searchTerm === '' && exchangeFilter === '');
  
    if (!shouldFetch) return;
  
    const fetchAndCache = async () => {
      setIsLoading(true);
  
      const cachedData = searchTerm && exchangeFilter
        ? null
        : await getCachedData(searchTerm, exchangeFilter, sortOrder, page);
  
      if (cachedData) {
        setIndices(cachedData.items);
        setTotalPages(cachedData.totalPages);
      } else {
        try {
          const data = await fetchData(searchTerm, exchangeFilter, sortOrder, page, pageSize);
          setIndices(data.items || []);
          setTotalPages(data.totalPages || 1);
          await saveCache(data, searchTerm, exchangeFilter, sortOrder, page);
        } catch (err) {
          console.error('Erreur chargement indices', err);
        }
      }
  
      setIsLoading(false);
      setDoSearch(false); // Reset flag après la recherche
    };
  
    fetchAndCache();
  }, [doSearch, searchTerm, exchangeFilter, sortOrder, page, pageSize, fetchData, getCachedData, saveCache]);
  
  const handleSearchClick = () => {
    setPage(1);
    setDoSearch(true);
  };

  const handleSearchInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setSearchTerm(value);
    setPage(1);
    if (value === '') {
      setDoSearch(true); // reset auto quand input est vidé
    }
  };

  const handleExchangeFilter = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setExchangeFilter(e.target.value);
    setPage(1);
  };

  const handleSort = (key: string) => {
    const newSortOrder = (prev: string) => {
      console.log(key)
      switch (key) {
        case 'symbol':
          return prev === 'symbol_desc' ? 'symbol_asc' : 'symbol_desc';
        case 'price':
          return prev === 'price_desc' ? 'price_asc' : 'price_desc';
        case 'change':
          return prev === 'change_asc' ? 'change_desc' : 'change_asc';
        case 'exercfinanc':
          return prev === 'exercfinanc_asc' ? 'exercfinanc_desc' : 'exercfinanc_asc';
        case 'bourse':
          return prev === 'bourse_asc' ? 'bourse_desc' : 'bourse_asc';
        case 'label':
          return prev === 'label_asc' ? 'label_desc' : 'label_asc';
        case 'prob':
          return prev === 'prob_desc' ? 'prob_asc' : 'prob_desc';
        default:
          return prev;
      }
    };
    setSortOrder(newSortOrder(sortOrder));
    setPage(1); // Remet à la première page pour chaque changement de tri
  };

  // const handleSort = (key: string) => {
  //   setSortOrder((prev) => (prev === key ? `${key}_desc` : `${key}_asc`));
  //   setPage(1);
  // };

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

  const formatDate = (dates: Date[]): string[] => {
    return dates.map(dateStr => {
      const date = new Date(dateStr);
      return date instanceof Date && !isNaN(date.getTime())
        ? date.toLocaleDateString()
        : "Date invalide";
    });
  };

  return (
    <div className='list-container'>
      <div className='list-filter'>
        <select 
          value={exchangeFilter}
          onChange={handleExchangeFilter}
        >
          <option value="">{t('list.filter')}</option>
          <option value="TOR">Toronto</option>
          <option value="NYC">New York</option>
        </select>

        <input
          type="text"
          placeholder={t('list.search')}
          className="list-search"
          value={searchTerm}
          onChange={handleSearchInput}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault(); // évite le comportement par défaut (soumission formulaire, etc.)
              handleSearchClick(); // déclenche la recherche
            }
          }}
        />
        <Button onClick={handleSearchClick} label={t('list.button-search') || 'Search'} />
      </div>
      <div className="flex justify-between items-center mt-4">
              <Button
                  onClick={() => setPage(p => Math.max(p - 1, 1))}
                  label={t('list.button-previous')}
                  disabled={page === 1}
              />

              <span>{t('list.page')} {page} {t('list.on')} {totalPages}</span>

              <Button
                  onClick={() => {
                    setPage(p => {
                      const newPage = Math.min(p + 1, totalPages);
                      return newPage;
                    });
                  }}
                  label={t('list.button-next')}
                  disabled={page === totalPages}
              />
        </div>
      <table className="table">
        <thead>
            <tr>
                <th className='orderColumn' onClick={() => handleSort('symbol')}>{t('list.table.symbol')}
                    {sortOrder === 'symbol_asc' || sortOrder === 'symbol_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                            {sortOrder.endsWith('_desc') ? '🔽' :
                            sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
                        </span>
                    ) : (
                      <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
                          ⇅
                      </span>
                  )}
                </th>
                <th >{t('list.table.name')}</th>
                <th >{t('list.table.close')}</th>
                <th className='orderColumn' onClick={() => handleSort('price')}>{t('list.table.price')}
                    {sortOrder === 'price_asc' || sortOrder === 'price_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                            {sortOrder.endsWith('_desc') ? '🔽' :
                            sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
                        </span>
                    ) : (
                      <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
                          ⇅
                      </span>
                  )}
                </th>
                <th className='orderColumn' onClick={() => handleSort('change')}>{t('list.table.change')}
                    {sortOrder === 'change_asc' || sortOrder === 'change_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                            {sortOrder.endsWith('_desc') ? '🔽' : 
                            sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
                        </span>
                    ) : (
                      <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
                          ⇅
                      </span>
                  )}
                </th>
                <th >{t('list.table.type')}</th>
                {role === 'premium' && (
                  <>
                    <th className='orderColumn' onClick={() => handleSort('exercfinanc')}>{t('list.table.dates')}
                      {sortOrder === 'exercfinanc_asc' || sortOrder === 'exercfinanc_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                          {sortOrder.endsWith('_desc') ? '🔽' :
                            sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
                        </span>
                      ) : (
                        <span style={{ marginLeft: '5px', color: 'darkslategray' }}>
                          ⇅
                        </span>
                      )}
                    </th>
                  </>
                )}
                <th className='orderColumn' onClick={() => handleSort('bourse')}>{t('list.table.exchange')}
                    {sortOrder === 'bourse_asc' || sortOrder === 'bourse_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                            {sortOrder.endsWith('_desc') ? '🔽' : 
                            sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
                        </span>
                    ) : (
                      <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
                          ⇅
                      </span>
                  )}
                </th>
                <th >{t('list.table.volume')}</th>
                <th className='orderColumn' onClick={() => handleSort('label')}>{t('list.table.label')}
                    {sortOrder === 'label_asc' || sortOrder === 'label_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                            {sortOrder.endsWith('_desc') ? '🔽' : 
                            sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
                        </span>
                    ) : (
                      <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
                          ⇅
                      </span>
                  )}
                </th>
                {role === 'premium' && (
                  <>
                    <th >{t('list.table.recommendation')}</th>
                    <th>{t('list.table.analysis')}</th>
                  </>
                )}
                <th id='update'>{t('list.table.updated')}</th>
                {role === 'premium' && (
                  <>
                    <th className='orderColumn' onClick={() => handleSort('prob')}>{t('list.table.probability')}
                      {sortOrder === 'prob_asc' || sortOrder === 'prob_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                          {sortOrder.endsWith('_desc') ? '🔽' : '🔼'}                            {sortOrder.endsWith('_desc') ? '🔽' :
                            sortOrder.endsWith('_asc') ? '🔼' : '⇅'}                        </span>
                      ) : (
                        <span style={{ marginLeft: '5px', color: 'darkslategray' }}>
                          ⇅
                        </span>
                      )}
                    </th>
                  </>
                )}
                <th></th>
            </tr>
        </thead>
        <tbody>
          {isLoading ? (
            <tr className='chargement'>
              <td colSpan={13} className="chargement-list">
              <div className="spinner"></div><br /> {/* Ajoute ici ton spinner */}
                {t('list.loading')} {/* ou un spinner ici */}
              </td>
            </tr>
          ) : indices.length === 0 ? (
            <tr>
              <td colSpan={13} className="chargement-list">
                {t('list.resultat')} {/* Aucun résultat */}
              </td>
            </tr>
          ) : (
          indices.map((indice, index) => (
            <tr key={index} className="border-t">
              <td className='textbold'>{indice.symbol}</td>
              <td className='textbold'>{indice.name}</td>
              <td>{indice.regularMarketPreviousClose?.toFixed(3)}</td>
              <td>{indice.regularMarketPrice?.toFixed(3)}</td>
              <td className={indice.regularMarketChange > 0 ? "color-positive" : "color-negative"}>{indice.regularMarketChange?.toFixed(3)}</td>
              <td>{indice.quoteType}</td>
              {role === 'premium' && (
                <>
                  <td>
                    {indice.datesExercicesFinancieres && indice.datesExercicesFinancieres.length > 0 ? (
                      indice.datesExercicesFinancieres.map((date, i) => {
                        const isMinDate = new Date(date).getFullYear() === 1; // Vérifie si c'est la date min
                        return (
                          <span key={i} className="block">
                            {isMinDate ? 'N/A' : formatDate([new Date(date)])}<br />
                          </span>
                        );
                      })
                    ) : (
                      <span>N/A</span> // S'il n'y a aucune date
                    )}
                  </td>
                </>
              )}
              <td>{indice.bourse}</td>
              <td>{indice.regularMarketVolume}</td>
              <td className={indice.label ? "color-positive" : "color-negative"}>{indice.label ? "UP" : "DOWN"}</td>
              {role === 'premium' && (
                <>
                  <td className={`rec-cell ${getRecommendationClass(indice.raccomandation)}`}>{indice.raccomandation}</td>
                  <td>
                    {indice.analysis ? (
                      <ul style={{ paddingLeft: 0, margin: -10 }}>
                        {Object.entries(indice.analysis).map(([key, value]) => (

                            <li 
                              key={index}
                              style={{ listStyle: "none", padding: 0, margin: 0 }}
                            >
                              <span 
                                style={{
                                  whiteSpace: "nowrap",
                                }}
                              >
                                <strong className={`rec-cell ${getRecommendationClass(key)}`}>{key}: {value}</strong>
                              </span>
                            </li>
                          )
                        )}
                      </ul>
                    ) : (
                      <span>N/A</span>
                    )}
                  </td>
                </>
              )}
              <td>{new Date(indice.dateUpdated).toDateString()}</td>
              {role === 'premium' && (
                <>
                  <td className={indice.probability > 0.49 ? "color-positive" : "color-negative"}>{(indice.probability * 100).toFixed(1)}%</td>
                </>
              )} 
              <td>
              {role === 'premium' || role === 'public' ? (
                  <Link to={`/details/${indice.symbol}?returnUrl=${returnUrl}`}>
                    <button className="button">Details</button>
                  </Link>
                ) : (
                  <button
                    className="button"
                    onClick={() => navigate('/registerForm')}
                  >
                    Details
                  </button>
                )}
              </td>
            </tr>
          )))}
        </tbody>
      </table>

        <div className="flex justify-between items-center mt-4">
              <Button
                  onClick={() => setPage(p => Math.max(p - 1, 1))}
                  label={t('list.button-previous')}
                  disabled={page === 1}
              />

              <span>{t('list.page')} {page} {t('list.on')} {totalPages}</span>

              <Button
                  onClick={() => {
                    setPage(p => {
                      const newPage = Math.min(p + 1, totalPages);
                      return newPage;
                    });
                  }}
                  label={t('list.button-next')}
                  disabled={page === totalPages}
              />
        </div>
    </div>
  );
};

export default IndiceExplorer;
