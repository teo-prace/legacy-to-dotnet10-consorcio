/*****************************************************************************
 SGC - conferencia de integridade (suporte)
 Rodar depois de cargas, fechamento de mes ou chamado de diferenca de saldo.
 Cada linha: verificacao, quantidade de ocorrencias (0 = ok).

 2016-10 J.P.
*****************************************************************************/
SET NOCOUNT ON

DECLARE @R TABLE (NR INT, DS_VERIFICACAO VARCHAR(120), QT_OCORRENCIAS INT)

-- 1. plano de parcelas: fundo comum (com o abatido por lance), taxa e fundo de reserva fecham no grupo
INSERT INTO @R
SELECT 1, 'Cotas cujo plano nao fecha 100% FC / taxa / fundo de reserva do grupo', COUNT(*)
  FROM (SELECT C.CD_COTA,
               SUM(P.PC_FUNDO_COMUM + ISNULL(P.PC_FC_AMORTIZADO_LANCE, 0)) AS FC,
               SUM(P.PC_TAXA_ADM) AS TA, SUM(P.PC_FUNDO_RESERVA) AS FR, MAX(G.PC_TAXA_ADM) AS TA_G, MAX(G.PC_FUNDO_RESERVA) AS FR_G
          FROM TB_COTA C INNER JOIN TB_PARCELA P ON P.CD_COTA = C.CD_COTA INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO
         WHERE C.ID_SITUACAO <> 6
         GROUP BY C.CD_COTA) X
 WHERE X.FC <> 100 OR X.TA <> X.TA_G OR X.FR <> X.FR_G

-- 2. % amortizado da cota = FC das parcelas pagas + lances
INSERT INTO @R
SELECT 2, '% amortizado da cota diferente de FC pago + lances', COUNT(*)
  FROM TB_COTA C
 WHERE C.ID_SITUACAO <> 6
   AND ABS(C.PC_AMORTIZADO - ISNULL((SELECT SUM(P.PC_FUNDO_COMUM) FROM TB_PARCELA P WHERE P.CD_COTA = C.CD_COTA AND P.DT_PAGAMENTO IS NOT NULL), 0)
                           - ISNULL((SELECT SUM(T.PC_LANCE) FROM TB_CONTEMPLACAO T WHERE T.CD_COTA = C.CD_COTA AND T.ID_TIPO = 2), 0)) > 0.000001

-- 3. recebimentos no caixa = valores pagos das parcelas (componentes + encargos + excedente)
INSERT INTO @R
SELECT 3, 'Grupos com recebimento no caixa diferente do pago nas parcelas', COUNT(*)
  FROM TB_GRUPO G
 WHERE ISNULL((SELECT SUM(M.VL_MOVIMENTO) FROM TB_GRUPO_MOVIMENTO M WHERE M.CD_GRUPO = G.CD_GRUPO AND M.ID_TIPO_MOVIMENTO IN (1, 2, 7, 8)), 0)
    <> ISNULL((SELECT SUM(P.VL_PAGO) FROM TB_PARCELA P INNER JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA
                WHERE C.CD_GRUPO = G.CD_GRUPO AND P.DT_PAGAMENTO IS NOT NULL AND C.ID_SITUACAO <> 6), 0)

-- 4. creditos e devolucoes pagos = contemplacoes liberadas
INSERT INTO @R
SELECT 4, 'Grupos com creditos/devolucoes pagos diferentes das contemplacoes liberadas', COUNT(*)
  FROM TB_GRUPO G
 WHERE ISNULL((SELECT -SUM(M.VL_MOVIMENTO) FROM TB_GRUPO_MOVIMENTO M WHERE M.CD_GRUPO = G.CD_GRUPO AND M.ID_TIPO_MOVIMENTO IN (3, 5)), 0)
    <> ISNULL((SELECT SUM(T.VL_CREDITO_LIQUIDO) FROM TB_CONTEMPLACAO T INNER JOIN TB_COTA C ON C.CD_COTA = T.CD_COTA
                WHERE C.CD_GRUPO = G.CD_GRUPO AND T.ID_SITUACAO = 2), 0)

-- 5. fundo comum nao pode ficar negativo
INSERT INTO @R
SELECT 5, 'Grupos com fundo comum negativo', COUNT(*)
  FROM TB_GRUPO G
 WHERE (SELECT ISNULL(SUM(VL_MOVIMENTO), 0) FROM TB_GRUPO_MOVIMENTO M WHERE M.CD_GRUPO = G.CD_GRUPO AND M.SG_FUNDO = 'FC') < 0

