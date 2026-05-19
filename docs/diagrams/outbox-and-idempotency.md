# Eaat — Outbox Pattern + Idempotent Receivers

To mønstre som arbejder sammen for at sikre exactly-once processing samt at-least-once delivery.

**OBS**: Nedunder ses udelukkende mermaid kode for de forskellige diagrammer. De kan loades i al mermaid software som f.eks. [mermaid.live](https://mermaid-js.github.io/mermaid-live-editor/).
Ellers findes diagrammerne også i images mappen i samme diagrams folder.


## Outbox-pattern

```mermaid
    sequenceDiagram
    autonumber
    participant API as API Endpoint
    participant DB as Service DB
    participant OP as OutboxPublisher<br/>(BackgroundService)
    participant MQ as RabbitMQ

    Note over API,DB: Indenfor én DB-transaktion
    activate DB
    API->>DB: BEGIN TRANSACTION
    API->>DB: INSERT domain state<br/>(fx orders eller deliveries)
    API->>DB: INSERT outbox_messages<br/>(payload + EventId)
    API->>DB: COMMIT
    deactivate DB
    API-->>API: Return 201 Created

    Note over OP,MQ: Asynkront, hvert 2. sek
    loop Polling
        OP->>DB: SELECT * FROM outbox_messages<br/>WHERE published_at IS NULL
        OP->>MQ: BasicPublishAsync (eaat.OrderPlaced)
        OP->>DB: UPDATE published_at = NOW()
    end
```

## Idempotent receivers 

```mermaid
    sequenceDiagram
    autonumber
    participant MQ as RabbitMQ
    participant EC as EventConsumerBase
    participant IC as IdempotencyChecker
    participant DB as Service DB
    participant H as HandleAsync<br/>(subclass)

    MQ->>+EC: Deliver message (MessageId = EventId)

    EC->>EC: Create DI scope
    EC->>IC: ExecuteOnceAsync(eventId, handler)
    IC->>+DB: SELECT FROM processed_messages<br/>WHERE EventId = ?

    alt Allerede processed (duplicate)
        DB-->>IC: row exists
        IC-->>EC: skip (return false)
        EC->>MQ: BasicAck
    else Ingen duplicate = Nyt event
        DB-->>-IC: no row
        IC->>+H: handler(ct)
        H->>DB: domain updates + SaveChanges
        H-->>-IC: done
        IC->>DB: INSERT processed_messages<br/>+ SaveChanges
        IC-->>EC: handler ran (return true)
        EC->>MQ: BasicAck
    end

    deactivate EC
```

## Tilsammen: effektivt exactly-once-processing

```mermaid
graph LR
    A[Service publish'er] -->|Outbox: at-least-once| B[Broker]
    B -->|Fanout| C[Consumer A]
    B -->|Fanout| D[Consumer B]
    C -->|Idempotent receiver| E[Handler kører max 1 gang]
    D -->|Idempotent receiver| F[Handler kører max 1 gang]
```

## First-to-claim logik

```mermaid
sequenceDiagram
    autonumber
    participant CA as Bud A
    participant CB as Bud B
    participant API as Delivery API
    participant DB as MySQL

    par Begge bude prøver samtidigt
        CA->>API: POST /deliveries/{id}/claim
        API->>+DB: UPDATE deliveries<br/>SET courier_id = A<br/>WHERE id = ? AND courier_id IS NULL
        Note over DB: Row-level lock erhverves
        DB-->>-API: rowsAffected = 1
        API-->>CA: 200 OK (vinder)
    and
        CB->>API: POST /deliveries/{id}/claim
        API->>+DB: UPDATE deliveries<br/>SET courier_id = B<br/>WHERE id = ? AND courier_id IS NULL
        Note over DB: Venter på lock,<br/>derefter WHERE matcher ikke
        DB-->>-API: rowsAffected = 0
        API-->>CB: 409 Conflict (taber)
    end
```
