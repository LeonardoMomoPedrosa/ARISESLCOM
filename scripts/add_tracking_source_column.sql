-- Script para adicionar coluna source nas tabelas tbTrackHistory e tbTrackControl
-- Executar este script no banco de dados antes de usar a funcionalidade

-- Adicionar coluna source em tbTrackHistory se não existir
IF NOT EXISTS (
    SELECT * 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[tbTrackHistory]') 
    AND name = 'source'
)
BEGIN
    ALTER TABLE tbTrackHistory 
    ADD source VARCHAR(1) NOT NULL DEFAULT 'E';
    
    PRINT 'Coluna source adicionada com sucesso na tabela tbTrackHistory';
END
ELSE
BEGIN
    PRINT 'Coluna source já existe na tabela tbTrackHistory';
END

-- Adicionar coluna source em tbTrackControl se não existir
IF NOT EXISTS (
    SELECT * 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[tbTrackControl]') 
    AND name = 'source'
)
BEGIN
    ALTER TABLE tbTrackControl 
    ADD source VARCHAR(1) NOT NULL DEFAULT 'E';
    
    PRINT 'Coluna source adicionada com sucesso na tabela tbTrackControl';
END
ELSE
BEGIN
    PRINT 'Coluna source já existe na tabela tbTrackControl';
END

-- Atualizar registros existentes em tbTrackHistory para 'E' (E-Commerce) como padrão
UPDATE tbTrackHistory 
SET source = 'E' 
WHERE source IS NULL OR source = '';

PRINT 'Registros existentes em tbTrackHistory atualizados com origem padrão (E-Commerce)';

-- Atualizar registros existentes em tbTrackControl para 'E' (E-Commerce) como padrão
UPDATE tbTrackControl 
SET source = 'E' 
WHERE source IS NULL OR source = '';

PRINT 'Registros existentes em tbTrackControl atualizados com origem padrão (E-Commerce)';

