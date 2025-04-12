DROP TABLE IF EXISTS PreviousData;

CREATE TEMP TABLE PreviousData AS
SELECT s1.Id AS CurrentId,
       s2.CurrentPrice AS PreviousCurrentPrice
FROM StockData s1
JOIN StockData s2
  ON s1.IndiceId = s2.IndiceId
     AND s2.Date = (
       SELECT MAX(s3.Date)
       FROM StockData s3
       WHERE s3.IndiceId = s1.IndiceId
         AND s3.Date < s1.Date
     );

UPDATE StockData
SET PrevPrice = (
    SELECT PreviousCurrentPrice
    FROM PreviousData
    WHERE PreviousData.CurrentId = StockData.Id
)
WHERE Id IN (
    SELECT CurrentId
    FROM PreviousData
    JOIN StockData ON StockData.Id = PreviousData.CurrentId
    WHERE PreviousCurrentPrice IS NOT PrevPrice
);