-- 6. contemplada com parcela vencida em aberto na data da assembleia
INSERT INTO @R
SELECT 6, 'Contemplacoes (sorteio/lance) de cota inadimplente na data da AGO', COUNT(*)
  FROM TB_CONTEMPLACAO T INNER JOIN TB_ASSEMBLEIA A ON A.CD_ASSEMBLEIA = T.CD_ASSEMBLEIA
 WHERE T.ID_TIPO IN (1, 2)
   AND EXISTS (SELECT 1 FROM TB_PARCELA P
                WHERE P.CD_COTA = T.CD_COTA AND dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) < A.DT_ASSEMBLEIA
                  AND (P.DT_PAGAMENTO IS NULL OR P.DT_PAGAMENTO >= A.DT_ASSEMBLEIA) AND P.ID_SITUACAO <> 4)

-- 7. assembleia contemplou mais do que os recursos
INSERT INTO @R
SELECT 7, 'Assembleias com contemplacao acima dos recursos', COUNT(*)
  FROM TB_ASSEMBLEIA A
 WHERE A.ID_SITUACAO = 2
   AND (SELECT ISNULL(SUM(T.VL_CREDITO - T.VL_LANCE - T.VL_DESCONTO), 0) FROM TB_CONTEMPLACAO T WHERE T.CD_ASSEMBLEIA = A.CD_ASSEMBLEIA) > A.VL_RECURSOS

-- 8. cota contemplada sem contemplacao (ou o contrario)
INSERT INTO @R
SELECT 8, 'Cotas contempladas/quitadas sem contemplacao valida, ou ativas com contemplacao', COUNT(*)
  FROM TB_COTA C
 WHERE (C.ID_SITUACAO IN (2, 5) AND NOT EXISTS (SELECT 1 FROM TB_CONTEMPLACAO T WHERE T.CD_COTA = C.CD_COTA AND T.ID_TIPO IN (1, 2) AND T.ID_SITUACAO IN (1, 2)))
    OR (C.ID_SITUACAO = 1 AND EXISTS (SELECT 1 FROM TB_CONTEMPLACAO T WHERE T.CD_COTA = C.CD_COTA AND T.ID_TIPO IN (1, 2) AND T.ID_SITUACAO IN (1, 2)))

-- 9. parcela paga sem pagamento / emitida sem nosso numero
INSERT INTO @R
SELECT 9, 'Parcelas pagas sem data/valor ou emitidas sem nosso numero', COUNT(*)
  FROM TB_PARCELA
 WHERE (ID_SITUACAO = 3 AND (DT_PAGAMENTO IS NULL OR VL_PAGO IS NULL))
    OR (ID_SITUACAO = 2 AND NR_NOSSO_NUMERO IS NULL)

-- 10. nosso numero duplicado
INSERT INTO @R
SELECT 10, 'Nosso numero duplicado', COUNT(*)
  FROM (SELECT NR_NOSSO_NUMERO FROM TB_PARCELA WHERE NR_NOSSO_NUMERO IS NOT NULL GROUP BY NR_NOSSO_NUMERO HAVING COUNT(*) > 1) X

-- 11. excluida/desistente com parcela ainda cobravel
INSERT INTO @R
SELECT 11, 'Cotas excluidas/desistentes com parcela em aberto', COUNT(DISTINCT C.CD_COTA)
  FROM TB_COTA C INNER JOIN TB_PARCELA P ON P.CD_COTA = C.CD_COTA
 WHERE C.ID_SITUACAO IN (3, 4) AND P.ID_SITUACAO IN (1, 2)

-- 12. grupo com cotas acima do maximo
INSERT INTO @R
SELECT 12, 'Grupos com numero de cotas ocupadas acima do maximo', COUNT(*)
  FROM TB_GRUPO G
 WHERE (SELECT COUNT(*) FROM TB_COTA C WHERE C.CD_GRUPO = G.CD_GRUPO AND C.ID_SITUACAO <> 6) > G.QT_COTAS_MAXIMO

SELECT NR, DS_VERIFICACAO, QT_OCORRENCIAS, CASE WHEN QT_OCORRENCIAS = 0 THEN 'OK' ELSE 'VERIFICAR' END AS RESULTADO
  FROM @R ORDER BY NR
