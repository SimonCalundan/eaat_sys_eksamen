# Arkitektur og designvalg

Dette dokument beskriver hvordan Eaat hænger sammen og hvorfor jeg har truffet mine design valg. Diagrammerne i `docs/diagrams/` viser de samme ting visuelt.

## Overordnet

Applikationen består af fire microservices, en API gateway, en RabbitMQ broker og en MySQL database. Hver service ejer sin egen del af forretningslogik
samt egen del af databasen. De forskellige services taler aldrig direkte med hinanden via HTTP. Al kommunikation mellem dem foregår asynkront gennem RabbitMQ.

Det eneste eksempel på synkron kommunikation i mit system er fra klienten ind i systemet via API gatewayen. 

De fire services er:
- **OrderService**: Holder styr på ordrernes lifecycle/state. Det er den eneste service der ved hvor ordren er i sit forløb lige fra placed til delivered.
- **RestaurantService**: er restaurant-personalets værktøj. De kan se hvilke ordre der er kommet ind og tage stilling til hvad der skal ske med den.
- **DeliveryService**: denne service håndtere udbuddet af leveringer til bude og selve first-to-claim logikken.
- **NotificationService**: denne service er en ren consumer der lytter på events og skriver dem ud i console. I et rigtigt produktion miljø, kunne man hurtigt tilpasse den til evt. at send beskeder eller mails til kunden.

## Hvorfor event-drevet i stedet for direkte HTTP-kald

En naiv løsning havde været at lade OrderService kalde RestaurantService direkte når en kunde lavede en ny ordre. Men så ville disse services være koblet sammen ved runtime. 
Det vil sige, at hvis RestaurantService var nede, så kunne kunden ikke lave en ordre. Og hvis jeg nu ville have en ny service eller lign til at lytte på OrderPlaced, så skulle jeg koble den nye service på manuelt.

Med events via. RabbitMQ ved publisher ikke hvem der lytter. OrderService kan bare publish OrderPlaced til RabbitMQ i en fanout-exchange, og alle services der har bunden en kø til den exchange får eventet. Det er et klassisk pub/sub mønster.

Jeg bruger fanout exchanges fordi jeg har én exchange per event-type. Det giver et 1:1 forhold mellem event-typen og exchange-navnet (fx `eaat.OrderPlaced`), og det er nemt at se i RabbitMQ's web ui, hvem der lytter på hvad. Man kunne også have brugt topic-exchange med routing keys, men det vurderede jeg var mere komplekst end nødvendigt for mit scope.
Jeg overvejde at bruge routing keys på bud logikken, her kunne man lave routing keys baseret på lokationen af restauranten, så man kunne f.eks. have en routing key for "Aarhus" og en for "Odense". Dog havde jeg ikke lige tiden til det i denne omgang.

## Database per service

Hver service har sin egen logiske database i MySQL. I produktion ville det være bedre med en database instans pr. service, men for at holde opsætningen simpel med docker compose, bruger jeg samme MySQL instans for alle services. Princippet er dog det samme, da ingen services læser/skriver fra/til hinandens tabeller.

Det giver lidt duplikering af data, men det er en bevidst tradeoff. Men til gengæld kan hver service opdatere sit schema uden at det påvirker de andre, og en service kan starte op og fungerer uden at være afhænging af de andre services' databaser.

## Outbox-pattern

Et af de større problemer ved event-drevne systemer er det såkalte dual-write problem. Hvis en service f.eks. både opdaterer sin egen database og publisher et event, har man to systemer (database og message broker) der ikke kan comitte atomisk sammen.
Hvis man committer til databasen først, og så publisher, og noget crasher imens, ender man med en ordre der eksisterer, men intet event nogen kan reagere på.

Løsningen er outbox pattern. I stedet for at publish direkte til RabbitMQ, skriver service-koden eventet til en outbox tabel i samme database transaktion som domæne-ændringen (f.eks. orderplaced).
Når denne transaktion committer, comitter både ordren og intentionen om at publish eventet (outbox entryen) til databasen.

En backgroundworker (OutboxPublisher) poller fra outbox tabellen med et givent interval og publicerer pending rows til RabbitMQ og markerer dem som published bagefter.
Hvis RabbitMQ er nede, fejler worker bare og prøver igen næste poll. Hvis service crasher mellem publish og mark-as-published, får man bare en duplicate besked.
Dette gør ikke noget, da consumer-siden håndterer duplicates.

## Idempotent receivers

Som sagt kan outbox pattern publish den samme besked flere gange, hvis det scenarie jeg beskrev tidligere rammer. Det er derfor mine consumers job at håndtere duplicates.

For at håndtere dette har hver service en processed_messages tabel med et EventId som primary key. Når en besked modtages, tjekker idempotency-checkeren om EventId allerede er processed (det vil sige findes allerede i tabellen). Hvis ja, skipper jeg handleren og acker beskeden. Hvis nej, kører jeg handleren og inserter et nyt row med det eventId i tabellen.

Dette sammen med outbox pattern giver det man kalder exactly-once processing. Det er ikke exactly-once delivery, men effekten er den samme: forretningslogikken kører præcis én gang.

## First-to-claim i DeliveryService

Når en delivery er tilgængelig og to bude prøver at tage imod den samtidig, er det vigtigt at kun én af dem får den. Det er det man kalder en distribueret race condition.
For at løse dette bruger jeg en atomisk SQL UPDATE med en WHERE-condition:

```sql
UPDATE deliveries
SET courier_id = ?, status = 'Assigned', assigned_at = ?
WHERE id = ? AND courier_id IS NULL AND status = 'Available'
```

MySQL holder en row-level lock på rækken når den bliver opdateret. Den anden update venter derfor på låsen indtil den update er færdig.
Når updaten så er færdig, passer where conditionen ikke længere da courier_id ikke er null. Derfor returnerer UPDATE'en 0 rows affected, og det kan jeg bruge til at signalere tilbage til buddet, at et andet bud kom først.

## Saga coordinator

Min OrderService er i systemet saga coordinator i den forstand, at den holder ansvaret for en ordres state gennem hele dens forløb. De andre 
services publisher events om hvad de har gjort, og OrderService consumer dem og opdaterer sin egen state. 

OrderService sørger også for at oversætte mellem services. Når DeliveryService publisher DeliveryCompleted, consumer OrderService det og publisher selv OrderDelivered. 

Dette betyder at NotificationService kun behøver at lytte på Order*-events. Den ved ikke at der findes en DeliveryService eller noget der hedder bude. Jeg kan frit ændre leveringslogikken uden
at ændre NotificationService.

## API Gateway

Jeg har en reverse proxy service som klienten/brugeren taler med. Den exposer port 8080 og router videre til relevante services baseret på path:

- `/orders/*` til OrderService
- `/restaurant/*` til RestaurantService 
- `/deliveries/*` til DeliveryService
 
Pointen med det er at klient/bruger ikke behøver at kende de interne routes/paths til de forskellige services. Hvis jeg f.eks. i fremtiden splittede OrderService op i to services, skal jeg blot opdatere routing-konfigurationen.
