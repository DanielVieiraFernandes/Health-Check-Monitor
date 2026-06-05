-- S002: Adiciona colunas para tipo de sistema e resposta esperada

ALTER TABLE monitored_systems
ADD COLUMN IF NOT EXISTS system_type INTEGER NOT NULL DEFAULT 1;

ALTER TABLE monitored_systems
ADD COLUMN IF NOT EXISTS expected_http_status INTEGER;

ALTER TABLE monitored_systems
ADD COLUMN IF NOT EXISTS expected_body_text TEXT;

COMMENT ON COLUMN monitored_systems.system_type IS '1=Web API, 2=Front-end, 3=Banco SQL, 4=Banco NoSQL';
COMMENT ON COLUMN monitored_systems.expected_http_status IS 'HTTP status code esperado';
COMMENT ON COLUMN monitored_systems.expected_body_text IS 'Texto esperado no body (Front-end)';
