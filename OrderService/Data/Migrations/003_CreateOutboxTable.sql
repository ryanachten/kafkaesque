CREATE TABLE outbox_events (
  id                UUID PRIMARY KEY,
  entity_type    TEXT NOT NULL, -- Name of domain entity relating to an event; allows identifying all events pertaining to a given entity 
  entity_id      TEXT NOT NULL, -- ID of entity relating to an event

  event_type        TEXT NOT NULL, -- Name of a given event
  event_version     INT NOT NULL, -- Version of a given event 

  payload           JSONB NOT NULL,

  occurred_at       TIMESTAMPTZ NOT NULL,
  status            TEXT NOT NULL DEFAULT 'PENDING',

  retry_count       INT NOT NULL DEFAULT 0,
  last_error        TEXT NULL,
  published_at      TIMESTAMPTZ NULL
);

CREATE INDEX idx_outbox_pending
  ON outbox_events (status, occurred_at);

CREATE INDEX idx_outbox_entity
  ON outbox_events (entity_type, entity_id);

