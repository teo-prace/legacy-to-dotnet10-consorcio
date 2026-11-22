IF OBJECT_ID('SP_APURA_ASSEMBLEIA') IS NOT NULL DROP PROCEDURE SP_APURA_ASSEMBLEIA
GO
/*****************************************************************************
  Apuracao da assembleia: sorteio pela Loteria Federal e lances.

  1. recursos = saldo FC na data da AGO - creditos contemplados nao liberados
  2. cotas aptas: FN_COTA_APTA
  3. sorteio: 1o premio, depois 2o..5o; senao primeira apta acima do numero
     do 1o premio (circular). Sem recursos para o credito -> sem contemplacao
     na AGO (nem por lance).
  4. lances: desclassifica invalidos; ordem por % decrescente, empate pela
     cota mais proxima do sorteado (para cima, circular); contempla enquanto
     recursos >= credito - lance.
  5. lance amortiza o fundo comum das ultimas parcelas (SP_AMORTIZA_LANCE);
     recursos proprios do lance entram no FC na data da AGO.
  3b. depois do sorteio: premio que cair em cota excluida/desistente com
     valor a receber -> contemplacao para devolucao (bem atual x % pago,
     menos a multa rescisoria).

  2005-09 R.A.  apuracao no SGCApuracao (VB6)
  2014-08 J.P.  apuracao passa para procedure; VB6 fica so para conferencia
  2015-03 J.P.  desempate pela proximidade do sorteado (antes: data do lance)
  2016-11 J.P.  devolucao a excluidos/desistentes sorteados
*****************************************************************************/
CREATE PROCEDURE SP_APURA_ASSEMBLEIA
    @CD_ASSEMBLEIA INT,
    @CD_USUARIO    INT,
    @DT_APURACAO   DATETIME = NULL
AS
SET NOCOUNT ON

DECLARE @ID_SIT          TINYINT,
        @CD_GRUPO        INT,
        @NR_GRUPO        CHAR(6),
        @NR_ASSEMBLEIA   SMALLINT,
        @DT_AGO          DATETIME,
        @DT_FIM_DIA      DATETIME,
        @QT_MAX          SMALLINT,
        @PC_EMB_MAX      DECIMAL(9,4),
        @PC_FIXO         DECIMAL(9,4),
        @P1 CHAR(5), @P2 CHAR(5), @P3 CHAR(5), @P4 CHAR(5), @P5 CHAR(5),
        @SALDO_FC        MONEY,
        @RESERVADO       MONEY,
        @RECURSOS        MONEY,
        @RECURSOS_INI    MONEY,
        @BASE            SMALLINT,
        @NR_REF          SMALLINT,
        @I               INT,
        @N               SMALLINT,
        @CD_COTA_SORT    INT,
        @CREDITO         MONEY,
        @SEM_RECURSOS    BIT,
        @CD_LANCE        INT,
        @CD_COTA         INT,
        @PC_LANCE        DECIMAL(9,4),
        @PC_EMB          DECIMAL(9,4),
        @VL_LANCE        MONEY,
        @VL_EMB          MONEY,
        @VL_PROPRIOS     MONEY,
        @ORDEM           SMALLINT,
        @QT_CONT         SMALLINT,
        @VL_CONT         MONEY,
        @RET             INT,
        @NR_COTA         SMALLINT,
        @NR_VERSAO       TINYINT,
        @CD_COTA_DEV     INT,
        @VL_BRUTO        MONEY,
        @VL_DESC         MONEY,
        @PC_MULTA_RESC   DECIMAL(9,4),
        @MSG             VARCHAR(300)

IF @DT_APURACAO IS NULL SET @DT_APURACAO = GETDATE()

SELECT @ID_SIT = A.ID_SITUACAO, @CD_GRUPO = A.CD_GRUPO, @NR_ASSEMBLEIA = A.NR_ASSEMBLEIA, @DT_AGO = A.DT_ASSEMBLEIA,
       @P1 = A.NR_PREMIO_1, @P2 = A.NR_PREMIO_2, @P3 = A.NR_PREMIO_3, @P4 = A.NR_PREMIO_4, @P5 = A.NR_PREMIO_5,
       @NR_GRUPO = G.NR_GRUPO, @QT_MAX = G.QT_COTAS_MAXIMO, @PC_EMB_MAX = G.PC_LANCE_EMBUTIDO_MAX, @PC_FIXO = G.PC_LANCE_FIXO
  FROM TB_ASSEMBLEIA A
 INNER JOIN TB_GRUPO G ON G.CD_GRUPO = A.CD_GRUPO
 WHERE A.CD_ASSEMBLEIA = @CD_ASSEMBLEIA

