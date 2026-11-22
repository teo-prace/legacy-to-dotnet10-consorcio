IF OBJECT_ID('SP_CONSTITUI_GRUPO') IS NOT NULL DROP PROCEDURE SP_CONSTITUI_GRUPO
GO
/*****************************************************************************
  Constitui o grupo (1a assembleia) quando atingido o quorum minimo.
  Define os vencimentos das parcelas 2..n das cotas ativas:
  dia de vencimento do grupo, a partir do mes seguinte a 1a assembleia.

  2005-05 R.A.
*****************************************************************************/
CREATE PROCEDURE SP_CONSTITUI_GRUPO
    @CD_GRUPO        INT,
    @DT_PRIMEIRA_AGO DATETIME,
    @CD_USUARIO      INT
AS
SET NOCOUNT ON

DECLARE @ID_SIT      TINYINT,
        @NR_GRUPO    CHAR(6),
        @QT_MINIMO   SMALLINT,
        @QT_ATIVAS   INT,
        @DIA         TINYINT,
        @DT_ABERTURA DATETIME,
        @DT_BASE     DATETIME,
        @MSG         VARCHAR(200)

SELECT @ID_SIT      = ID_SITUACAO,
       @NR_GRUPO    = NR_GRUPO,
       @QT_MINIMO   = QT_COTAS_MINIMO,
       @DIA         = NR_DIA_VENCIMENTO,
       @DT_ABERTURA = DT_ABERTURA
  FROM TB_GRUPO
 WHERE CD_GRUPO = @CD_GRUPO

IF @ID_SIT IS NULL
BEGIN
    RAISERROR('Grupo nao encontrado.', 16, 1)
    RETURN 1
END

IF @ID_SIT <> 1
BEGIN
    RAISERROR('Grupo %s nao esta em formacao.', 16, 1, @NR_GRUPO)
    RETURN 1
END

SELECT @QT_ATIVAS = COUNT(*)
  FROM TB_COTA
 WHERE CD_GRUPO = @CD_GRUPO
   AND ID_SITUACAO = 1

IF @QT_ATIVAS < @QT_MINIMO
BEGIN
    SET @MSG = 'Grupo ' + @NR_GRUPO + ' com ' + CAST(@QT_ATIVAS AS VARCHAR) + ' cotas; minimo para constituir: ' + CAST(@QT_MINIMO AS VARCHAR) + '.'
    RAISERROR(@MSG, 16, 1)
    RETURN 1
END

IF @DT_PRIMEIRA_AGO < @DT_ABERTURA
BEGIN
    RAISERROR('Data da 1a assembleia anterior a abertura do grupo.', 16, 1)
    RETURN 1
END

SET @DT_BASE = DATEADD(DAY, @DIA - 1, DATEADD(MONTH, DATEDIFF(MONTH, 0, @DT_PRIMEIRA_AGO), 0))

BEGIN TRANSACTION

UPDATE TB_GRUPO
   SET ID_SITUACAO     = 2,
       DT_SITUACAO     = GETDATE(),
       DT_CONSTITUICAO = @DT_PRIMEIRA_AGO
 WHERE CD_GRUPO = @CD_GRUPO

IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao atualizar grupo.' GOTO ERRO END

UPDATE P
   SET DT_VENCIMENTO = DATEADD(MONTH, P.NR_PARCELA - 1, @DT_BASE)
  FROM TB_PARCELA P
 INNER JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA
 WHERE C.CD_GRUPO = @CD_GRUPO
   AND C.ID_SITUACAO = 1
   AND P.NR_PARCELA > 1

IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao definir vencimentos.' GOTO ERRO END

SET @MSG = 'Grupo ' + @NR_GRUPO + ' - 1a AGO ' + CONVERT(VARCHAR(10), @DT_PRIMEIRA_AGO, 103) + ' - ' + CAST(@QT_ATIVAS AS VARCHAR) + ' cotas'
EXEC SP_GRAVA_LOG @CD_USUARIO, 'CONSTITUI_GRUPO', @MSG

COMMIT TRANSACTION
RETURN 0

ERRO:
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
RAISERROR(@MSG, 16, 1)
RETURN 1
GO
