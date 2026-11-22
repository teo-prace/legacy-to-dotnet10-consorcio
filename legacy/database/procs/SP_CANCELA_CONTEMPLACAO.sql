IF OBJECT_ID('SP_CANCELA_CONTEMPLACAO') IS NOT NULL DROP PROCEDURE SP_CANCELA_CONTEMPLACAO
GO
/*****************************************************************************
  Cancela contemplacao por SORTEIO ainda nao liberada (desistencia do credito
  pelo contemplado). A cota volta a concorrer.
  Contemplacao por lance: estorno do lance e da amortizacao e feito pelo
  suporte (script), nao por aqui.
  2014-08 J.P.
*****************************************************************************/
CREATE PROCEDURE SP_CANCELA_CONTEMPLACAO
    @CD_CONTEMPLACAO INT,
    @CD_USUARIO      INT
AS
SET NOCOUNT ON

DECLARE @ID_SIT TINYINT, @ID_TIPO TINYINT, @CD_COTA INT, @MSG VARCHAR(200)

SELECT @ID_SIT = ID_SITUACAO, @ID_TIPO = ID_TIPO, @CD_COTA = CD_COTA
  FROM TB_CONTEMPLACAO WHERE CD_CONTEMPLACAO = @CD_CONTEMPLACAO

IF @ID_SIT IS NULL OR @ID_SIT <> 1
BEGIN
    RAISERROR('Contemplacao nao esta aguardando liberacao.', 16, 1)
    RETURN 1
END

IF @ID_TIPO = 2
BEGIN
    RAISERROR('Cancelamento de contemplacao por lance: abrir chamado para o suporte.', 16, 1)
    RETURN 1
END

BEGIN TRANSACTION
UPDATE TB_CONTEMPLACAO SET ID_SITUACAO = 3 WHERE CD_CONTEMPLACAO = @CD_CONTEMPLACAO
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao cancelar.' GOTO ERRO END
IF @ID_TIPO = 1
BEGIN
    UPDATE TB_COTA SET ID_SITUACAO = 1, DT_SITUACAO = GETDATE() WHERE CD_COTA = @CD_COTA
    IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao reativar cota.' GOTO ERRO END
END
SET @MSG = CAST(@CD_CONTEMPLACAO AS VARCHAR)
EXEC SP_GRAVA_LOG @CD_USUARIO, 'CANCELA_CONTEMPLACAO', @MSG
COMMIT TRANSACTION
RETURN 0

ERRO:
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
RAISERROR(@MSG, 16, 1)
RETURN 1
GO
