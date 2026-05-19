# Eaat — Event Flow 

Sekvensdiagram der viser det fule flow lige fra kunder er lægger en ordre til at leveringen er fuldført.

**OBS**: Nedunder ses udelukkende mermaid kode for de forskellige diagrammer. De kan loades i al mermaid software som f.eks. [mermaid.live](https://mermaid-js.github.io/mermaid-live-editor/). 
Ellers findes diagrammerne også i images mappen i samme diagrams folder.

```mermaid
    sequenceDiagram
    autonumber
    participant C as Klient
    participant GW as ApiGateway
    participant OS as OrderService
    participant MQ as RabbitMQ
    participant RS as RestaurantService
    participant DS as DeliveryService
    participant NS as NotificationService

    Note over C,NS: 1. Kunde bestiller
    C->>GW: POST /orders
    GW->>OS: POST /orders
    OS->>OS: Order.Place(...) + outbox row
    OS-->>C: 201 Created
    OS-)MQ: OrderPlaced
    MQ-)RS: OrderPlaced
    MQ-)NS: OrderPlaced
    RS->>RS: RestaurantOrder.Receive (Pending)
    NS->>NS: log "din ordre modtaget"

    Note over C,NS: 2. Restaurant accepterer den nye ordre
    C->>GW: POST /restaurant/orders/{id}/accept
    GW->>RS: POST /orders/{id}/accept
    RS->>RS: RestaurantOrder.Accept + outbox row
    RS-->>C: 200 OK
    RS-)MQ: OrderAccepted
    MQ-)OS: OrderAccepted
    MQ-)NS: OrderAccepted
    OS->>OS: Order.Accept
    NS->>NS: log "preparation started"

    Note over C,NS: 3. Restaurant markerer ordren klar
    C->>GW: POST /restaurant/orders/{id}/ready
    GW->>RS: POST /orders/{id}/ready
    RS->>RS: RestaurantOrder.MarkReady + outbox row
    RS-->>C: 200 OK
    RS-)MQ: OrderReadyForPickup
    MQ-)OS: OrderReadyForPickup
    MQ-)DS: OrderReadyForPickup
    MQ-)NS: OrderReadyForPickup
    OS->>OS: Order.MarkReady
    DS->>DS: Delivery.Offer (Available)
    DS-)MQ: DeliveryOffered
    NS->>NS: log "din mad er klar"

    Note over C,NS: 4. Bud accepterer (First-to-claim race)
    par Bud A
        C->>GW: POST /deliveries/{id}/claim (A)
        GW->>DS: POST /deliveries/{id}/claim
        DS->>DS: ATOMIC UPDATE WHERE courier_id IS NULL
        DS-->>C: 200 OK (vinder)
    and Bud B
        C->>GW: POST /deliveries/{id}/claim (B)
        GW->>DS: POST /deliveries/{id}/claim
        DS->>DS: ATOMIC UPDATE (0 rows)
        DS-->>C: 409 Conflict (taber)
    end
    DS-)MQ: DeliveryAssigned
    DS-)MQ: DeliveryUnavailable (broadcast)
    MQ-)OS: DeliveryAssigned
    MQ-)NS: DeliveryUnavailable
    OS->>OS: Order.MarkPickedUp + outbox row
    NS->>NS: log "broadcast to couriers: no longer available"
    OS-)MQ: OrderPickedUp
    MQ-)NS: OrderPickedUp
    NS->>NS: log "leveringen er på vej"

    Note over C,NS: 5. Bud leverer ordre
    C->>GW: POST /deliveries/{id}/complete
    GW->>DS: POST /deliveries/{id}/complete
    DS->>DS: Delivery.Complete + outbox row
    DS-->>C: 200 OK
    DS-)MQ: DeliveryCompleted
    MQ-)OS: DeliveryCompleted
    OS->>OS: Order.MarkDelivered + outbox row
    OS-)MQ: OrderDelivered
    MQ-)NS: OrderDelivered
    NS->>NS: log "din mad er leveret"
```
