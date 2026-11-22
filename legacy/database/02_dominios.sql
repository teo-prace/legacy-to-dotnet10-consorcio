/*****************************************************************************
 SGC - dominios
 Obs.: as telas e procedures usam os codigos direto; esta tabela serve
 para descricoes em relatorios e combos.
*****************************************************************************/
SET NOCOUNT ON
GO

INSERT INTO TB_DOMINIO (NM_DOMINIO, CD_VALOR, DS_VALOR) VALUES
('PERFIL', '1', 'Administrador'),
('PERFIL', '2', 'Operador'),
('PERFIL', '3', 'Consulta'),

('TIPO_BEM', '1', 'Imóvel'),
('TIPO_BEM', '2', 'Veículo leve'),
('TIPO_BEM', '3', 'Veículo pesado'),
('TIPO_BEM', '4', 'Serviços'),

('ESTADO_CIVIL', '1', 'Solteiro(a)'),
('ESTADO_CIVIL', '2', 'Casado(a)'),
('ESTADO_CIVIL', '3', 'Divorciado(a)'),
('ESTADO_CIVIL', '4', 'Viúvo(a)'),
('ESTADO_CIVIL', '5', 'União estável'),

('SITUACAO_GRUPO', '1', 'Em formação'),
('SITUACAO_GRUPO', '2', 'Em andamento'),
('SITUACAO_GRUPO', '3', 'Encerrado'),
('SITUACAO_GRUPO', '4', 'Cancelado'),

('SITUACAO_COTA', '1', 'Ativa'),
('SITUACAO_COTA', '2', 'Contemplada'),
('SITUACAO_COTA', '3', 'Excluída'),
('SITUACAO_COTA', '4', 'Desistente'),
('SITUACAO_COTA', '5', 'Quitada'),
('SITUACAO_COTA', '6', 'Cancelada'),

('SITUACAO_PROPOSTA', '1', 'Digitada'),
('SITUACAO_PROPOSTA', '2', 'Aguardando pagamento'),
('SITUACAO_PROPOSTA', '3', 'Efetivada'),
('SITUACAO_PROPOSTA', '4', 'Cancelada'),
('SITUACAO_PROPOSTA', '5', 'Arrependimento'),

('SITUACAO_PARCELA', '1', 'Aberta'),
('SITUACAO_PARCELA', '2', 'Emitida'),
('SITUACAO_PARCELA', '3', 'Paga'),
('SITUACAO_PARCELA', '4', 'Cancelada'),

('TIPO_MOVIMENTO', '1', 'Recebimento de parcela'),
('TIPO_MOVIMENTO', '2', 'Estorno por arrependimento'),
('TIPO_MOVIMENTO', '3', 'Crédito de contemplação'),
('TIPO_MOVIMENTO', '4', 'Recebimento de lance'),
('TIPO_MOVIMENTO', '5', 'Devolução de valores'),
('TIPO_MOVIMENTO', '6', 'Transferência à administradora'),
('TIPO_MOVIMENTO', '7', 'Multa e juros'),
('TIPO_MOVIMENTO', '8', 'Pagamento a maior'),

('ORIGEM_BAIXA', '1', 'Manual'),
('ORIGEM_BAIXA', '2', 'Retorno bancário'),
('ORIGEM_BAIXA', '3', 'Adesão'),

('SITUACAO_ASSEMBLEIA', '1', 'Agendada'),
('SITUACAO_ASSEMBLEIA', '2', 'Apurada'),
('SITUACAO_ASSEMBLEIA', '3', 'Cancelada'),

('TIPO_LANCE', '1', 'Livre'),
('TIPO_LANCE', '2', 'Fixo'),

('SITUACAO_LANCE', '1', 'Registrado'),
('SITUACAO_LANCE', '2', 'Vencedor'),
('SITUACAO_LANCE', '3', 'Não contemplado'),
('SITUACAO_LANCE', '4', 'Desclassificado'),
('SITUACAO_LANCE', '5', 'Cancelado'),

('TIPO_CONTEMPLACAO', '1', 'Sorteio'),
('TIPO_CONTEMPLACAO', '2', 'Lance'),
('TIPO_CONTEMPLACAO', '3', 'Devolução'),

('SITUACAO_CONTEMPLACAO', '1', 'Aguardando liberação'),
('SITUACAO_CONTEMPLACAO', '2', 'Crédito liberado'),
('SITUACAO_CONTEMPLACAO', '3', 'Cancelada')
GO
