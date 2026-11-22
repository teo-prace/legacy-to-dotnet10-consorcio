/*****************************************************************************
 SGC - dados iniciais: parametros, usuarios, indices, feriados, bens,
 pontos de venda, planos e grupos.
 Indices: valores ILUSTRATIVOS (nao oficiais).
*****************************************************************************/
SET NOCOUNT ON
GO

/* Parametros ------------------------------------------------------------- */
INSERT INTO TB_PARAMETRO (NM_PARAMETRO, DS_VALOR, DS_DESCRICAO) VALUES
('NM_ADMINISTRADORA',        'CeCons Administradora de Consórcios Ltda', 'Razão social'),
('NR_CNPJ_ADMINISTRADORA',   '00000000000191',  'CNPJ (fictício)'),
('PC_MULTA_ATRASO',          '2.00',            '% multa por atraso'),
('PC_JUROS_MES',             '1.00',            '% juros de mora ao mês, pro rata'),
('QT_PARCELAS_EXCLUSAO',     '3',               'Parcelas em atraso para exclusão'),
('QT_DIAS_ARREPENDIMENTO',   '7',               'Prazo de arrependimento (dias)'),
('PC_MULTA_RESCISORIA',      '10.00',           '% retido na devolução a excluídos e desistentes'),
('NR_BANCO',                 '999',             'Banco cobrador (fictício)'),
('NR_AGENCIA',               '0001',            'Agência cedente'),
('NR_CONTA_CEDENTE',         '123456',          'Conta cedente'),
('NR_CARTEIRA',              '17',              'Carteira de cobrança'),
('SEQ_NOSSO_NUMERO',         '0',               'Último nosso número emitido'),
('DS_PASTA_RETORNO',         'C:\SGC\Retorno',  'Pasta dos arquivos de retorno bancário')
GO

/* Usuarios (senha inicial: cecons123) ------------------------------------ */
INSERT INTO TB_USUARIO (DS_LOGIN, DS_SENHA, NM_USUARIO, DS_EMAIL, ID_PERFIL) VALUES
('admin',    CONVERT(VARCHAR(32), HASHBYTES('MD5', 'cecons123'), 2), 'Administrador do sistema', NULL, 1),
('msouza',   CONVERT(VARCHAR(32), HASHBYTES('MD5', 'cecons123'), 2), 'Marcia Souza',   'msouza@cecons.local', 2),
('jpereira', CONVERT(VARCHAR(32), HASHBYTES('MD5', 'cecons123'), 2), 'Joao Pereira',   'jpereira@cecons.local', 1),
('rafonso',  CONVERT(VARCHAR(32), HASHBYTES('MD5', 'cecons123'), 2), 'Renato Afonso',  'rafonso@cecons.local', 2),
('consulta', CONVERT(VARCHAR(32), HASHBYTES('MD5', 'cecons123'), 2), 'Usuário consulta', NULL, 3)
GO
UPDATE TB_USUARIO SET FL_ATIVO = 'N' WHERE DS_LOGIN = 'rafonso'
GO

/* Indices ---------------------------------------------------------------- */
INSERT INTO TB_INDICE (CD_INDICE, SG_INDICE, NM_INDICE) VALUES
(1, 'INCC-M', 'Índice Nacional de Custo da Construção - Mercado'),
(2, 'IPCA',   'Índice Nacional de Preços ao Consumidor Amplo')
GO

DECLARE @AM INT, @ANO INT, @MES INT
SET @ANO = 2024
SET @MES = 1
WHILE @ANO * 100 + @MES <= 202610
BEGIN
    SET @AM = @ANO * 100 + @MES
    INSERT INTO TB_INDICE_VALOR (CD_INDICE, NR_ANO_MES, PC_VARIACAO, DT_CADASTRO)
    VALUES (1, @AM, 0.18 + ((@AM * 37) % 41) / 100.0, DATEADD(MONTH, 1, CAST(CAST(@AM AS VARCHAR) + '10' AS DATETIME)))
    INSERT INTO TB_INDICE_VALOR (CD_INDICE, NR_ANO_MES, PC_VARIACAO, DT_CADASTRO)
    VALUES (2, @AM, 0.12 + ((@AM * 53) % 37) / 100.0, DATEADD(MONTH, 1, CAST(CAST(@AM AS VARCHAR) + '12' AS DATETIME)))
    SET @MES = @MES + 1
    IF @MES = 13 BEGIN SET @MES = 1 SET @ANO = @ANO + 1 END
