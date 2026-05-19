# Eaat — System Arkitektur

Overordnet diagram over selve systemets arkitektur. 

**OBS**: Nedunder ses udelukkende mermaid kode for de forskellige diagrammer. De kan loades i al mermaid software som f.eks. [mermaid.live](https://mermaid-js.github.io/mermaid-live-editor/).
Ellers findes diagrammerne også i images mappen i samme diagrams folder.

```mermaid
    graph TB
    Client[Klient]

    subgraph Gateway["API Gateway"]
        ApiGateway[Eaat.ApiGateway<br/>YARP reverse proxy på port<br/>:8080]
    end

    subgraph Services["Microservices"]
        OrderService[Eaat.OrderService<br/>:5001]
        RestaurantService[Eaat.RestaurantService<br/>:5002]
        DeliveryService[Eaat.DeliveryService<br/>:5003]
        NotificationService[Eaat.NotificationService<br/>Worker / ingen HTTP]
    end

    subgraph Infrastructure["Infrastruktur"]
        RabbitMQ[(RabbitMQ<br/>fanout exchanges<br/>:5672)]
        MySQL[(MySQL<br/>4 databaser<br/>:3306)]
    end

    Client -->|HTTP request| ApiGateway
    ApiGateway -->|/orders/*| OrderService
    ApiGateway -->|/restaurant/*| RestaurantService
    ApiGateway -->|/deliveries/*| DeliveryService

    OrderService <-->|pub/sub| RabbitMQ
    RestaurantService <-->|pub/sub| RabbitMQ
    DeliveryService <-->|pub/sub| RabbitMQ
    NotificationService -->|subscribe only| RabbitMQ

    OrderService -.->|eaat_orders| MySQL
    RestaurantService -.->|eaat_restaurants| MySQL
    DeliveryService -.->|eaat_deliveries| MySQL
    NotificationService -.->|eaat_notifications| MySQL

    classDef service fill:#bfdbfe,stroke:#1e3a8a,color:#000
    classDef infra fill:#fef3c7,stroke:#92400e,color:#000
    classDef gateway fill:#d1fae5,stroke:#065f46,color:#000
    classDef client fill:#e5e7eb,stroke:#374151,color:#000

    class OrderService,RestaurantService,DeliveryService,NotificationService service
    class RabbitMQ,MySQL infra
    class ApiGateway gateway
    class Client client
```