IF @ID_SIT IS NULL
BEGIN
    RAISERROR('Assembleia nao encontrada.', 16, 1)
    RETURN 1
END

IF @ID_SIT <> 1
BEGIN
    RAISERROR('Assembleia ja apurada ou cancelada.', 16, 1)
    RETURN 1
END

IF @P1 IS NULL OR @P2 IS NULL OR @P3 IS NULL OR @P4 IS NULL OR @P5 IS NULL
   OR @P1 LIKE '%[^0-9]%' OR @P2 LIKE '%[^0-9]%' OR @P3 LIKE '%[^0-9]%' OR @P4 LIKE '%[^0-9]%' OR @P5 LIKE '%[^0-9]%'
BEGIN
    RAISERROR('Informe os 5 premios da Loteria Federal (5 digitos).', 16, 1)
    RETURN 1
END

SET @DT_FIM_DIA = DATEADD(MS, -3, DATEADD(DAY, 1, @DT_AGO))

-- 1. recursos
SELECT @SALDO_FC = ISNULL(SUM(VL_MOVIMENTO), 0)
  FROM TB_GRUPO_MOVIMENTO
 WHERE CD_GRUPO = @CD_GRUPO AND SG_FUNDO = 'FC' AND DT_MOVIMENTO <= @DT_FIM_DIA

SELECT @RESERVADO = ISNULL(SUM(T.VL_CREDITO_LIQUIDO), 0)
  FROM TB_CONTEMPLACAO T
 INNER JOIN TB_COTA C ON C.CD_COTA = T.CD_COTA
 WHERE C.CD_GRUPO = @CD_GRUPO AND T.ID_SITUACAO = 1

SET @RECURSOS = @SALDO_FC - @RESERVADO
SET @RECURSOS_INI = @RECURSOS

-- 2. cotas aptas
CREATE TABLE #APTAS (CD_COTA INT PRIMARY KEY, NR_COTA SMALLINT, CD_BEM INT, VL_CREDITO MONEY, PC_FC_ABERTO DECIMAL(12,6))

INSERT INTO #APTAS (CD_COTA, NR_COTA, CD_BEM, VL_CREDITO, PC_FC_ABERTO)
SELECT C.CD_COTA, C.NR_COTA, C.CD_BEM, dbo.FN_VALOR_BEM(C.CD_BEM, @DT_AGO),
       (SELECT ISNULL(SUM(P.PC_FUNDO_COMUM), 0) FROM TB_PARCELA P WHERE P.CD_COTA = C.CD_COTA AND P.ID_SITUACAO = 1)
  FROM TB_COTA C
 WHERE C.CD_GRUPO = @CD_GRUPO
   AND C.ID_SITUACAO = 1
   AND dbo.FN_COTA_APTA(C.CD_COTA, @DT_AGO) = 1

-- 3. sorteio
SET @BASE = dbo.FN_NUMERO_SORTEADO(@P1, @QT_MAX)
SET @CD_COTA_SORT = NULL
SET @I = 1
WHILE @I <= 5 AND @CD_COTA_SORT IS NULL
BEGIN
    SET @N = dbo.FN_NUMERO_SORTEADO(CASE @I WHEN 1 THEN @P1 WHEN 2 THEN @P2 WHEN 3 THEN @P3 WHEN 4 THEN @P4 ELSE @P5 END, @QT_MAX)
    SELECT @CD_COTA_SORT = CD_COTA FROM #APTAS WHERE NR_COTA = @N
    SET @I = @I + 1
END

IF @CD_COTA_SORT IS NULL
    SELECT TOP 1 @CD_COTA_SORT = CD_COTA
      FROM #APTAS
     ORDER BY CASE WHEN NR_COTA > @BASE THEN 0 ELSE 1 END, NR_COTA

SET @NR_REF = @BASE
SET @SEM_RECURSOS = 0
SET @QT_CONT = 0
SET @VL_CONT = 0