END
GO

/* Feriados --------------------------------------------------------------- */
INSERT INTO TB_FERIADO (DT_FERIADO, DS_FERIADO, ID_ABRANGENCIA) VALUES
('20260101', 'Confraternização Universal', 'N'),
('20260216', 'Carnaval', 'N'),
('20260217', 'Carnaval', 'N'),
('20260403', 'Sexta-feira Santa', 'N'),
('20260421', 'Tiradentes', 'N'),
('20260501', 'Dia do Trabalho', 'N'),
('20260604', 'Corpus Christi', 'N'),
('20260907', 'Independência', 'N'),
('20260920', 'Revolução Farroupilha', 'E'),
('20261012', 'Nossa Senhora Aparecida', 'N'),
('20261102', 'Finados', 'N'),
('20261115', 'Proclamação da República', 'N'),
('20261120', 'Dia da Consciência Negra', 'N'),
('20261225', 'Natal', 'N'),
('20270101', 'Confraternização Universal', 'N'),
('20270208', 'Carnaval', 'N'),
('20270209', 'Carnaval', 'N'),
('20270326', 'Sexta-feira Santa', 'N'),
('20270421', 'Tiradentes', 'N'),
('20270501', 'Dia do Trabalho', 'N'),
('20270527', 'Corpus Christi', 'N'),
('20270907', 'Independência', 'N'),
('20270920', 'Revolução Farroupilha', 'E'),
('20271012', 'Nossa Senhora Aparecida', 'N'),
('20271102', 'Finados', 'N'),
('20271115', 'Proclamação da República', 'N'),
('20271120', 'Dia da Consciência Negra', 'N'),
('20271225', 'Natal', 'N')
GO

/* Bens ------------------------------------------------------------------- */
SET IDENTITY_INSERT TB_BEM ON
INSERT INTO TB_BEM (CD_BEM, DS_BEM, ID_TIPO_BEM, CD_INDICE, VL_BEM) VALUES
( 1, 'CRÉDITO IMOBILIÁRIO 200 MIL', 1, 1, 200000),
( 2, 'CRÉDITO IMOBILIÁRIO 250 MIL', 1, 1, 250000),
( 3, 'CRÉDITO IMOBILIÁRIO 300 MIL', 1, 1, 300000),
( 4, 'CRÉDITO IMOBILIÁRIO 350 MIL', 1, 1, 350000),
( 5, 'CRÉDITO IMOBILIÁRIO 400 MIL', 1, 1, 400000),
( 6, 'CRÉDITO VEÍCULO 50 MIL',      2, 2,  50000),
( 7, 'CRÉDITO VEÍCULO 65 MIL',      2, 2,  65000),
( 8, 'CRÉDITO VEÍCULO 80 MIL',      2, 2,  80000),
( 9, 'CRÉDITO VEÍCULO 100 MIL',     2, 2, 100000),
(10, 'CRÉDITO VEÍCULO 120 MIL',     2, 2, 120000),
(11, 'CRÉDITO PESADOS 250 MIL',     3, 2, 250000),
(12, 'CRÉDITO PESADOS 350 MIL',     3, 2, 350000),
(13, 'CRÉDITO PESADOS 450 MIL',     3, 2, 450000),
(14, 'CRÉDITO VEÍCULO 40 MIL',      2, 2,  40000)
SET IDENTITY_INSERT TB_BEM OFF
GO
UPDATE TB_BEM SET FL_ATIVO = 'N' WHERE CD_BEM = 14
GO

-- vigencias 2024 e 2025 (valores anteriores aos reajustes) e 2026 (valor atual)
INSERT INTO TB_BEM_VALOR (CD_BEM, DT_VIGENCIA, VL_BEM, DS_MOTIVO, CD_USUARIO)
SELECT CD_BEM, '20240101', ROUND(VL_BEM / 1.0512 / 1.0468, -2), 'Reajuste anual 2024', 3 FROM TB_BEM
UNION ALL
SELECT CD_BEM, '20250101', ROUND(VL_BEM / 1.0512, -2), 'Reajuste anual 2025', 3 FROM TB_BEM
UNION ALL
SELECT CD_BEM, '20260101', VL_BEM, 'Reajuste anual 2026', 3 FROM TB_BEM
GO

