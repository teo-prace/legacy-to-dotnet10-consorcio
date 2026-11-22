IF OBJECT_ID('SP_DESISTE_COTA') IS NOT NULL DROP PROCEDURE SP_DESISTE_COTA
GO
/*****************************************************************************
  Desistencia do consorciado (cota ativa nao contemplada).
  Cancela as parcelas em aberto; o pago fica no fundo comum e e devolvido
  quando a cota for sorteada (SP_APURA_ASSEMBLEIA) ou no encerramento.
  2006-02 R.A.
  2011-09 M.S. chamada pela retaguarda web
*****************************************************************************/
CREATE PROCEDURE SP_DESISTE_COTA
    @CD_COTA       INT,
    @CD_USUARIO    INT,
    @DT_DESISTENCIA DATETIME = NULL,
    @DS_MOTIVO     VARCHAR(200) = NULL
AS
SET NOCOUNT ON

DECLARE @ID_SIT TINYINT, @DS VARCHAR(300), @MSG VARCHAR(200)

IF @DT_DESISTENCIA IS NULL SET @DT_DESISTENCIA = GETDATE()

SELECT @ID_SIT = C.ID_SITUACAO,
       @DS = G.NR_GRUPO + '/' + RIGHT('000' + CAST(C.NR_COTA AS VARCHAR), 3) + '-' + CAST(C.NR_VERSAO AS VARCHAR)
  FROM TB_COTA C INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO
 WHERE C.CD_COTA = @CD_COTA

IF @ID_SIT IS NULL OR @ID_SIT <> 1
BEGIN
    RAISERROR('Somente cota ativa nao contemplada pode desistir.', 16, 1)
    RETURN 1
END

BEGIN TRANSACTION

UPDATE TB_PARCELA SET ID_SITUACAO = 4 WHERE CD_COTA = @CD_COTA AND ID_SITUACAO IN (1, 2)
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao cancelar parcelas.' GOTO ERRO END

UPDATE TB_COTA SET ID_SITUACAO = 4, DT_SITUACAO = @DT_DESISTENCIA WHERE CD_COTA = @CD_COTA
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao atualizar cota.' GOTO ERRO END

UPDATE TB_LANCE SET ID_SITUACAO = 5, DS_MOTIVO = 'Desistencia da cota'
 WHERE CD_COTA = @CD_COTA AND ID_SITUACAO = 1

SET @DS = @DS + ISNULL(' - ' + @DS_MOTIVO, '')
EXEC SP_GRAVA_LOG @CD_USUARIO, 'DESISTENCIA', @DS

COMMIT TRANSACTION
RETURN 0

ERRO:
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
RAISERROR(@MSG, 16, 1)
RETURN 1
GO