BEGIN TRANSACTION

IF @CD_COTA_SORT IS NOT NULL
BEGIN
    SELECT @CREDITO = VL_CREDITO, @NR_REF = NR_COTA FROM #APTAS WHERE CD_COTA = @CD_COTA_SORT

    IF @RECURSOS >= @CREDITO
    BEGIN
        INSERT INTO TB_CONTEMPLACAO (CD_COTA, CD_ASSEMBLEIA, ID_TIPO, VL_CREDITO, VL_CREDITO_LIQUIDO, ID_SITUACAO, DT_CONTEMPLACAO)
        VALUES (@CD_COTA_SORT, @CD_ASSEMBLEIA, 1, @CREDITO, @CREDITO, 1, @DT_AGO)
        IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro na contemplacao por sorteio.' GOTO ERRO END

        UPDATE TB_COTA SET ID_SITUACAO = 2, DT_SITUACAO = @DT_AGO WHERE CD_COTA = @CD_COTA_SORT
        IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao atualizar cota sorteada.' GOTO ERRO END

        UPDATE TB_LANCE SET ID_SITUACAO = 5, DS_MOTIVO = 'Cota contemplada por sorteio'
         WHERE CD_ASSEMBLEIA = @CD_ASSEMBLEIA AND CD_COTA = @CD_COTA_SORT

        DELETE FROM #APTAS WHERE CD_COTA = @CD_COTA_SORT
        SET @RECURSOS = @RECURSOS - @CREDITO
        SET @QT_CONT = 1
        SET @VL_CONT = @CREDITO
    END
    ELSE
        SET @SEM_RECURSOS = 1
END
ELSE
    SET @SEM_RECURSOS = 1   -- nenhuma cota apta

-- 3b. devolucao: premio que cair em cota excluida/desistente ainda nao paga (Lei 11.795, art. 22)
IF @SEM_RECURSOS = 0
BEGIN
    SELECT @PC_MULTA_RESC = CAST(DS_VALOR AS DECIMAL(9,4)) FROM TB_PARAMETRO WHERE NM_PARAMETRO = 'PC_MULTA_RESCISORIA'
    SET @CD_COTA_DEV = NULL
    SET @I = 1
    WHILE @I <= 5 AND @CD_COTA_DEV IS NULL
    BEGIN
        SET @N = dbo.FN_NUMERO_SORTEADO(CASE @I WHEN 1 THEN @P1 WHEN 2 THEN @P2 WHEN 3 THEN @P3 WHEN 4 THEN @P4 ELSE @P5 END, @QT_MAX)
        SELECT TOP 1 @CD_COTA_DEV = C.CD_COTA,
               @VL_BRUTO = ROUND(dbo.FN_VALOR_BEM(C.CD_BEM, @DT_AGO) * C.PC_AMORTIZADO / 100, 2)
          FROM TB_COTA C
         WHERE C.CD_GRUPO = @CD_GRUPO AND C.NR_COTA = @N AND C.ID_SITUACAO IN (3, 4) AND C.PC_AMORTIZADO > 0
           AND NOT EXISTS (SELECT 1 FROM TB_CONTEMPLACAO T WHERE T.CD_COTA = C.CD_COTA AND T.ID_SITUACAO IN (1, 2))
         ORDER BY C.NR_VERSAO DESC
        SET @I = @I + 1
    END

    IF @CD_COTA_DEV IS NOT NULL
    BEGIN
        SET @VL_DESC = ROUND(@VL_BRUTO * ISNULL(@PC_MULTA_RESC, 0) / 100, 2)
        IF @RECURSOS >= @VL_BRUTO - @VL_DESC
        BEGIN
            INSERT INTO TB_CONTEMPLACAO (CD_COTA, CD_ASSEMBLEIA, ID_TIPO, VL_CREDITO, VL_DESCONTO, VL_CREDITO_LIQUIDO, ID_SITUACAO, DT_CONTEMPLACAO)
            VALUES (@CD_COTA_DEV, @CD_ASSEMBLEIA, 3, @VL_BRUTO, @VL_DESC, @VL_BRUTO - @VL_DESC, 1, @DT_AGO)
            IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro na contemplacao para devolucao.' GOTO ERRO END
            SET @RECURSOS = @RECURSOS - (@VL_BRUTO - @VL_DESC)
            SET @QT_CONT = @QT_CONT + 1
            SET @VL_CONT = @VL_CONT + (@VL_BRUTO - @VL_DESC)
        END
    END
