
import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { IndiceDTO } from '../Models/IndiceDTO.ts';
import { Button } from '../Components/ui/button.tsx';
import { useTranslation } from 'react-i18next';

type Props = {
  fetchData: (
    search: string,
    exchange: string,
    sort: string,
    page: number,
    pageSize: number
  ) => Promise<any>;
  returnUrl: string; // Ou tu peux typer selon la structure retournée
  saveCache: (data: { items: any[]; totalPages: number }) => Promise<void>;
  getCachedData: () => Promise<{ items: any[]; totalPages: number } | null>;
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

  useEffect(() => {
    const loadData = async () => {
      setIsLoading(true);

      const cachedData = await getCachedData();
      if (cachedData && cachedData.items && cachedData.totalPages) {
        setIndices(cachedData.items);
        setTotalPages(cachedData.totalPages);
        setIsLoading(false);
      } else {
        try {
          const data = await fetchData(searchTerm, exchangeFilter, sortOrder, page, pageSize);
          setIndices(data.items || []);
          setTotalPages(data.totalPages || 1);
          await saveCache(data); // Sauvegarde dans le bon cache
        } catch (err) {
          console.error('Erreur chargement indices', err);
        } finally {
          setIsLoading(false);
        }
      }
    };

    loadData();
  }, [searchTerm, exchangeFilter, sortOrder, page, pageSize, fetchData, saveCache, getCachedData]);

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(e.target.value);
    setPage(1);
  };

  const handleExchangeFilter = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setExchangeFilter(e.target.value);
    setPage(1);
  };

  const handleSort = (key: string) => {
    const newSortOrder = (prev) => {
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
    console.log(sortOrder)
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

        {/* <select
          value={pageSize}
          onChange={(e) => {
            setPageSize(Number(e.target.value));
            setPage(1);
          }}
        >
          {pageSizeOptions.map(size => (
            <option key={size} value={size}>{size} par page</option>
          ))}
        </select> */}

        <input
          type="text"
          placeholder={t('list.search')}
          className="list-search"
          value={searchTerm}
          onChange={handleSearchChange}
        />

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
                <th className='orderColumn' onClick={() => handleSort('exercfinanc')}>{t('list.table.dates')}
                    {sortOrder === 'exercfinanc_asc' || sortOrder === 'exercfinanc_desc' ? (
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
                <th >{t('list.table.recommendation')}</th>
                <th id='update'>{t('list.table.updated')}</th>
                <th className='orderColumn' onClick={() => handleSort('prob')}>{t('list.table.probability')}
                    {sortOrder === 'prob_asc' || sortOrder === 'prob_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                            {sortOrder.endsWith('_desc') ? '🔽' : '🔼'}                            {sortOrder.endsWith('_desc') ? '🔽' : 
                            sortOrder.endsWith('_asc') ? '🔼' : '⇅'}                        </span>
                    ) : (
                      <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
                          ⇅
                      </span>
                  )}
                </th>
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
              <td>
              {indice.datesExercicesFinancieres && formatDate(indice.datesExercicesFinancieres).map((d, i) => (
                  <span key={i} className="block">{d}<br /></span>
                ))}
              </td>
              <td>{indice.bourse}</td>
              <td className={indice.label ? "color-positive" : "color-negative"}>{indice.label ? "UP" : "DOWN"}</td>
              <td className={`rec-cell ${getRecommendationClass(indice.raccomandation)}`}>{indice.raccomandation}</td>
              <td>{new Date(indice.dateUpdated).toDateString()}</td>
              <td className={indice.probability > 0.49 ? "color-positive" : "color-negative"}>{(indice.probability * 100).toFixed(1)}%</td> 
              <td>
                <Link to={`/details/${indice.symbol}?returnUrl=${returnUrl}`}>
                  <button className="button">Details</button>
                </Link>
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

// import React, { useEffect, useState } from 'react';
// import { Link } from 'react-router-dom';
// import { IndiceDTO } from '../Models/IndiceDTO.ts';
// import { Button } from '../Components/ui/button.tsx';
// import { getCachedList, saveList } from '../Data/dataCache.ts';  // Importation des fonctions
// import { useTranslation } from 'react-i18next';

// type Props = {
//   fetchData: (
//     search: string,
//     exchange: string,
//     sort: string,
//     page: number,
//     pageSize: number
//   ) => Promise<any>;
//   returnUrl: string; // Ou tu peux typer selon la structure retournée
// };

// const IndiceExplorer: React.FC<Props> = ({ fetchData, returnUrl }) => {
//   const [indices, setIndices] = useState<IndiceDTO[]>([]);
//   const [searchTerm, setSearchTerm] = useState('');
//   const [exchangeFilter, setExchangeFilter] = useState('');
//   const [sortOrder, setSortOrder] = useState('');
//   const [page, setPage] = useState(1);
//   const [totalPages, setTotalPages] = useState(1);
//   const [pageSize] = useState(50);
//   const [isLoading, setIsLoading] = useState(false);
//   const { t } = useTranslation();

//   useEffect(() => {
//     setIsLoading(true);

//     fetchData(searchTerm, exchangeFilter, sortOrder, page, pageSize)
//       .then((data: any) => {
//         setIndices(data.items || []);
//         setTotalPages(data.totalPages || 1);
//       })
//       .catch((err) => console.error('Erreur chargement indices', err))
//       .finally(() => setIsLoading(false));
//   }, [searchTerm, exchangeFilter, sortOrder, page, pageSize, fetchData]);

//   const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
//     setSearchTerm(e.target.value);
//     setPage(1);
//   };

//   const handleExchangeFilter = (e: React.ChangeEvent<HTMLSelectElement>) => {
//     setExchangeFilter(e.target.value);
//     setPage(1);
//   };

//   const handleSort = (key: string) => {
//     const newSortOrder = (prev) => {
//       switch (key) {
//         case 'symbol':
//           return prev === 'symbol_desc' ? 'symbol_asc' : 'symbol_desc';
//         case 'price':
//           return prev === 'price_desc' ? 'price_asc' : 'price_desc';
//         case 'change':
//           return prev === 'change_asc' ? 'change_desc' : 'change_asc';
//         case 'exercfinanc':
//           return prev === 'exercfinanc_asc' ? 'exercfinanc_desc' : 'exercfinanc_asc';
//         case 'bourse':
//           return prev === 'bourse_asc' ? 'bourse_desc' : 'bourse_asc';
//         case 'label':
//           return prev === 'label_asc' ? 'label_desc' : 'label_asc';
//         case 'prob':
//           return prev === 'prob_desc' ? 'prob_asc' : 'prob_desc';
//         default:
//           return prev;
//       }
//     };
//     setSortOrder(newSortOrder(sortOrder));
//     console.log(sortOrder)
//     setPage(1); // Remet à la première page pour chaque changement de tri
//   };

//   // const handleSort = (key: string) => {
//   //   setSortOrder((prev) => (prev === key ? `${key}_desc` : `${key}_asc`));
//   //   setPage(1);
//   // };

//   const getRecommendationClass = (rec: string): string => {
//     switch (rec) {
//       case 'Strong Buy': return 'text-strong-buy';
//       case 'Buy': return 'text-buy';
//       case 'Hold': return 'text-hold';
//       case 'Sell': return 'text-sell';
//       case 'Strong Sell': return 'text-strong-sell';
//       default: return '';
//     }
//   };

//   const formatDate = (dates: Date[]): string[] => {
//     return dates.map(dateStr => {
//       const date = new Date(dateStr);
//       return date instanceof Date && !isNaN(date.getTime())
//         ? date.toLocaleDateString()
//         : "Date invalide";
//     });
//   };

//   return (
//     <div className='list-container'>
//       <div className='list-filter'>
//         <select 
//           value={exchangeFilter}
//           onChange={handleExchangeFilter}
//         >
//           <option value="">{t('list.filter')}</option>
//           <option value="TOR">Toronto</option>
//           <option value="NYC">New York</option>
//         </select>

//         {/* <select
//           value={pageSize}
//           onChange={(e) => {
//             setPageSize(Number(e.target.value));
//             setPage(1);
//           }}
//         >
//           {pageSizeOptions.map(size => (
//             <option key={size} value={size}>{size} par page</option>
//           ))}
//         </select> */}

//         <input
//           type="text"
//           placeholder={t('list.search')}
//           className="list-search"
//           value={searchTerm}
//           onChange={handleSearchChange}
//         />

//       </div>
//       <div className="flex justify-between items-center mt-4">
//               <Button
//                   onClick={() => setPage(p => Math.max(p - 1, 1))}
//                   label={t('list.button-previous')}
//                   disabled={page === 1}
//               />

//               <span>{t('list.page')} {page} {t('list.on')} {totalPages}</span>

//               <Button
//                   onClick={() => {
//                     setPage(p => {
//                       const newPage = Math.min(p + 1, totalPages);
//                       return newPage;
//                     });
//                   }}
//                   label={t('list.button-next')}
//                   disabled={page === totalPages}
//               />
//         </div>
//       <table className="table">
//         <thead>
//             <tr>
//                 <th className='orderColumn' onClick={() => handleSort('symbol')}>{t('list.table.symbol')}
//                     {sortOrder === 'symbol_asc' || sortOrder === 'symbol_desc' ? (
//                         <span style={{ marginLeft: '5px' }}>
//                             {sortOrder.endsWith('_desc') ? '🔽' :
//                             sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
//                         </span>
//                     ) : (
//                       <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
//                           ⇅
//                       </span>
//                   )}
//                 </th>
//                 <th >{t('list.table.name')}</th>
//                 <th >{t('list.table.close')}</th>
//                 <th className='orderColumn' onClick={() => handleSort('price')}>{t('list.table.price')}
//                     {sortOrder === 'price_asc' || sortOrder === 'price_desc' ? (
//                         <span style={{ marginLeft: '5px' }}>
//                             {sortOrder.endsWith('_desc') ? '🔽' :
//                             sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
//                         </span>
//                     ) : (
//                       <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
//                           ⇅
//                       </span>
//                   )}
//                 </th>
//                 <th className='orderColumn' onClick={() => handleSort('change')}>{t('list.table.change')}
//                     {sortOrder === 'change_asc' || sortOrder === 'change_desc' ? (
//                         <span style={{ marginLeft: '5px' }}>
//                             {sortOrder.endsWith('_desc') ? '🔽' : 
//                             sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
//                         </span>
//                     ) : (
//                       <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
//                           ⇅
//                       </span>
//                   )}
//                 </th>
//                 <th >{t('list.table.type')}</th>
//                 <th className='orderColumn' onClick={() => handleSort('exercfinanc')}>{t('list.table.dates')}
//                     {sortOrder === 'exercfinanc_asc' || sortOrder === 'exercfinanc_desc' ? (
//                         <span style={{ marginLeft: '5px' }}>
//                             {sortOrder.endsWith('_desc') ? '🔽' : 
//                             sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
//                         </span>
//                     ) : (
//                       <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
//                           ⇅
//                       </span>
//                   )}
//                 </th>
//                 <th className='orderColumn' onClick={() => handleSort('bourse')}>{t('list.table.exchange')}
//                     {sortOrder === 'bourse_asc' || sortOrder === 'bourse_desc' ? (
//                         <span style={{ marginLeft: '5px' }}>
//                             {sortOrder.endsWith('_desc') ? '🔽' : 
//                             sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
//                         </span>
//                     ) : (
//                       <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
//                           ⇅
//                       </span>
//                   )}
//                 </th>
//                 <th className='orderColumn' onClick={() => handleSort('label')}>{t('list.table.label')}
//                     {sortOrder === 'label_asc' || sortOrder === 'label_desc' ? (
//                         <span style={{ marginLeft: '5px' }}>
//                             {sortOrder.endsWith('_desc') ? '🔽' : 
//                             sortOrder.endsWith('_asc') ? '🔼' : '⇅'}
//                         </span>
//                     ) : (
//                       <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
//                           ⇅
//                       </span>
//                   )}
//                 </th>
//                 <th >{t('list.table.recommendation')}</th>
//                 <th id='update'>{t('list.table.updated')}</th>
//                 <th className='orderColumn' onClick={() => handleSort('prob')}>{t('list.table.probability')}
//                     {sortOrder === 'prob_asc' || sortOrder === 'prob_desc' ? (
//                         <span style={{ marginLeft: '5px' }}>
//                             {sortOrder.endsWith('_desc') ? '🔽' : '🔼'}                            {sortOrder.endsWith('_desc') ? '🔽' : 
//                             sortOrder.endsWith('_asc') ? '🔼' : '⇅'}                        </span>
//                     ) : (
//                       <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
//                           ⇅
//                       </span>
//                   )}
//                 </th>
//                 <th></th>
//             </tr>
//         </thead>
//         <tbody>
//           {isLoading ? (
//             <tr className='chargement'>
//               <td colSpan={13} className="chargement-list">
//               <div className="spinner"></div><br /> {/* Ajoute ici ton spinner */}
//                 {t('list.loading')} {/* ou un spinner ici */}
//               </td>
//             </tr>
//           ) : indices.length === 0 ? (
//             <tr>
//               <td colSpan={13} className="chargement-list">
//                 {t('list.resultat')} {/* Aucun résultat */}
//               </td>
//             </tr>
//           ) : (
//           indices.map((indice, index) => (
//             <tr key={index} className="border-t">
//               <td className='textbold'>{indice.symbol}</td>
//               <td className='textbold'>{indice.name}</td>
//               <td>{indice.regularMarketPreviousClose?.toFixed(3)}</td>
//               <td>{indice.regularMarketPrice?.toFixed(3)}</td>
//               <td className={indice.regularMarketChange > 0 ? "color-positive" : "color-negative"}>{indice.regularMarketChange?.toFixed(3)}</td>
//               <td>{indice.quoteType}</td>
//               <td>
//               {indice.datesExercicesFinancieres && formatDate(indice.datesExercicesFinancieres).map((d, i) => (
//                   <span key={i} className="block">{d}<br /></span>
//                 ))}
//               </td>
//               <td>{indice.bourse}</td>
//               <td className={indice.label ? "color-positive" : "color-negative"}>{indice.label ? "UP" : "DOWN"}</td>
//               <td className={`rec-cell ${getRecommendationClass(indice.raccomandation)}`}>{indice.raccomandation}</td>
//               <td>{new Date(indice.dateUpdated).toDateString()}</td>
//               <td className={indice.probability > 0.49 ? "color-positive" : "color-negative"}>{(indice.probability * 100).toFixed(1)}%</td> 
//               <td>
//                 <Link to={`/details/${indice.symbol}?returnUrl=${returnUrl}`}>
//                   <button className="button">Details</button>
//                 </Link>
//               </td>
//             </tr>
//           )))}
//         </tbody>
//       </table>

//         <div className="flex justify-between items-center mt-4">
//               <Button
//                   onClick={() => setPage(p => Math.max(p - 1, 1))}
//                   label={t('list.button-previous')}
//                   disabled={page === 1}
//               />

//               <span>{t('list.page')} {page} {t('list.on')} {totalPages}</span>

//               <Button
//                   onClick={() => {
//                     setPage(p => {
//                       const newPage = Math.min(p + 1, totalPages);
//                       return newPage;
//                     });
//                   }}
//                   label={t('list.button-next')}
//                   disabled={page === totalPages}
//               />
//         </div>
//     </div>
//   );
// };

// export default IndiceExplorer;
