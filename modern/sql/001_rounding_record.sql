/*****************************************************************************
  SGC.Novo - rounding record for late fees (ADR-005).

  Fines and interest round half a cent up. The legacy payment screen rounded
  half to even (banker's). Every payment posted by the new system with fees
  records here:
    - the exact value (before rounding),
    - the value charged (new rule),
    - what the old rule (banker's) would have charged,
  so "how much did this decision change, and for whom?" is answered with data.

  Own schema (novo): no legacy table changes. Written in the same transaction
  as the payment (SP_BAIXA_PARCELA): no payment without its record.
  Idempotent: safe to run more than once.
*****************************************************************************/
IF SCHEMA_ID('novo') IS NULL EXEC('CREATE SCHEMA novo')
GO
IF OBJECT_ID('novo.TB_ENCARGO_ARREDONDAMENTO') IS NULL
CREATE TABLE novo.TB_ENCARGO_ARREDONDAMENTO (
    CD_REGISTRO         INT IDENTITY PRIMARY KEY,
    CD_PARCELA          INT            NOT NULL,
    DT_REGISTRO         DATETIME2(0)   NOT NULL DEFAULT SYSDATETIME(),
    DT_PAGAMENTO        DATE           NOT NULL,
    QT_DIAS_ATRASO      INT            NOT NULL,
    VL_PARCELA          MONEY          NOT NULL,
    VL_MULTA_EXATA      DECIMAL(19,8)  NOT NULL,
    VL_JUROS_EXATO      DECIMAL(19,8)  NOT NULL,
    VL_MULTA            MONEY          NOT NULL,   -- charged (half up)
    VL_JUROS            MONEY          NOT NULL,
    VL_MULTA_BANCARIO   MONEY          NOT NULL,   -- what the legacy payment screen would charge
    VL_JUROS_BANCARIO   MONEY          NOT NULL,
    VL_DIFERENCA        AS (VL_MULTA + VL_JUROS - VL_MULTA_BANCARIO - VL_JUROS_BANCARIO) PERSISTED,
    DS_REGRA            VARCHAR(20)    NOT NULL,   -- 'meio-para-cima'
    CD_USUARIO          INT            NOT NULL
)
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ENCARGO_ARREDONDAMENTO_PARCELA')
CREATE INDEX IX_ENCARGO_ARREDONDAMENTO_PARCELA ON novo.TB_ENCARGO_ARREDONDAMENTO (CD_PARCELA)
GO