END

-- 4. lances: desclassificacao
UPDATE L SET ID_SITUACAO = 4, DS_MOTIVO = 'Cota nao apta (inadimplente ou ja contemplada)'
  FROM TB_LANCE L
 WHERE L.CD_ASSEMBLEIA = @CD_ASSEMBLEIA AND L.ID_SITUACAO = 1
   AND NOT EXISTS (SELECT 1 FROM #APTAS A WHERE A.CD_COTA = L.CD_COTA)

UPDATE TB_LANCE SET ID_SITUACAO = 4, DS_MOTIVO = 'Lance fixo invalido para o grupo'
 WHERE CD_ASSEMBLEIA = @CD_ASSEMBLEIA AND ID_SITUACAO = 1 AND ID_TIPO_LANCE = 2
   AND (@PC_FIXO IS NULL OR PC_LANCE <> @PC_FIXO)

UPDATE TB_LANCE SET ID_SITUACAO = 4, DS_MOTIVO = 'Embutido acima do permitido'
 WHERE CD_ASSEMBLEIA = @CD_ASSEMBLEIA AND ID_SITUACAO = 1
   AND (PC_EMBUTIDO > @PC_EMB_MAX OR PC_EMBUTIDO > PC_LANCE)

UPDATE L SET ID_SITUACAO = 4, DS_MOTIVO = 'Lance acima do saldo devedor de fundo comum'
  FROM TB_LANCE L INNER JOIN #APTAS A ON A.CD_COTA = L.CD_COTA
 WHERE L.CD_ASSEMBLEIA = @CD_ASSEMBLEIA AND L.ID_SITUACAO = 1
   AND L.PC_LANCE > A.PC_FC_ABERTO

IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro na desclassificacao de lances.' GOTO ERRO END

-- 4. lances: classificacao e contemplacao
SET @ORDEM = 0

DECLARE C_LANCES CURSOR LOCAL FAST_FORWARD FOR
    SELECT L.CD_LANCE, L.CD_COTA, L.PC_LANCE, L.PC_EMBUTIDO, A.VL_CREDITO
      FROM TB_LANCE L
     INNER JOIN #APTAS A ON A.CD_COTA = L.CD_COTA
     WHERE L.CD_ASSEMBLEIA = @CD_ASSEMBLEIA AND L.ID_SITUACAO = 1
     ORDER BY L.PC_LANCE DESC, (A.NR_COTA - @NR_REF + @QT_MAX) % @QT_MAX, A.NR_COTA

OPEN C_LANCES
FETCH NEXT FROM C_LANCES INTO @CD_LANCE, @CD_COTA, @PC_LANCE, @PC_EMB, @CREDITO

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @ORDEM = @ORDEM + 1

    IF @SEM_RECURSOS = 1
    BEGIN
        UPDATE TB_LANCE SET ID_SITUACAO = 3, NR_ORDEM = @ORDEM, DS_MOTIVO = 'Sem recursos para contemplar nesta assembleia' WHERE CD_LANCE = @CD_LANCE
    END
    ELSE
    BEGIN
        SET @VL_LANCE = ROUND(@CREDITO * @PC_LANCE / 100, 2)
        SET @VL_EMB = ROUND(@CREDITO * @PC_EMB / 100, 2)
        SET @VL_PROPRIOS = @VL_LANCE - @VL_EMB

        IF @RECURSOS >= @CREDITO - @VL_LANCE
        BEGIN
            INSERT INTO TB_CONTEMPLACAO (CD_COTA, CD_ASSEMBLEIA, ID_TIPO, CD_LANCE, VL_CREDITO, PC_LANCE, VL_LANCE, VL_EMBUTIDO,
                                         VL_RECURSOS_PROPRIOS, VL_CREDITO_LIQUIDO, ID_SITUACAO, DT_CONTEMPLACAO)
            VALUES (@CD_COTA, @CD_ASSEMBLEIA, 2, @CD_LANCE, @CREDITO, @PC_LANCE, @VL_LANCE, @VL_EMB,
                    @VL_PROPRIOS, @CREDITO - @VL_EMB, 1, @DT_AGO)
            IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro na contemplacao por lance.' GOTO ERRO_CURSOR END

            UPDATE TB_COTA SET ID_SITUACAO = 2, DT_SITUACAO = @DT_AGO WHERE CD_COTA = @CD_COTA
            UPDATE TB_LANCE SET ID_SITUACAO = 2, NR_ORDEM = @ORDEM, DS_MOTIVO = NULL WHERE CD_LANCE = @CD_LANCE

            IF @VL_PROPRIOS > 0
            BEGIN
                SELECT @NR_COTA = NR_COTA, @NR_VERSAO = NR_VERSAO FROM TB_COTA WHERE CD_COTA = @CD_COTA
                INSERT INTO TB_GRUPO_MOVIMENTO (CD_GRUPO, SG_FUNDO, ID_TIPO_MOVIMENTO, DT_MOVIMENTO, VL_MOVIMENTO, CD_COTA, DS_HISTORICO, CD_USUARIO)
                VALUES (@CD_GRUPO, 'FC', 4, @DT_AGO, @VL_PROPRIOS, @CD_COTA,
                        'Lance ' + CAST(@PC_LANCE AS VARCHAR) + '% - cota ' + RIGHT('000' + CAST(@NR_COTA AS VARCHAR), 3) + '-' + CAST(@NR_VERSAO AS VARCHAR)
                        + ' - AGO ' + CAST(@NR_ASSEMBLEIA AS VARCHAR), @CD_USUARIO)
                IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro no lancamento do lance.' GOTO ERRO_CURSOR END
            END

            EXEC @RET = SP_AMORTIZA_LANCE @CD_COTA, @PC_LANCE
            IF @@ERROR <> 0 OR @RET <> 0 BEGIN SET @MSG = 'Erro na amortizacao do lance.' GOTO ERRO_CURSOR END

            SET @RECURSOS = @RECURSOS - (@CREDITO - @VL_LANCE)
            SET @QT_CONT = @QT_CONT + 1
            SET @VL_CONT = @VL_CONT + @CREDITO
        END
        ELSE
            UPDATE TB_LANCE SET ID_SITUACAO = 3, NR_ORDEM = @ORDEM, DS_MOTIVO = 'Recursos insuficientes' WHERE CD_LANCE = @CD_LANCE
    END

    FETCH NEXT FROM C_LANCES INTO @CD_LANCE, @CD_COTA, @PC_LANCE, @PC_EMB, @CREDITO
END

CLOSE C_LANCES
DEALLOCATE C_LANCES

UPDATE TB_ASSEMBLEIA
   SET ID_SITUACAO = 2,
       NR_COTA_SORTEADA = @BASE,
       VL_SALDO_FC = @SALDO_FC,
       VL_RECURSOS = @RECURSOS_INI,
       VL_CONTEMPLADO = @VL_CONT,
       QT_CONTEMPLACOES = @QT_CONT,
       DT_APURACAO = @DT_APURACAO,
       CD_USUARIO_APURACAO = @CD_USUARIO
 WHERE CD_ASSEMBLEIA = @CD_ASSEMBLEIA
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao fechar a assembleia.' GOTO ERRO END

UPDATE TB_GRUPO SET QT_ASSEMBLEIAS_REALIZADAS = QT_ASSEMBLEIAS_REALIZADAS + 1 WHERE CD_GRUPO = @CD_GRUPO
IF @@ERROR <> 0 BEGIN SET @MSG = 'Erro ao atualizar o grupo.' GOTO ERRO END

SET @MSG = 'Grupo ' + @NR_GRUPO + ' AGO ' + CAST(@NR_ASSEMBLEIA AS VARCHAR) + ': ' + CAST(@QT_CONT AS VARCHAR) + ' contemplacao(oes)'
EXEC SP_GRAVA_LOG @CD_USUARIO, 'APURA_ASSEMBLEIA', @MSG

COMMIT TRANSACTION
RETURN 0

ERRO_CURSOR:
CLOSE C_LANCES
DEALLOCATE C_LANCES

ERRO:
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
RAISERROR(@MSG, 16, 1)
RETURN 1
GO
