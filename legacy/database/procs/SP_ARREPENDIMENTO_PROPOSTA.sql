IF OBJECT_ID('SP_ARREPENDIMENTO_PROPOSTA') IS NOT NULL DROP PROCEDURE SP_ARREPENDIMENTO_PROPOSTA
GO
/*****************************************************************************
  Arrependimento (CDC art. 49): ate 7 dias da assinatura.
  Estorna os lancamentos da cota no caixa do grupo, cancela cota e parcelas.
  O numero da cota fica livre para nova venda (proxima versao).

  2011-07 M.S.
*****************************************************************************/
CREATE PROCEDURE SP_ARREPENDIMENTO_PROPOSTA
    @CD_PROPOSTA INT,
    @CD_USUARIO  INT
AS
SET NOCOUNT ON

DECLARE @ID_SIT       TINYINT,
        @NR_PROPOSTA  VARCHAR(12),
        @DT_ASSINATURA DATETIME,
        @CD_COTA      INT,
        @ID_SIT_COTA  TINYINT,
        @MSG          VARCHAR(200)

SELECT @ID_SIT        = P.ID_SITUACAO,
       @NR_PROPOSTA   = P.NR_PROPOSTA,
       @DT_ASSINATURA = ISNULL(P.DT_ASSINATURA, P.DT_PROPOSTA),
       @CD_COTA       = P.CD_COTA,
       @ID_SIT_COTA   = C.ID_SITUACAO
  FROM TB_PROPOSTA P
  LEFT JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA
 WHERE P.CD_PROPOSTA = @CD_PROPOSTA

IF @ID_SIT IS NULL
BEGIN
    RAISERROR('Proposta nao encontrada.', 16, 1)
    RETURN 1
END

IF @ID_SIT <> 3
BEGIN
    RAISERROR('Somente proposta efetivada pode ter arrependimento.', 16, 1)
    RETURN 1
END

IF DATEDIFF(DAY, @DT_ASSINATURA, GETDATE()) > 7
BEGIN
    RAISERROR('Prazo de arrependimento (7 dias) expirado.', 16, 1)
    RETURN 1
END

IF @ID_SIT_COTA <> 1
BEGIN
    RAISERROR('Cota nao esta ativa.', 16, 1)
    RETURN 1
END

BEGIN TRANSACTION

INSERT INTO TB_GRUPO_MOVIMENTO (CD_GRUPO, SG_FUNDO, ID_TIPO_MOVIMENTO, DT_MOVIMENTO, VL_MOVIMENTO, CD_COTA, CD_PARCELA, CD_PROPOSTA, DS_HISTORICO, CD_USUARIO)
SELECT CD_GRUPO, SG_FUNDO, 2, GETDATE(), -VL_MOVIMENTO, CD_COTA, CD_PARCELA, CD_PROPOSTA,
       'Estorno - arrependimento proposta ' + @NR_PROPOSTA, @CD_USUARIO
  FROM TB_GRUPO_MOVIMENTO
 WHERE CD_COTA = @CD_COTA
   AND ID_TIPO_MOVIMENTO = 1

IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao estornar lancamentos.' GOTO ERRO END

UPDATE TB_PARCELA
   SET ID_SITUACAO = 4
 WHERE CD_COTA = @CD_COTA

IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao cancelar parcelas.' GOTO ERRO END

UPDATE TB_COTA
   SET ID_SITUACAO   = 6,
       DT_SITUACAO   = GETDATE(),
       PC_AMORTIZADO = 0
 WHERE CD_COTA = @CD_COTA

IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao cancelar cota.' GOTO ERRO END

UPDATE TB_PROPOSTA
   SET ID_SITUACAO = 5,
       DT_SITUACAO = GETDATE()
 WHERE CD_PROPOSTA = @CD_PROPOSTA

IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao atualizar proposta.' GOTO ERRO END

EXEC SP_GRAVA_LOG @CD_USUARIO, 'ARREPENDIMENTO', @NR_PROPOSTA

COMMIT TRANSACTION
RETURN 0

ERRO:
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
RAISERROR(@MSG, 16, 1)
RETURN 1
GO
