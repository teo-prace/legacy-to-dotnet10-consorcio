IF OBJECT_ID('FN_COTA_APTA') IS NOT NULL DROP FUNCTION FN_COTA_APTA
GO
/*
  Cota apta a contemplacao na data:
    - ativa (nao contemplada) e com adesao ate a data
    - sem parcela vencida em aberto (prazo sem encargos = proximo dia util
      apos o vencimento)
  2014-08 J.P.
*/
CREATE FUNCTION FN_COTA_APTA (@CD_COTA INT, @DT DATETIME)
RETURNS BIT
AS
BEGIN
    DECLARE @DIA DATETIME
    SET @DIA = DATEADD(DAY, DATEDIFF(DAY, 0, @DT), 0)

    IF NOT EXISTS (SELECT 1 FROM TB_COTA WHERE CD_COTA = @CD_COTA AND ID_SITUACAO = 1 AND DT_ADESAO < @DIA + 1)
        RETURN 0

    IF EXISTS (SELECT 1
                 FROM TB_PARCELA
                WHERE CD_COTA = @CD_COTA
                  AND ID_SITUACAO IN (1, 2)
                  AND DT_VENCIMENTO IS NOT NULL
                  AND DT_VENCIMENTO < @DIA
                  AND dbo.FN_PROXIMO_DIA_UTIL(DT_VENCIMENTO) < @DIA)
        RETURN 0

    RETURN 1
END
GO