/* Pontos de venda e vendedores -------------------------------------------- */
SET IDENTITY_INSERT TB_PONTO_VENDA ON
INSERT INTO TB_PONTO_VENDA (CD_PONTO_VENDA, NM_PONTO_VENDA, NM_CIDADE, SG_UF, FL_ATIVO) VALUES
(1, 'MATRIZ - CENTRO',       'Porto Alegre',   'RS', 'S'),
(2, 'LOJA CAXIAS',           'Caxias do Sul',  'RS', 'S'),
(3, 'LOJA PELOTAS',          'Pelotas',        'RS', 'S'),
(4, 'LOJA SANTA MARIA',      'Santa Maria',    'RS', 'S'),
(5, 'LOJA PASSO FUNDO',      'Passo Fundo',    'RS', 'S'),
(6, 'LOJA NOVO HAMBURGO',    'Novo Hamburgo',  'RS', 'N'),
(7, 'CORRESPONDENTE ONLINE', 'Porto Alegre',   'RS', 'S')
SET IDENTITY_INSERT TB_PONTO_VENDA OFF
GO

INSERT INTO TB_VENDEDOR (CD_PONTO_VENDA, NM_VENDEDOR) VALUES
(1, 'ANA PAULA MACHADO'), (1, 'GILBERTO ROSA'), (1, 'LUCIANA FERREIRA'),
(2, 'MARCOS ZANOTTO'), (2, 'TATIANE BORTOLINI'),
(3, 'FERNANDO AVILA'), (3, 'CAROLINE NUNES'),
(4, 'PAULO DORNELES'), (4, 'BRUNA LOPES'),
(5, 'RICARDO TESSARO'), (5, 'SIMONE BAGGIO'),
(6, 'EDSON KLEIN'),
(7, 'ATENDIMENTO DIGITAL')
GO
UPDATE TB_VENDEDOR SET FL_ATIVO = 'N' WHERE CD_PONTO_VENDA = 6
GO

/* Planos ----------------------------------------------------------------- */
SET IDENTITY_INSERT TB_PLANO ON
INSERT INTO TB_PLANO (CD_PLANO, NM_PLANO, ID_TIPO_BEM, QT_PRAZO_MESES, PC_TAXA_ADM, PC_ANTECIPACAO_TA, PC_FUNDO_RESERVA,
                      PC_LANCE_EMBUTIDO_MAX, PC_LANCE_FIXO, QT_COTAS_MINIMO, QT_COTAS_MAXIMO, NR_DIA_VENCIMENTO,
                      NR_DIA_ASSEMBLEIA, CD_INDICE, DS_VERSAO_REGULAMENTO, FL_ATIVO, DT_CADASTRO) VALUES
(1, 'VEÍCULO LEVE 72 MESES (2019)', 2,  72, 16.0000, 0.0000, 2.0000, 25.0000, NULL,    80,  90, 10, 20, 2, 'REG-VEIC-2019.1', 'N', '20190304'),
(2, 'IMÓVEL 150 MESES',             1, 150, 18.0000, 1.0000, 2.0000, 30.0000, 30.0000, 150, 165, 10, 18, 1, 'REG-IMOV-2025.1', 'S', '20250210'),
(3, 'VEÍCULO LEVE 60 MESES',        2,  60, 15.0000, 1.0000, 2.0000, 30.0000, 25.0000,  72,  85, 15, 25, 2, 'REG-VEIC-2024.2', 'S', '20240805'),
(4, 'PESADOS 80 MESES',             3,  80, 16.0000, 1.0000, 3.0000, 25.0000, NULL,     90, 100,  5, 12, 2, 'REG-PES-2025.1',  'S', '20250915'),
(5, 'VEÍCULO LEVE 60 MESES (2024)', 2,  60, 16.0000, 1.0000, 2.0000, 25.0000, NULL,     72,  80, 15, 25, 2, 'REG-VEIC-2024.1', 'N', '20240205')
SET IDENTITY_INSERT TB_PLANO OFF
GO

INSERT INTO TB_PLANO_BEM (CD_PLANO, CD_BEM) VALUES
(5, 6), (5, 7), (5, 8), (5, 9),
(1, 14), (1, 6), (1, 7),
(2, 1), (2, 2), (2, 3), (2, 4), (2, 5),
(3, 6), (3, 7), (3, 8), (3, 9), (3, 10),
(4, 11), (4, 12), (4, 13)
GO

