IF OBJECT_ID('SP_BAIXA_PARCELA') IS NOT NULL DROP PROCEDURE SP_BAIXA_PARCELA
GO
/*****************************************************************************
  Baixa de parcela emitida.

  Multa e juros chegam calculados por quem chama (tela de baixa ou batch
  de retorno).
  - valor pago < devido   -> rejeita (nao aceita pagamento parcial)
  - valor pago > devido   -> excedente vai para RD (devolucao ao consorciado)
  - caixa: FC/FR/TA pelos valores EMITIDOS; multa+juros metade FC, metade TA
    (FC fica com o centavo impar); excedente em RD
  - cota contemplada com todas as parcelas pagas -> quitada

  @ID_ORIGEM: 1 manual, 2 retorno bancario

  2005-07 R.A.  versao inicial
  2014-05 J.P.  origem retorno bancario, multa/juros
  2016-09 J.P.  lancamentos por fundo
*****************************************************************************/
CREATE PROCEDURE SP_BAIXA_PARCELA
    @CD_PARCELA          INT,
    @DT_PAGAMENTO        DATETIME,
    @VL_PAGO             MONEY,
    @VL_MULTA            MONEY,
    @VL_JUROS            MONEY,
    @ID_ORIGEM           TINYINT,
    @CD_USUARIO          INT,
    @CD_RETORNO_DETALHE  INT = NULL
AS
SET NOCOUNT ON

DECLARE @ID_SIT        TINYINT,
        @CD_COTA       INT,
        @CD_GRUPO      INT,
        @ID_SIT_COTA   TINYINT,
        @NR_PARCELA    SMALLINT,
        @NR_COTA       SMALLINT,
        @NR_VERSAO     TINYINT,
        @PC_FC         DECIMAL(9,6),
        @VL_PARCELA    MONEY,
        @VL_FC         MONEY,
        @VL_TA         MONEY,
        @VL_FR         MONEY,
        @VL_DEVIDO     MONEY,
        @VL_ENCARGOS   MONEY,
        @VL_ENC_TA     MONEY,
        @VL_ENC_FC     MONEY,
        @VL_EXCEDENTE  MONEY,
        @HIST          VARCHAR(200),
        @MSG           VARCHAR(200),
        @ERRO          INT,
        @LINHAS        INT

SELECT @ID_SIT      = P.ID_SITUACAO,
       @CD_COTA     = P.CD_COTA,
       @NR_PARCELA  = P.NR_PARCELA,
       @PC_FC       = P.PC_FUNDO_COMUM,
       @VL_PARCELA  = P.VL_PARCELA,
       @VL_FC       = P.VL_FUNDO_COMUM,
       @VL_TA       = P.VL_TAXA_ADM,
       @VL_FR       = P.VL_FUNDO_RESERVA,
       @CD_GRUPO    = C.CD_GRUPO,
       @ID_SIT_COTA = C.ID_SITUACAO,
       @NR_COTA     = C.NR_COTA,
       @NR_VERSAO   = C.NR_VERSAO
  FROM TB_PARCELA P
 INNER JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA
 WHERE P.CD_PARCELA = @CD_PARCELA

IF @ID_SIT IS NULL
BEGIN
    RAISERROR('Parcela nao encontrada.', 16, 1)
    RETURN 1
END

IF @ID_SIT = 3
BEGIN
    RAISERROR('Parcela ja paga.', 16, 1)
    RETURN 1
END

IF @ID_SIT <> 2
BEGIN
    RAISERROR('Parcela nao emitida ou cancelada.', 16, 1)
    RETURN 1
END

SET @VL_MULTA = ISNULL(@VL_MULTA, 0)
SET @VL_JUROS = ISNULL(@VL_JUROS, 0)
SET @VL_ENCARGOS = @VL_MULTA + @VL_JUROS
SET @VL_DEVIDO = @VL_PARCELA + @VL_ENCARGOS

IF @VL_PAGO < @VL_DEVIDO
BEGIN
    SET @MSG = 'Valor pago (' + CONVERT(VARCHAR, @VL_PAGO, 1) + ') menor que o devido (' + CONVERT(VARCHAR, @VL_DEVIDO, 1) + ').'
    RAISERROR(@MSG, 16, 1)
    RETURN 1
END

SET @VL_EXCEDENTE = @VL_PAGO - @VL_DEVIDO
SET @VL_ENC_TA = ROUND(@VL_ENCARGOS / 2, 2, 1)
SET @VL_ENC_FC = @VL_ENCARGOS - @VL_ENC_TA

SET @HIST = 'Parcela ' + CAST(@NR_PARCELA AS VARCHAR) + ' - cota ' + RIGHT('000' + CAST(@NR_COTA AS VARCHAR), 3) + '-' + CAST(@NR_VERSAO AS VARCHAR)

BEGIN TRANSACTION

