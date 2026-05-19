# Eaat — Madudleveringsplatform

Eksamensprojekt på 4. semester System Integration (datamatiker, Erhvervsakademi Aarhus, maj 2026).

Eaat er en prototype på en event-drevet madudleveringsplatform. En backend der orkestrerer flowet fra kunde lægger ordre, restaurant accepterer, til bud leverer maden.

For yderligere dokumentation om design, systemets struktur, event-flow osv. se [architecture.md](./docs/architecture.md) og de andre filer i /docs.

## Kør projektet

```bash
docker compose up --build
```

Det starter:

| Service | Port              | Type                                                                         |
|---|-------------------|------------------------------------------------------------------------------|
| API Gateway (YARP) | 8080              | Reverse proxy. Kun denne port er beregnet til klienter                       |
| OrderService | 5001              | Saga coordinator og Order state                                              |
| RestaurantService | 5002              | Restaurant-personalets værktøj                                               |
| DeliveryService | 5003              | First-to-claim auction                                                       |
| NotificationService | ingen             | Worker (ingen HTTP). Lytter på Order*-events + DeliveryUnavailable broadcast |
| RabbitMQ | 5672 + 15672 (UI) | Message broker                                                               |
| MySQL | 3306              | Database                                                                     |

Første start tager 1-3 minutter (Docker bygger 5 .NET-images). EF migrations applies automatisk ved app-startup.

## Demo flowet

Når alt er oppe, kør:

```bash
./scripts/demo.sh
```

Scriptet kører hele flowet ende-til-ende:
1. Kunde lægger ordre
2. Restaurant accepterer
3. Restaurant markerer maden klar
4. Bud A og B prøver at claime samtidigt (kun A vinder, der sendes 409 til B)
5. Bud A leverer
6. Final state vises + notifikationer udskrives

Det fremhæver `correlationId` så man kan følge eventet gennem alle services.

```bash
SLEEP=1 ./scripts/demo.sh                       # hurtigere pauser
```

## Manuel test via Yaak/curl

Alternativt kan endpoints kaldes direkte. Faste test-UUIDs:

```
customer    = 11111111-1111-1111-1111-111111111111
restaurant  = 22222222-2222-2222-2222-222222222222
courier_A   = 1aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
courier_B   = 2bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb
```

Eksempler:

```bash
# Lav ordre
curl -X POST http://localhost:8080/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":"11111111-1111-1111-1111-111111111111","restaurantId":"22222222-2222-2222-2222-222222222222","deliveryArea":"Aarhus C"}'

# Restaurant accepterer
curl -X POST http://localhost:8080/restaurant/orders/<ORDER_ID>/accept

# Bud claimer
curl -X POST http://localhost:8080/deliveries/<DELIVERY_ID>/claim \
  -H "Content-Type: application/json" \
  -d '{"courierId":"1aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}'
```

Se [docs/diagrams/event-flow.md](docs/diagrams/event-flow.md) for det fulde flow.

## Projektets struktur

```
Eaat/
├── docker-compose.yml              hele systemet kører herfra
├── infra/
│   └── mysql-init/                 SQL der opretter 4 databaser ved første MySQL-start
├── scripts/
│   └── demo.sh                     ende-til-ende demo script
├── docs/
│   └── diagrams/                   dokumentation og diagrammer
├── src/
│   ├── Eaat.Contracts/             event records + IEvent
│   ├── Eaat.Infra/                 RabbitMQ + Outbox + Idempotency + DI extension
│   ├── Eaat.OrderService/          saga coordinator og Order state
│   ├── Eaat.RestaurantService/     restaurant-personale værktøj
│   ├── Eaat.DeliveryService/       first-to-claim auction
│   ├── Eaat.NotificationService/   pure consumer (logger til console)
│   └── Eaat.ApiGateway/            reverse proxy
└── Eaat.sln
```

## Teknologi-stack

- **.NET 10** — alle services
- **RabbitMQ 3.13** med `RabbitMQ.Client 7.0.0` (fuldt async API)
- **EF Core 8** + **Pomelo MySQL provider**
- **MySQL 8.0** — én instans, fire logiske databaser
- **YARP 2.2** — reverse proxy
- **System.Text.Json** — event-serialisering
