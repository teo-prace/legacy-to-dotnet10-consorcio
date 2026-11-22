IF OBJECT_ID('FN_CPF_VALIDO') IS NOT NULL DROP FUNCTION FN_CPF_VALIDO
GO
/*
  Valida digitos verificadores do CPF (somente numeros, 11 posicoes).
  2005-03 R.A.
  2009-11 R.A. rejeita CPF com todos os digitos iguais
*/
CREATE FUNCTION FN_CPF_VALIDO (@CPF VARCHAR(11))
RETURNS BIT
AS
BEGIN
    DECLARE @I INT, @SOMA INT, @DV1 INT, @DV2 INT

    IF @CPF IS NULL OR LEN(@CPF) <> 11 OR @CPF LIKE '%[^0-9]%'
        RETURN 0

    IF REPLICATE(LEFT(@CPF, 1), 11) = @CPF
        RETURN 0

    SET @SOMA = 0
    SET @I = 1
    WHILE @I <= 9
    BEGIN
        SET @SOMA = @SOMA + CAST(SUBSTRING(@CPF, @I, 1) AS INT) * (11 - @I)
        SET @I = @I + 1
    END
    SET @DV1 = 11 - (@SOMA % 11)
    IF @DV1 >= 10 SET @DV1 = 0

    SET @SOMA = 0
    SET @I = 1
    WHILE @I <= 10
    BEGIN
        SET @SOMA = @SOMA + CAST(SUBSTRING(@CPF, @I, 1) AS INT) * (12 - @I)
        SET @I = @I + 1
    END
    SET @DV2 = 11 - (@SOMA % 11)
    IF @DV2 >= 10 SET @DV2 = 0

    IF @DV1 = CAST(SUBSTRING(@CPF, 10, 1) AS INT) AND @DV2 = CAST(SUBSTRING(@CPF, 11, 1) AS INT)
        RETURN 1

    RETURN 0
END
GO