UPDATE TB_PARCELA
   SET DT_PAGAMENTO       = @DT_PAGAMENTO,
       VL_PAGO            = @VL_PAGO,
       VL_MULTA           = @VL_MULTA,
       VL_JUROS           = @VL_JUROS,
       ID_SITUACAO        = 3,
       ID_ORIGEM_BAIXA    = @ID_ORIGEM,
       CD_USUARIO_BAIXA   = @CD_USUARIO,
       CD_RETORNO_DETALHE = @CD_RETORNO_DETALHE
 WHERE CD_PARCELA = @CD_PARCELA
   AND ID_SITUACAO = 2

SELECT @ERRO = @@ERROR, @LINHAS = @@ROWCOUNT
IF @ERRO <> 0 OR @LINHAS <> 1 BEGIN SET @MSG = 'Erro ao baixar parcela (alterada por outro processo?).' GOTO ERRO END

UPDATE TB_COTA
   SET PC_AMORTIZADO = PC_AMORTIZADO + @PC_FC
 WHERE CD_COTA = @CD_COTA
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao atualizar cota.' GOTO ERRO END

INSERT INTO TB_GRUPO_MOVIMENTO (CD_GRUPO, SG_FUNDO, ID_TIPO_MOVIMENTO, DT_MOVIMENTO, VL_MOVIMENTO, CD_COTA, CD_PARCELA, DS_HISTORICO, CD_USUARIO)
VALUES (@CD_GRUPO, 'FC', 1, @DT_PAGAMENTO, @VL_FC, @CD_COTA, @CD_PARCELA, @HIST, @CD_USUARIO)
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro no lancamento FC.' GOTO ERRO END

INSERT INTO TB_GRUPO_MOVIMENTO (CD_GRUPO, SG_FUNDO, ID_TIPO_MOVIMENTO, DT_MOVIMENTO, VL_MOVIMENTO, CD_COTA, CD_PARCELA, DS_HISTORICO, CD_USUARIO)
VALUES (@CD_GRUPO, 'FR', 1, @DT_PAGAMENTO, @VL_FR, @CD_COTA, @CD_PARCELA, @HIST, @CD_USUARIO)
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro no lancamento FR.' GOTO ERRO END

INSERT INTO TB_GRUPO_MOVIMENTO (CD_GRUPO, SG_FUNDO, ID_TIPO_MOVIMENTO, DT_MOVIMENTO, VL_MOVIMENTO, CD_COTA, CD_PARCELA, DS_HISTORICO, CD_USUARIO)
VALUES (@CD_GRUPO, 'TA', 1, @DT_PAGAMENTO, @VL_TA, @CD_COTA, @CD_PARCELA, @HIST, @CD_USUARIO)
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro no lancamento TA.' GOTO ERRO END

IF @VL_ENCARGOS > 0
BEGIN
    INSERT INTO TB_GRUPO_MOVIMENTO (CD_GRUPO, SG_FUNDO, ID_TIPO_MOVIMENTO, DT_MOVIMENTO, VL_MOVIMENTO, CD_COTA, CD_PARCELA, DS_HISTORICO, CD_USUARIO)
    VALUES (@CD_GRUPO, 'FC', 7, @DT_PAGAMENTO, @VL_ENC_FC, @CD_COTA, @CD_PARCELA, 'Multa/juros - ' + @HIST, @CD_USUARIO)
    IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro no lancamento de encargos.' GOTO ERRO END

    IF @VL_ENC_TA > 0
    BEGIN
        INSERT INTO TB_GRUPO_MOVIMENTO (CD_GRUPO, SG_FUNDO, ID_TIPO_MOVIMENTO, DT_MOVIMENTO, VL_MOVIMENTO, CD_COTA, CD_PARCELA, DS_HISTORICO, CD_USUARIO)
        VALUES (@CD_GRUPO, 'TA', 7, @DT_PAGAMENTO, @VL_ENC_TA, @CD_COTA, @CD_PARCELA, 'Multa/juros - ' + @HIST, @CD_USUARIO)
        IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro no lancamento de encargos.' GOTO ERRO END
    END
END

IF @VL_EXCEDENTE > 0
BEGIN
    INSERT INTO TB_GRUPO_MOVIMENTO (CD_GRUPO, SG_FUNDO, ID_TIPO_MOVIMENTO, DT_MOVIMENTO, VL_MOVIMENTO, CD_COTA, CD_PARCELA, DS_HISTORICO, CD_USUARIO)
    VALUES (@CD_GRUPO, 'RD', 8, @DT_PAGAMENTO, @VL_EXCEDENTE, @CD_COTA, @CD_PARCELA, 'Pagamento a maior - ' + @HIST, @CD_USUARIO)
    IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro no lancamento do excedente.' GOTO ERRO END
END

-- contemplada sem parcelas em aberto -> quitada
IF @ID_SIT_COTA = 2 AND NOT EXISTS (SELECT 1 FROM TB_PARCELA WHERE CD_COTA = @CD_COTA AND ID_SITUACAO IN (1, 2))
BEGIN
    UPDATE TB_COTA SET ID_SITUACAO = 5, DT_SITUACAO = GETDATE() WHERE CD_COTA = @CD_COTA
    IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao quitar cota.' GOTO ERRO END
END

COMMIT TRANSACTION
RETURN 0

ERRO:
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
RAISERROR(@MSG, 16, 1)
RETURN 1
GO
