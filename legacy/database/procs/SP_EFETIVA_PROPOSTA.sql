IF OBJECT_ID('SP_EFETIVA_PROPOSTA') IS NOT NULL DROP PROCEDURE SP_EFETIVA_PROPOSTA
GO
/*****************************************************************************
  Efetiva a proposta apos o pagamento da 1a parcela:
    1. confere vaga no grupo da proposta
    2. aloca o menor numero de cota livre (versao seguinte, se o numero ja foi usado)
    3. gera o plano de parcelas
    4. baixa a parcela 1
    5. lanca FC / FR / TA da parcela 1 no caixa do grupo

  O valor da parcela 1 e o gravado na proposta (calculado na tela).
  A taxa de administracao absorve a diferenca de centavos do arredondamento.

  2005-04 R.A.  versao inicial (adesao direta no desktop)
  2011-06 M.S.  passa a partir de TB_PROPOSTA
  2016-09 J.P.  lancamentos por fundo (SG_FUNDO)
*****************************************************************************/
CREATE PROCEDURE SP_EFETIVA_PROPOSTA
    @CD_PROPOSTA    INT,
    @DT_PAGAMENTO   DATETIME,
    @VL_PAGO        MONEY,
    @CD_USUARIO     INT,
    @CD_COTA        INT OUTPUT
AS
SET NOCOUNT ON

DECLARE @ID_SIT_PROPOSTA    TINYINT,
        @NR_PROPOSTA        VARCHAR(12),
        @CD_GRUPO           INT,
        @NR_GRUPO           CHAR(6),
        @CD_CONSORCIADO     INT,
        @CD_BEM             INT,
        @VL_BEM             MONEY,
        @VL_PARCELA         MONEY,
        @ID_SIT_GRUPO       TINYINT,
        @QT_MAX             SMALLINT,
        @QT_PRAZO           SMALLINT,
        @QT_REALIZADAS      SMALLINT,
        @NR_COTA            SMALLINT,
        @NR_VERSAO          TINYINT,
        @CD_PARCELA         INT,
        @PC_FC              DECIMAL(9,6),
        @PC_FR              DECIMAL(9,6),
        @VL_FC              MONEY,
        @VL_FR              MONEY,
        @VL_TA              MONEY,
        @RET                INT,
        @MSG                VARCHAR(200),
        @HIST               VARCHAR(200)

SET @CD_COTA = NULL

SELECT @ID_SIT_PROPOSTA = ID_SITUACAO,
       @NR_PROPOSTA     = NR_PROPOSTA,
       @CD_GRUPO        = CD_GRUPO,
       @CD_CONSORCIADO  = CD_CONSORCIADO,
       @CD_BEM          = CD_BEM,
       @VL_BEM          = VL_BEM_PROPOSTA,
       @VL_PARCELA      = VL_PRIMEIRA_PARCELA
  FROM TB_PROPOSTA
 WHERE CD_PROPOSTA = @CD_PROPOSTA

IF @ID_SIT_PROPOSTA IS NULL
BEGIN
    RAISERROR('Proposta nao encontrada.', 16, 1)
    RETURN 1
END

IF @ID_SIT_PROPOSTA <> 2
BEGIN
    RAISERROR('Proposta %s nao esta aguardando pagamento.', 16, 1, @NR_PROPOSTA)
    RETURN 1
END

IF @VL_PAGO <> @VL_PARCELA
BEGIN
    SET @MSG = 'Valor pago (' + CONVERT(VARCHAR, @VL_PAGO, 1) + ') diferente da 1a parcela (' + CONVERT(VARCHAR, @VL_PARCELA, 1) + ').'
    RAISERROR(@MSG, 16, 1)
    RETURN 1
END

SELECT @ID_SIT_GRUPO  = ID_SITUACAO,
       @NR_GRUPO      = NR_GRUPO,
       @QT_MAX        = QT_COTAS_MAXIMO,
       @QT_PRAZO      = QT_PRAZO_MESES,
       @QT_REALIZADAS = QT_ASSEMBLEIAS_REALIZADAS
  FROM TB_GRUPO
 WHERE CD_GRUPO = @CD_GRUPO

IF @ID_SIT_GRUPO NOT IN (1, 2)
BEGIN
    RAISERROR('Grupo %s nao aceita adesoes.', 16, 1, @NR_GRUPO)
    RETURN 1
END

IF NOT EXISTS (SELECT 1 FROM TB_GRUPO_BEM WHERE CD_GRUPO = @CD_GRUPO AND CD_BEM = @CD_BEM)
BEGIN
    RAISERROR('Bem nao permitido no grupo %s.', 16, 1, @NR_GRUPO)
    RETURN 1
END

IF @QT_PRAZO - @QT_REALIZADAS < 1
BEGIN
    RAISERROR('Grupo %s sem prazo restante.', 16, 1, @NR_GRUPO)
    RETURN 1
END

BEGIN TRANSACTION

-- menor numero de cota livre (cota cancelada libera o numero)
SELECT @NR_COTA = MIN(V.number)
  FROM master..spt_values V
 WHERE V.type = 'P'
   AND V.number BETWEEN 1 AND @QT_MAX
   AND NOT EXISTS (SELECT 1
                     FROM TB_COTA C WITH (UPDLOCK, HOLDLOCK)
                    WHERE C.CD_GRUPO = @CD_GRUPO
                      AND C.NR_COTA = V.number
                      AND C.ID_SITUACAO <> 6)

IF @NR_COTA IS NULL
BEGIN
    SET @MSG = 'Grupo ' + @NR_GRUPO + ' sem vaga.'
    GOTO ERRO
END

SELECT @NR_VERSAO = ISNULL(MAX(NR_VERSAO) + 1, 0)
  FROM TB_COTA
 WHERE CD_GRUPO = @CD_GRUPO
   AND NR_COTA = @NR_COTA

