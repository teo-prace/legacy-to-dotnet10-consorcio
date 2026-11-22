IF OBJECT_ID('FN_NUMERO_SORTEADO') IS NOT NULL DROP FUNCTION FN_NUMERO_SORTEADO
GO
/*
  Numero da cota a partir de um premio da Loteria Federal (regulamento CeCons):
  tres ultimos algarismos ('000' vale 1000); acima do numero de cotas do
  grupo -> ((n - 1) mod maximo) + 1.
  2014-08 J.P.  (regra copiada do SGCApuracao VB6)
*/
CREATE FUNCTION FN_NUMERO_SORTEADO (@PREMIO CHAR(5), @QT_COTAS SMALLINT)
RETURNS SMALLINT
AS
BEGIN
    DECLARE @N INT
    SET @N = CAST(RIGHT(@PREMIO, 3) AS INT)
    IF @N = 0 SET @N = 1000
    RETURN ((@N - 1) % @QT_COTAS) + 1
END
GO
