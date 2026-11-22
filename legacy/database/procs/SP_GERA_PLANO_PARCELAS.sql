IF OBJECT_ID('SP_GERA_PLANO_PARCELAS') IS NOT NULL DROP PROCEDURE SP_GERA_PLANO_PARCELAS
GO
/*****************************************************************************
  Gera o plano de parcelas da cota, em PERCENTUAL do bem.

  - Fundo comum:     100 / prazo da cota
  - Taxa adm.:       (taxa - antecipacao) / prazo; antecipacao somada na 1a parcela
  - Fundo reserva:   fundo reserva / prazo
  - Percentuais com 6 casas; a ULTIMA parcela absorve o residuo para cada
    componente fechar exatamente no total.
  - Parcela 1 vence na adesao. Demais:
      grupo em andamento -> dia de vencimento do grupo, a partir do mes seguinte a adesao
      grupo em formacao  -> sem vencimento ate a constituicao (SP_CONSTITUI_GRUPO)

  Nao abre transacao: e chamada dentro de SP_EFETIVA_PROPOSTA.

  2005-03 R.A.  versao inicial
  2006-10 R.A.  ajuste do residuo na ultima parcela (auditoria)
  2011-05 M.S.  antecipacao de taxa de administracao
*****************************************************************************/
CREATE PROCEDURE SP_GERA_PLANO_PARCELAS
    @CD_COTA INT
AS
SET NOCOUNT ON

DECLARE @CD_GRUPO       INT,
        @N              SMALLINT,
        @TA             DECIMAL(9,4),
        @ANT            DECIMAL(9,4),
        @FR             DECIMAL(9,4),
        @DT_ADESAO      DATETIME,
        @ID_SIT_GRUPO   TINYINT,
        @DIA            TINYINT,
        @DT_BASE        DATETIME,
        @PC_FC          DECIMAL(9,6),
        @PC_TA          DECIMAL(9,6),
        @PC_FR          DECIMAL(9,6),
        @SOMA_FC        DECIMAL(12,6),
        @SOMA_TA        DECIMAL(12,6),
        @SOMA_FR        DECIMAL(12,6),
        @I              SMALLINT,
        @FC_I           DECIMAL(9,6),
        @TA_I           DECIMAL(9,6),
        @FR_I           DECIMAL(9,6),
        @DT_VENC        DATETIME

SELECT @CD_GRUPO     = C.CD_GRUPO,
       @N            = C.QT_PRAZO_COTA,
       @DT_ADESAO    = C.DT_ADESAO,
       @TA           = G.PC_TAXA_ADM,
       @ANT          = G.PC_ANTECIPACAO_TA,
       @FR           = G.PC_FUNDO_RESERVA,
       @ID_SIT_GRUPO = G.ID_SITUACAO,
       @DIA          = G.NR_DIA_VENCIMENTO
  FROM TB_COTA C
 INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO
 WHERE C.CD_COTA = @CD_COTA

IF @CD_GRUPO IS NULL
BEGIN
    RAISERROR('Cota %d nao encontrada.', 16, 1, @CD_COTA)
    RETURN 1
END

IF @N IS NULL OR @N < 1
BEGIN
    RAISERROR('Prazo da cota invalido.', 16, 1)
    RETURN 1
END

IF EXISTS (SELECT 1 FROM TB_PARCELA WHERE CD_COTA = @CD_COTA)
BEGIN
    RAISERROR('Cota %d ja possui plano de parcelas.', 16, 1, @CD_COTA)
    RETURN 1
END

SET @PC_FC = ROUND(100.0 / @N, 6)
SET @PC_TA = ROUND((@TA - @ANT) / @N, 6)
SET @PC_FR = ROUND(@FR / @N, 6)

-- dia de vencimento do grupo no mes da adesao
SET @DT_BASE = DATEADD(DAY, @DIA - 1, DATEADD(MONTH, DATEDIFF(MONTH, 0, @DT_ADESAO), 0))

SET @SOMA_FC = 0
SET @SOMA_TA = 0
SET @SOMA_FR = 0
SET @I = 1

WHILE @I <= @N
BEGIN
    IF @I = @N
    BEGIN
        SET @FC_I = 100 - @SOMA_FC
        SET @TA_I = @TA - @SOMA_TA
        SET @FR_I = @FR - @SOMA_FR
    END
    ELSE
    BEGIN
        SET @FC_I = @PC_FC
        SET @TA_I = @PC_TA
        IF @I = 1 SET @TA_I = @TA_I + @ANT
        SET @FR_I = @PC_FR
    END

    IF @I = 1
        SET @DT_VENC = @DT_ADESAO
    ELSE IF @ID_SIT_GRUPO = 2
        SET @DT_VENC = DATEADD(MONTH, @I - 1, @DT_BASE)
    ELSE
        SET @DT_VENC = NULL

    INSERT INTO TB_PARCELA (CD_COTA, NR_PARCELA, DT_VENCIMENTO, PC_FUNDO_COMUM, PC_TAXA_ADM, PC_FUNDO_RESERVA, ID_SITUACAO)
    VALUES (@CD_COTA, @I, @DT_VENC, @FC_I, @TA_I, @FR_I, 1)

    IF @@ERROR <> 0 GOTO ERRO

    SET @SOMA_FC = @SOMA_FC + @FC_I
    SET @SOMA_TA = @SOMA_TA + @TA_I
    SET @SOMA_FR = @SOMA_FR + @FR_I
    SET @I = @I + 1
END

RETURN 0

ERRO:
RAISERROR('Erro ao gerar plano de parcelas da cota %d.', 16, 1, @CD_COTA)
RETURN 1
GO
