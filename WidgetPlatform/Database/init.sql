CREATE TABLE IF NOT EXISTS tenants (
    id             UUID PRIMARY KEY,
    email          TEXT NOT NULL UNIQUE,
    password_hash  TEXT NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL
);

CREATE TABLE IF NOT EXISTS widgets (
    id             UUID PRIMARY KEY,
    tenant_id      UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    type           TEXT NOT NULL,
    title          TEXT NOT NULL,
    fields_json    JSONB NOT NULL,
    is_active      BOOLEAN NOT NULL DEFAULT TRUE,
    version        INT NOT NULL DEFAULT 1,
    created_at_utc TIMESTAMPTZ NOT NULL
);

CREATE TABLE IF NOT EXISTS submissions (
    id             UUID PRIMARY KEY,
    widget_id      UUID NOT NULL REFERENCES widgets(id) ON DELETE CASCADE,
    tenant_id      UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    data_json      JSONB NOT NULL,
    ip_address     TEXT,
    country        TEXT,
    city           TEXT,
    created_at_utc TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_widgets_tenant ON widgets (tenant_id);
CREATE INDEX IF NOT EXISTS ix_submissions_widget ON submissions (widget_id);
CREATE INDEX IF NOT EXISTS ix_submissions_tenant_time ON submissions (tenant_id, created_at_utc DESC);