INSERT INTO TB_COTA (CD_GRUPO, NR_COTA, NR_VERSAO, CD_CONSORCIADO, CD_BEM, CD_PROPOSTA,
                     VL_CREDITO_ADESAO, DT_ADESAO, QT_PRAZO_COTA, ID_SITUACAO, DT_SITUACAO)
VALUES (@CD_GRUPO, @NR_COTA, @NR_VERSAO, @CD_CONSORCIADO, @CD_BEM, @CD_PROPOSTA,
        @VL_BEM, @DT_PAGAMENTO, @QT_PRAZO - @QT_REALIZADAS, 1, GETDATE())

IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao incluir cota.' GOTO ERRO END

SET @CD_COTA = @@IDENTITY

EXEC @RET = SP_GERA_PLANO_PARCELAS @CD_COTA
IF @@ERROR <> 0 OR @RET <> 0 BEGIN SET @MSG = 'Erro ao gerar plano de parcelas.' GOTO ERRO END

-- baixa da parcela 1
SELECT @CD_PARCELA = CD_PARCELA,
       @PC_FC      = PC_FUNDO_COMUM,
       @PC_FR      = PC_FUNDO_RESERVA
  FROM TB_PARCELA
 WHERE CD_COTA = @CD_COTA
   AND NR_PARCELA = 1

SET @VL_FC = ROUND(@VL_BEM * @PC_FC / 100, 2)
SET @VL_FR = ROUND(@VL_BEM * @PC_FR / 100, 2)
SET @VL_TA = @VL_PARCELA - @VL_FC - @VL_FR

UPDATE TB_PARCELA
   SET VL_BEM_BASE      = @VL_BEM,
       VL_FUNDO_COMUM   = @VL_FC,
       VL_TAXA_ADM      = @VL_TA,
       VL_FUNDO_RESERVA = @VL_FR,
       VL_PARCELA       = @VL_PARCELA,
       DT_EMISSAO       = @DT_PAGAMENTO,
       DT_PAGAMENTO     = @DT_PAGAMENTO,
       VL_PAGO          = @VL_PAGO,
       VL_MULTA         = 0,
       VL_JUROS         = 0,
       ID_SITUACAO      = 3,
       ID_ORIGEM_BAIXA  = 3,
       CD_USUARIO_BAIXA = @CD_USUARIO
 WHERE CD_PARCELA = @CD_PARCELA

IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao baixar parcela 1.' GOTO ERRO END

UPDATE TB_COTA
   SET PC_AMORTIZADO = @PC_FC
 WHERE CD_COTA = @CD_COTA

IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao atualizar cota.' GOTO ERRO END

SET @HIST = 'Parcela 1 - cota ' + RIGHT('000' + CAST(@NR_COTA AS VARCHAR), 3) + '-' + CAST(@NR_VERSAO AS VARCHAR)
          + ' - proposta ' + @NR_PROPOSTA

INSERT INTO TB_GRUPO_MOVIMENTO (CD_GRUPO, SG_FUNDO, ID_TIPO_MOVIMENTO, DT_MOVIMENTO, VL_MOVIMENTO, CD_COTA, CD_PARCELA, CD_PROPOSTA, DS_HISTORICO, CD_USUARIO)
VALUES (@CD_GRUPO, 'FC', 1, @DT_PAGAMENTO, @VL_FC, @CD_COTA, @CD_PARCELA, @CD_PROPOSTA, @HIST, @CD_USUARIO)
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro no lancamento FC.' GOTO ERRO END

INSERT INTO TB_GRUPO_MOVIMENTO (CD_GRUPO, SG_FUNDO, ID_TIPO_MOVIMENTO, DT_MOVIMENTO, VL_MOVIMENTO, CD_COTA, CD_PARCELA, CD_PROPOSTA, DS_HISTORICO, CD_USUARIO)
VALUES (@CD_GRUPO, 'FR', 1, @DT_PAGAMENTO, @VL_FR, @CD_COTA, @CD_PARCELA, @CD_PROPOSTA, @HIST, @CD_USUARIO)
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro no lancamento FR.' GOTO ERRO END

INSERT INTO TB_GRUPO_MOVIMENTO (CD_GRUPO, SG_FUNDO, ID_TIPO_MOVIMENTO, DT_MOVIMENTO, VL_MOVIMENTO, CD_COTA, CD_PARCELA, CD_PROPOSTA, DS_HISTORICO, CD_USUARIO)
VALUES (@CD_GRUPO, 'TA', 1, @DT_PAGAMENTO, @VL_TA, @CD_COTA, @CD_PARCELA, @CD_PROPOSTA, @HIST, @CD_USUARIO)
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro no lancamento TA.' GOTO ERRO END

UPDATE TB_PROPOSTA
   SET ID_SITUACAO   = 3,
       DT_SITUACAO   = GETDATE(),
       DT_EFETIVACAO = @DT_PAGAMENTO,
       CD_COTA       = @CD_COTA
 WHERE CD_PROPOSTA = @CD_PROPOSTA

IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao atualizar proposta.' GOTO ERRO END

SET @HIST = 'Proposta ' + @NR_PROPOSTA + ' -> grupo ' + @NR_GRUPO + ' cota ' + CAST(@NR_COTA AS VARCHAR) + '-' + CAST(@NR_VERSAO AS VARCHAR)
EXEC SP_GRAVA_LOG @CD_USUARIO, 'EFETIVA_PROPOSTA', @HIST

COMMIT TRANSACTION
RETURN 0

ERRO:
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
SET @CD_COTA = NULL
RAISERROR(@MSG, 16, 1)
RETURN 1
GO