/* Grupos ------------------------------------------------------------------
   Numeracao: 1o digito significativo = tipo de bem (1 imovel, 2 veiculo,
   3 pesados) + sequencial.
--------------------------------------------------------------------------- */
SET IDENTITY_INSERT TB_GRUPO ON
INSERT INTO TB_GRUPO (CD_GRUPO, NR_GRUPO, CD_PLANO, ID_TIPO_BEM, QT_PRAZO_MESES, PC_TAXA_ADM, PC_ANTECIPACAO_TA, PC_FUNDO_RESERVA,
                      PC_LANCE_EMBUTIDO_MAX, PC_LANCE_FIXO, QT_COTAS_MINIMO, QT_COTAS_MAXIMO, NR_DIA_VENCIMENTO, NR_DIA_ASSEMBLEIA,
                      CD_INDICE, DS_VERSAO_REGULAMENTO, DT_ABERTURA, ID_SITUACAO, DT_SITUACAO, CD_USUARIO_CADASTRO, DT_CADASTRO)
SELECT 1, '001041', CD_PLANO, ID_TIPO_BEM, QT_PRAZO_MESES, PC_TAXA_ADM, PC_ANTECIPACAO_TA, PC_FUNDO_RESERVA,
       PC_LANCE_EMBUTIDO_MAX, PC_LANCE_FIXO, QT_COTAS_MINIMO, QT_COTAS_MAXIMO, NR_DIA_VENCIMENTO, NR_DIA_ASSEMBLEIA,
       CD_INDICE, DS_VERSAO_REGULAMENTO, '20260504', 1, '20260504', 3, '20260504'
  FROM TB_PLANO WHERE CD_PLANO = 2
UNION ALL
SELECT 2, '002087', CD_PLANO, ID_TIPO_BEM, QT_PRAZO_MESES, PC_TAXA_ADM, PC_ANTECIPACAO_TA, PC_FUNDO_RESERVA,
       PC_LANCE_EMBUTIDO_MAX, PC_LANCE_FIXO, QT_COTAS_MINIMO, QT_COTAS_MAXIMO, NR_DIA_VENCIMENTO, NR_DIA_ASSEMBLEIA,
       CD_INDICE, DS_VERSAO_REGULAMENTO, '20260803', 1, '20260803', 3, '20260803'
  FROM TB_PLANO WHERE CD_PLANO = 3
UNION ALL
SELECT 3, '003012', CD_PLANO, ID_TIPO_BEM, QT_PRAZO_MESES, PC_TAXA_ADM, PC_ANTECIPACAO_TA, PC_FUNDO_RESERVA,
       PC_LANCE_EMBUTIDO_MAX, PC_LANCE_FIXO, QT_COTAS_MINIMO, QT_COTAS_MAXIMO, NR_DIA_VENCIMENTO, NR_DIA_ASSEMBLEIA,
       CD_INDICE, DS_VERSAO_REGULAMENTO, '20261109', 1, '20261109', 3, '20261109'
  FROM TB_PLANO WHERE CD_PLANO = 4
UNION ALL
SELECT 4, '002071', CD_PLANO, ID_TIPO_BEM, QT_PRAZO_MESES, PC_TAXA_ADM, PC_ANTECIPACAO_TA, PC_FUNDO_RESERVA,
       PC_LANCE_EMBUTIDO_MAX, PC_LANCE_FIXO, QT_COTAS_MINIMO, QT_COTAS_MAXIMO, NR_DIA_VENCIMENTO, NR_DIA_ASSEMBLEIA,
       CD_INDICE, DS_VERSAO_REGULAMENTO, '20240304', 1, '20240304', 3, '20240304'
  FROM TB_PLANO WHERE CD_PLANO = 5
SET IDENTITY_INSERT TB_GRUPO OFF
GO

INSERT INTO TB_GRUPO_BEM (CD_GRUPO, CD_BEM)
SELECT 4, CD_BEM FROM TB_PLANO_BEM WHERE CD_PLANO = 5
UNION ALL
SELECT 1, CD_BEM FROM TB_PLANO_BEM WHERE CD_PLANO = 2
UNION ALL
SELECT 2, CD_BEM FROM TB_PLANO_BEM WHERE CD_PLANO = 3
UNION ALL
SELECT 3, CD_BEM FROM TB_PLANO_BEM WHERE CD_PLANO = 4
GO
