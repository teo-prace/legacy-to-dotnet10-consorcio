IF OBJECT_ID('FN_DV_MODULO11') IS NOT NULL DROP FUNCTION FN_DV_MODULO11
GO
/*
  DV modulo 11 do nosso numero (banco 999): pesos 2 a 9 da direita para a
  esquerda; DV = 11 - resto; 10 e 11 viram 0.
  2014-05 J.P.
*/
CREATE FUNCTION FN_DV_MODULO11 (@NUM VARCHAR(30))
RETURNS CHAR(1)
AS
BEGIN
    DECLARE @I INT, @PESO INT, @SOMA INT, @DV INT

    SET @SOMA = 0
    SET @PESO = 2
    SET @I = LEN(@NUM)
    WHILE @I >= 1
    BEGIN
        SET @SOMA = @SOMA + CAST(SUBSTRING(@NUM, @I, 1) AS INT) * @PESO
        SET @PESO = CASE WHEN @PESO = 9 THEN 2 ELSE @PESO + 1 END
        SET @I = @I - 1
    END

    SET @DV = 11 - (@SOMA % 11)
    IF @DV >= 10 SET @DV = 0

    RETURN CAST(@DV AS CHAR(1))
END
GO
