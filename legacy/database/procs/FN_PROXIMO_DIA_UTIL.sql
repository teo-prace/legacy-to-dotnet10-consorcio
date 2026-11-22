IF OBJECT_ID('FN_PROXIMO_DIA_UTIL') IS NOT NULL DROP FUNCTION FN_PROXIMO_DIA_UTIL
GO
/*
  Proximo dia util a partir da data (a propria data, se ja for util).
  Sabado, domingo e TB_FERIADO.
  Obs.: 01/01/1900 foi segunda-feira -> resto 5 = sabado, 6 = domingo
        (nao depende do SET DATEFIRST).
  2014-05 J.P.
*/
CREATE FUNCTION FN_PROXIMO_DIA_UTIL (@DT DATETIME)
RETURNS DATETIME
AS
BEGIN
    DECLARE @D DATETIME
    SET @D = DATEADD(DAY, DATEDIFF(DAY, 0, @DT), 0)

    WHILE DATEDIFF(DAY, '19000101', @D) % 7 IN (5, 6)
       OR EXISTS (SELECT 1 FROM TB_FERIADO WHERE DT_FERIADO = @D)
    BEGIN
        SET @D = DATEADD(DAY, 1, @D)
    END

    RETURN @D
END
GO
