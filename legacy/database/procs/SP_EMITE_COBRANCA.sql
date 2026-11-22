IF OBJECT_ID('SP_EMITE_COBRANCA') IS NOT NULL DROP PROCEDURE SP_EMITE_COBRANCA
GO
/*****************************************************************************
  Emissao mensal da cobranca.

  Parcelas ABERTAS com vencimento no mes informado, de cotas ativas ou
  contempladas, em grupos em andamento:
    - valor do bem na data da emissao
    - componentes arredondados ao centavo; parcela = soma dos componentes
    - nosso numero sequencial (10 digitos) + DV modulo 11
  Idempotente: so pega parcelas ainda abertas.
  Retorna o resumo por grupo.

  2005-07 R.A.  versao inicial (carne impresso)
  2014-05 J.P.  nosso numero e boleto registrado no banco 999
  2016-02 J.P.  chamada pelo SGC.Batch
*****************************************************************************/
CREATE PROCEDURE SP_EMITE_COBRANCA
    @NR_ANO_MES INT,
    @DT_EMISSAO DATETIME = NULL,
    @CD_USUARIO INT
AS
SET NOCOUNT ON

DECLARE @DT_INI      DATETIME,
        @DT_FIM      DATETIME,
        @CD_PARCELA  INT,
        @CD_BEM      INT,
        @PC_FC       DECIMAL(9,6),
        @PC_TA       DECIMAL(9,6),
        @PC_FR       DECIMAL(9,6),
        @VL_BEM      MONEY,
        @VL_FC       MONEY,
        @VL_TA       MONEY,
        @VL_FR       MONEY,
        @SEQ         BIGINT,
        @NOSSO       VARCHAR(11),
        @QT          INT,
        @MSG         VARCHAR(200)

IF @DT_EMISSAO IS NULL SET @DT_EMISSAO = GETDATE()

SET @DT_INI = CAST(CAST(@NR_ANO_MES AS VARCHAR) + '01' AS DATETIME)
SET @DT_FIM = DATEADD(MONTH, 1, @DT_INI)
SET @QT = 0

CREATE TABLE #EMITIDAS (CD_PARCELA INT)

DECLARE C_PARCELAS CURSOR LOCAL FAST_FORWARD FOR
    SELECT P.CD_PARCELA, C.CD_BEM, P.PC_FUNDO_COMUM, P.PC_TAXA_ADM, P.PC_FUNDO_RESERVA
      FROM TB_PARCELA P
     INNER JOIN TB_COTA C  ON C.CD_COTA = P.CD_COTA
     INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO
     WHERE P.ID_SITUACAO = 1
       AND P.DT_VENCIMENTO >= @DT_INI
       AND P.DT_VENCIMENTO <  @DT_FIM
       AND C.ID_SITUACAO IN (1, 2)
       AND G.ID_SITUACAO = 2
     ORDER BY G.NR_GRUPO, C.NR_COTA, P.NR_PARCELA

BEGIN TRANSACTION

OPEN C_PARCELAS
FETCH NEXT FROM C_PARCELAS INTO @CD_PARCELA, @CD_BEM, @PC_FC, @PC_TA, @PC_FR

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @VL_BEM = dbo.FN_VALOR_BEM(@CD_BEM, @DT_EMISSAO)
    IF @VL_BEM IS NULL
    BEGIN
        SET @MSG = 'Bem ' + CAST(@CD_BEM AS VARCHAR) + ' sem valor vigente em ' + CONVERT(VARCHAR(10), @DT_EMISSAO, 103) + '.'
        GOTO ERRO
    END

    SET @VL_FC = ROUND(@VL_BEM * @PC_FC / 100, 2)
    SET @VL_TA = ROUND(@VL_BEM * @PC_TA / 100, 2)
    SET @VL_FR = ROUND(@VL_BEM * @PC_FR / 100, 2)

    UPDATE TB_PARAMETRO
       SET @SEQ = CAST(DS_VALOR AS BIGINT) + 1,
           DS_VALOR = CAST(CAST(DS_VALOR AS BIGINT) + 1 AS VARCHAR)
     WHERE NM_PARAMETRO = 'SEQ_NOSSO_NUMERO'
    IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro na sequencia do nosso numero.' GOTO ERRO END

    SET @NOSSO = RIGHT('0000000000' + CAST(@SEQ AS VARCHAR), 10)
    SET @NOSSO = @NOSSO + dbo.FN_DV_MODULO11(@NOSSO)

    UPDATE TB_PARCELA
       SET VL_BEM_BASE      = @VL_BEM,
           VL_FUNDO_COMUM   = @VL_FC,
           VL_TAXA_ADM      = @VL_TA,
           VL_FUNDO_RESERVA = @VL_FR,
           VL_PARCELA       = @VL_FC + @VL_TA + @VL_FR,
           NR_NOSSO_NUMERO  = @NOSSO,
           DT_EMISSAO       = @DT_EMISSAO,
           ID_SITUACAO      = 2
     WHERE CD_PARCELA = @CD_PARCELA
    IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao emitir parcela.' GOTO ERRO END

    INSERT INTO #EMITIDAS VALUES (@CD_PARCELA)
    SET @QT = @QT + 1

    FETCH NEXT FROM C_PARCELAS INTO @CD_PARCELA, @CD_BEM, @PC_FC, @PC_TA, @PC_FR
END

CLOSE C_PARCELAS
DEALLOCATE C_PARCELAS

SET @MSG = CAST(@NR_ANO_MES AS VARCHAR) + ' - ' + CAST(@QT AS VARCHAR) + ' parcelas emitidas'
EXEC SP_GRAVA_LOG @CD_USUARIO, 'EMISSAO_COBRANCA', @MSG

COMMIT TRANSACTION

SELECT G.NR_GRUPO, COUNT(*) AS QT_PARCELAS, SUM(P.VL_PARCELA) AS VL_TOTAL
  FROM #EMITIDAS E
 INNER JOIN TB_PARCELA P ON P.CD_PARCELA = E.CD_PARCELA
 INNER JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA
 INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO
 GROUP BY G.NR_GRUPO
 ORDER BY G.NR_GRUPO

RETURN 0

ERRO:
IF CURSOR_STATUS('local', 'C_PARCELAS') >= 0
BEGIN
    CLOSE C_PARCELAS
    DEALLOCATE C_PARCELAS
END
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
RAISERROR(@MSG, 16, 1)
RETURN 1
GO
