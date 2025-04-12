import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { IndiceDTO } from '../Models/IndiceDTO.ts';
import { getIndices } from '../Data/dataApi.ts';
import { Button } from '../Components/ui/button.tsx';
import { useTranslation } from 'react-i18next';

const IndiceExplorer: React.FC = () => {
  const [indices, setIndices] = useState<IndiceDTO[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [exchangeFilter, setExchangeFilter] = useState('');
  const [sortOrder, setSortOrder] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [pageSize] = useState(50);

  //  Configuration for using translation with json file
  const { t } = useTranslation();

  useEffect(() => {
    getIndices(searchTerm, exchangeFilter, sortOrder, page, pageSize)
      .then((data: any) => {
        setIndices(data.items || []);
        console.log(data)
        setTotalPages(data.totalPages || 1);
      })
      .catch((err) => console.error('Erreur chargement indices', err));
  }, [searchTerm, exchangeFilter, sortOrder, page, pageSize]);
  
  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(e.target.value);
    setPage(1); // reset page
  };

  const handleExchangeFilter = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setExchangeFilter(e.target.value);
    setPage(1);
  };

  const handleSort = (key: string) => {
    setSortOrder((prev) => {
      const newOrder = prev === key ? `${key}_desc` : key;
      console.log("Sort Order Changed:", newOrder);  // Debugging
      return newOrder;
    });
    setPage(1);
  };

  const getRecommendationClass = (rec: string): string => {
    switch (rec) {
      case 'Strong Buy':
        return 'text-strong-buy';
      case 'Buy':
        return 'text-buy';
      case 'Hold':
        return 'text-hold';
      case 'Sell':
        return 'text-sell';
      case 'Strong Sell':
        return 'text-strong-sell';
      default:
        return '';
    }
  };

  // Fonction pour formater les dates
  const formatDate = (dates: Date[]): string[] => {
      return dates.map(dateStr => {
          const date = new Date(dateStr); // Convertir la chaîne en objet Date
          return date instanceof Date && !isNaN(date.getTime()) ? date.toLocaleDateString() : "Date invalide";
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
                    {sortOrder === 'symbol' || sortOrder === 'symbol_desc' ? (
                        <span>
                            {sortOrder.endsWith('_desc') ? '🔽' : '🔼'}
                        </span>
                    ) : (
                      <span>
                          ⇅
                      </span>
                  )}
                </th>
                <th >{t('list.table.name')}</th>
                <th >{t('list.table.close')}</th>
                <th className='orderColumn' onClick={() => handleSort('price')}>{t('list.table.price')}
                    {sortOrder === 'price' || sortOrder === 'price_desc' ? (
                        <span>
                            {sortOrder.startsWith('price') ? sortOrder.endsWith('_desc') ? '🔽' : '🔼' : '⇅'}
                        </span>
                    ) : (
                      <span>
                          ⇅
                      </span>
                  )}
                </th>
                <th className='orderColumn' onClick={() => handleSort('change')}>{t('list.table.change')}
                    {sortOrder === 'change' || sortOrder === 'change_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                            {sortOrder.endsWith('_desc') ? '🔽' : '🔼'}
                        </span>
                    ) : (
                      <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
                          ⇅
                      </span>
                  )}
                </th>
                <th >{t('list.table.type')}</th>
                <th className='orderColumn' onClick={() => handleSort('exercfinanc')}>{t('list.table.dates')}
                    {sortOrder === 'exercfinanc' || sortOrder === 'exercfinanc_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                            {sortOrder.endsWith('_desc') ? '🔽' : '🔼'}
                        </span>
                    ) : (
                      <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
                          ⇅
                      </span>
                  )}
                </th>
                <th className='orderColumn' onClick={() => handleSort('bourse')}>{t('list.table.exchange')}
                    {sortOrder === 'bourse' || sortOrder === 'bourse_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                            {sortOrder.endsWith('_desc') ? '🔽' : '🔼'}
                        </span>
                    ) : (
                      <span style={{ marginLeft: '5px' , color: 'darkslategray'}}>
                          ⇅
                      </span>
                  )}
                </th>
                <th className='orderColumn' onClick={() => handleSort('label')}>{t('list.table.label')}
                    {sortOrder === 'label' || sortOrder === 'label_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                            {sortOrder.endsWith('_desc') ? '🔽' : '🔼'}
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
                    {sortOrder === 'prob' || sortOrder === 'prob_desc' ? (
                        <span style={{ marginLeft: '5px' }}>
                            {sortOrder.endsWith('_desc') ? '🔽' : '🔼'}
                        </span>
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
          {indices.length === 0 && (
            <tr>
              <td colSpan={7}>{t('list.resultat')}</td>
            </tr>
          )}
          {indices.map((indice, index) => (
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
                <Link to={`/details/${indice.symbol}?returnUrl=list`}>
                  <button className="button">Details</button>
                </Link>
              </td>
            </tr>
          ))}
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
// import { useTranslation } from 'react-i18next';
// import '../Css/list.css'

// type IndiceDTO = {
//     symbol: string;
//     name: string;
//     regularMarketPreviousClose: number;
//     regularMarketPrice: number;
//     regularMarketChange: number;
//     quoteType: string;
//     datesExercicesFinancieres: string[];
//     bourse: string;
//     label: boolean;
//     raccomandation: string;
//     dateUpdated: string;
//     probability: number;
// };

// type Props = {
//     indices: IndiceDTO[];
//     exchanges: { [key: string]: string };
// };

// const getRecommendationColor = (raccomandation: string) => {
//     switch (raccomandation) {
//         case 'Strong Buy': return 'green';
//         case 'Buy': return 'lightgreen';
//         case 'Hold': return 'lightblue';
//         case 'Sell': return 'orange';
//         case 'Strong Sell': return 'red';
//         default: return 'black';
//     }
// };

// const formatDate = (dates?: string[]) => {
//     if (!dates || dates.length === 0) return ['N/A'];
//     return dates.map(d => new Date(d).toLocaleDateString('fr-FR'));
// };

// const IndiceExplorer: React.FC<Props> = ({ indices, exchanges }) => {
//     const [filtered, setFiltered] = useState<IndiceDTO[]>([]);
//     const [exchangeFilter, setExchangeFilter] = useState('');
//     const [searchTerm, setSearchTerm] = useState('');
//     const [currentPage, setCurrentPage] = useState(1);
//     const pageSize = 50;

//     //  Configuration for using translation with json file
//     const { t } = useTranslation();

//     useEffect(() => {
//         let data = indices;
//         if (exchangeFilter) data = data.filter(i => i.bourse === exchangeFilter);
//         if (searchTerm) data = data.filter(i => i.name.toLowerCase().includes(searchTerm.toLowerCase()));
//         setFiltered(data);
//         setCurrentPage(1);
//     }, [exchangeFilter, searchTerm, indices]);

//     const pagedData = filtered.slice((currentPage - 1) * pageSize, currentPage * pageSize);
//     const totalPages = Math.ceil(filtered.length / pageSize);

//     return (
//         <div>
//             <h1 className='list-title'>{t('list.title')}</h1>
//             <div className='list-container'>
//                 <div style={{ display: 'flex', gap: '1rem', marginBottom: '1rem' }}>
//                     <div className='list-filter'>
//                         <label>{t('list.filter.label')}</label>
//                         <select onChange={e => setExchangeFilter(e.target.value)} value={exchangeFilter}>
//                             <option value="">{t('list.filter.select')}</option>
//                             {Object.entries(exchanges).map(([key, value]) => (
//                                 <option key={key} value={key}>{value}</option>
//                             ))}
//                         </select>
//                     </div>

//                     <div className='list-search'>
//                         <label>{t('list.search.label')}</label>
//                         <input
//                             type="text"
//                             placeholder={t('list.search.select')}
//                             value={searchTerm}
//                             onChange={e => setSearchTerm(e.target.value)}
//                         />
//                     </div>
//                 </div>

//                 <table className="table">
//                     <thead>
//                         <tr>
//                             <th>{t('list.table.symbol')}</th>
//                             <th>{t('list.table.name')}</th>
//                             <th>{t('list.table.close')}</th>
//                             <th>{t('list.table.price')}</th>
//                             <th>{t('list.table.change')}</th>
//                             <th>{t('list.table.type')}</th>
//                             <th>{t('list.table.dates')}</th>
//                             <th>{t('list.table.exchange')}</th>
//                             <th>{t('list.table.label')}</th>
//                             <th>{t('list.table.recommendation')}</th>
//                             <th>{t('list.table.updated')}</th>
//                             <th>{t('list.table.probability')}</th>
//                             <th></th>
//                         </tr>
//                     </thead>
//                     <tbody>
//                         {pagedData.map((item) => (
//                             <tr key={item.symbol}>
//                                 <td>{item.symbol}</td>
//                                 <td>{item.name}</td>
//                                 <td>{item.regularMarketPreviousClose}</td>
//                                 <td>{item.regularMarketPrice}</td>
//                                 <td style={{ color: item.regularMarketChange < 0 ? 'lightcoral' : 'green', fontWeight: 'bold' }}>
//                                     {item.regularMarketChange}
//                                 </td>
//                                 <td>{item.quoteType}</td>
//                                 <td style={{ whiteSpace: 'nowrap' }}>
//                                     {formatDate(item.datesExercicesFinancieres).map((d, i) => (
//                                         <div key={i}>{d}</div>
//                                     ))}
//                                 </td>
//                                 <td>{item.bourse}</td>
//                                 <td style={{ color: item.label ? 'green' : 'lightcoral', fontWeight: 'bold' }}>
//                                     {item.label ? 'UP' : 'DOWN'}
//                                 </td>
//                                 <td style={{ color: getRecommendationColor(item.raccomandation), fontWeight: 'bold' }}>
//                                     {item.raccomandation}
//                                 </td>
//                                 <td>{new Date(item.dateUpdated).toLocaleDateString()}</td>
//                                 <td style={{ color: item.probability > 0.49 ? 'green' : 'lightcoral', fontWeight: 'bold' }}>
//                                     {item.probability ? `${Math.round(item.probability * 100)}%` : 'N/A'}
//                                 </td>
//                                 <td>
//                                     <a href={`/details/${item.symbol}`}>Details</a>
//                                 </td>
//                             </tr>
//                         ))}
//                     </tbody>
//                 </table>

//                 {/* <nav style={{ marginTop: '1rem' }}>
//                     <ul className="pagination">
//                         {currentPage > 1 && (
//                             <li><button onClick={() => setCurrentPage(p => p - 1)}>Previous</button></li>
//                         )}
//                         {Array.from({ length: totalPages }, (_, i) => i + 1).slice(Math.max(0, currentPage - 5), currentPage + 4).map(page => (
//                             <li key={page}>
//                                 <button
//                                     onClick={() => setCurrentPage(page)}
//                                     style={{ fontWeight: currentPage === page ? 'bold' : 'normal' }}
//                                 >
//                                     {page}
//                                 </button>
//                             </li>
//                         ))}
//                         {currentPage < totalPages && (
//                             <li><button onClick={() => setCurrentPage(p => p + 1)}>Next</button></li>
//                         )}
//                     </ul>
//                 </nav> */}
//             </div>
//         </div>
//     );
// };

// export default IndiceExplorer;
