#!/usr/bin/env bash
#
# Eaat — full flow demo
#
# Kører det komplette flow ende-til-ende og fremhæver
# correlation IDs så man kan følge ordren på tværs af services.
#
# Krav: docker compose up skal være startet (alle services healthy)
#
# Brug:
#   ./scripts/demo.sh                        # default: 3s pauser
#   SLEEP=1 ./scripts/demo.sh                # hurtigere
#   GATEWAY=http://localhost:8080 ./scripts/demo.sh
#

set -euo pipefail

# --- Configuration ---
GATEWAY="${GATEWAY:-http://localhost:8080}"
SLEEP="${SLEEP:-3}"

# Faste test-UUIDs
CUSTOMER_ID="11111111-1111-1111-1111-111111111111"
RESTAURANT_ID="22222222-2222-2222-2222-222222222222"
COURIER_A="1aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
COURIER_B="2bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"

# --- Colors ---
CYAN='\033[36m'
GREEN='\033[32m'
YELLOW='\033[33m'
RED='\033[31m'
BOLD='\033[1m'
DIM='\033[2m'
RESET='\033[0m'

# --- Helpers ---
section() {
    echo ""
    echo -e "${CYAN}${BOLD}════════════════════════════════════════════════════════════════${RESET}"
    echo -e "${CYAN}${BOLD}  $1${RESET}"
    echo -e "${CYAN}${BOLD}════════════════════════════════════════════════════════════════${RESET}"
}

step() {
    echo ""
    echo -e "${BOLD}>>> $1${RESET}"
}

info() {
    echo -e "${DIM}    $1${RESET}"
}

success() {
    echo -e "${GREEN}    ✓ $1${RESET}"
}

warning() {
    echo -e "${YELLOW}    ! $1${RESET}"
}

error() {
    echo -e "${RED}    ✗ $1${RESET}"
}

highlight_correlation() {
    echo ""
    echo -e "${YELLOW}${BOLD}    CORRELATION ID: $1${RESET}"
    echo -e "${DIM}    (dette ID følger ordren gennem alle services og events)${RESET}"
    echo ""
}

pause() {
    sleep "$SLEEP"
}

require_jq() {
    if ! command -v jq &> /dev/null; then
        error "jq er ikke installeret. brew install jq"
        exit 1
    fi
}

# --- Health check ---
section "Health check"

require_jq

step "Tjekker at gateway svarer på $GATEWAY"
if ! curl -sf -o /dev/null "$GATEWAY/"; then
    error "Gateway svarer ikke. Har du kørt 'docker compose up'?"
    exit 1
fi
success "Gateway er oppe"

# --- 1. Place order ---
section "1. Kunde lægger ordre"

step "POST /orders"
info "Body: customerId=$CUSTOMER_ID, restaurantId=$RESTAURANT_ID, deliveryArea='Aarhus C'"

ORDER_RESPONSE=$(curl -s -X POST "$GATEWAY/orders" \
    -H "Content-Type: application/json" \
    -d "{
        \"customerId\": \"$CUSTOMER_ID\",
        \"restaurantId\": \"$RESTAURANT_ID\",
        \"deliveryArea\": \"Aarhus C\"
    }")

ORDER_ID=$(echo "$ORDER_RESPONSE" | jq -r '.id')
ORDER_STATUS=$(echo "$ORDER_RESPONSE" | jq -r '.status')

success "Order skabt, status: $ORDER_STATUS"
highlight_correlation "$ORDER_ID"

info "OrderService committede atomisk: Order + outbox-row med OrderPlaced-event"
info "OutboxPublisher publicerer eventet inden for ${SLEEP}s"
pause

# --- 2. Verify RestaurantService received OrderPlaced ---
section "2. Verifikation: RestaurantService modtog OrderPlaced"

step "GET /restaurant/orders/$ORDER_ID"
RESTAURANT_VIEW=$(curl -s "$GATEWAY/restaurant/orders/$ORDER_ID")
RESTAURANT_STATUS=$(echo "$RESTAURANT_VIEW" | jq -r '.status')

if [ "$RESTAURANT_STATUS" = "Pending" ]; then
    success "RestaurantService har lokal kopi i status: $RESTAURANT_STATUS"
    info "OrderPlaced-event blev konsumeret, RestaurantOrder.Receive() blev kaldt"
else
    warning "Status er '$RESTAURANT_STATUS' (forventede 'Pending'). Måske outbox-poll ikke nået endnu?"
fi
pause

# --- 3. Restaurant accepts ---
section "3. Restaurant accepterer ordren"

step "POST /restaurant/orders/$ORDER_ID/accept"
ACCEPT_RESPONSE=$(curl -s -X POST "$GATEWAY/restaurant/orders/$ORDER_ID/accept")
ACCEPTED_AT=$(echo "$ACCEPT_RESPONSE" | jq -r '.acceptedAt')
success "Restaurant accepterede, acceptedAt: $ACCEPTED_AT"
info "OrderAccepted publiceres via outbox"
pause

step "GET /orders/$ORDER_ID — er order service updated?"
ORDER_VIEW=$(curl -s "$GATEWAY/orders/$ORDER_ID")
STATUS=$(echo "$ORDER_VIEW" | jq -r '.status')
if [ "$STATUS" = "Accepted" ]; then
    success "OrderService.Order: $STATUS (OrderAccepted event blev konsumeret)"
else
    warning "Status: $STATUS (måske outbox-poll ikke nået endnu)"
fi
pause

# --- 4. Restaurant marks ready ---
section "4. Restaurant markerer maden klar"

step "POST /restaurant/orders/$ORDER_ID/ready"
READY_RESPONSE=$(curl -s -X POST "$GATEWAY/restaurant/orders/$ORDER_ID/ready")
success "Maden er klar, ReadyAt: $(echo "$READY_RESPONSE" | jq -r '.readyAt')"
info "OrderReadyForPickup publiceres → OrderService + DeliveryService modtager"
pause

step "GET /deliveries — har DeliveryService skabt en delivery?"
DELIVERIES=$(curl -s "$GATEWAY/deliveries")
DELIVERY_ID=$(echo "$DELIVERIES" | jq -r --arg oid "$ORDER_ID" '.[] | select(.orderId==$oid) | .id')

if [ -z "$DELIVERY_ID" ] || [ "$DELIVERY_ID" = "null" ]; then
    warning "Ingen delivery fundet endnu. Outbox-poll måske ikke nået?"
    info "Venter ekstra ${SLEEP}s..."
    pause
    DELIVERIES=$(curl -s "$GATEWAY/deliveries")
    DELIVERY_ID=$(echo "$DELIVERIES" | jq -r --arg oid "$ORDER_ID" '.[] | select(.orderId==$oid) | .id')
fi

if [ -n "$DELIVERY_ID" ] && [ "$DELIVERY_ID" != "null" ]; then
    DELIVERY_STATUS=$(echo "$DELIVERIES" | jq -r --arg oid "$ORDER_ID" '.[] | select(.orderId==$oid) | .status')
    success "Delivery $DELIVERY_ID skabt, status: $DELIVERY_STATUS"
else
    error "Kunne ikke finde delivery — flow er broken"
    exit 1
fi
pause

# --- 5. First-to-claim race ---
section "5. First-to-claim race"

step "Bud A og B prøver at claime samme delivery samtidigt"
info "Atomic UPDATE WHERE courier_id IS NULL — kun én vinder"
echo ""

# Bud A claimer
info "Bud A: POST /deliveries/$DELIVERY_ID/claim"
CLAIM_A_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$GATEWAY/deliveries/$DELIVERY_ID/claim" \
    -H "Content-Type: application/json" \
    -d "{\"courierId\": \"$COURIER_A\"}")
CLAIM_A_CODE=$(echo "$CLAIM_A_RESPONSE" | tail -1)
CLAIM_A_BODY=$(echo "$CLAIM_A_RESPONSE" | sed '$d')

if [ "$CLAIM_A_CODE" = "200" ]; then
    success "Bud A: $CLAIM_A_CODE OK — vandt claim"
    info "Status: $(echo "$CLAIM_A_BODY" | jq -r '.status'), CourierId: $(echo "$CLAIM_A_BODY" | jq -r '.courierId')"
else
    error "Bud A fik uventet status $CLAIM_A_CODE"
fi

echo ""

# Bud B claimer (taber)
info "Bud B: POST /deliveries/$DELIVERY_ID/claim"
CLAIM_B_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$GATEWAY/deliveries/$DELIVERY_ID/claim" \
    -H "Content-Type: application/json" \
    -d "{\"courierId\": \"$COURIER_B\"}")
CLAIM_B_CODE=$(echo "$CLAIM_B_RESPONSE" | tail -1)
CLAIM_B_BODY=$(echo "$CLAIM_B_RESPONSE" | sed '$d')

if [ "$CLAIM_B_CODE" = "409" ]; then
    success "Bud B: $CLAIM_B_CODE Conflict — tabte first-to-claim (forventet)"
    info "Body: $CLAIM_B_BODY"
else
    error "Bud B fik uventet status $CLAIM_B_CODE (forventede 409)"
fi
pause

step "DeliveryUnavailable broadcast publiceres til alle interesserede"
info "Opgavekrav: 'resten af budene skal have besked om at opgaven ikke længere er tilgængelig'"
info "NotificationService logger broadcastet — i produktion ville en Courier-app lytte"
pause

step "OrderService modtager DeliveryAssigned via fanout"
ORDER_VIEW=$(curl -s "$GATEWAY/orders/$ORDER_ID")
STATUS=$(echo "$ORDER_VIEW" | jq -r '.status')
if [ "$STATUS" = "PickedUp" ]; then
    success "OrderService.Order: $STATUS"
    info "OrderService publicerede OrderPickedUp business-event til NotificationService"
else
    warning "Status: $STATUS (outbox poll måske ikke nået)"
fi
pause

# --- 6. Complete delivery ---
section "6. Bud leverer"

step "POST /deliveries/$DELIVERY_ID/complete"
COMPLETE_RESPONSE=$(curl -s -X POST "$GATEWAY/deliveries/$DELIVERY_ID/complete" \
    -H "Content-Type: application/json" \
    -d "{\"courierId\": \"$COURIER_A\"}")
success "Delivery completed: $(echo "$COMPLETE_RESPONSE" | jq -r '.status')"
info "DeliveryCompleted → OrderService → OrderDelivered → NotificationService"
pause

step "GET /orders/$ORDER_ID — final state"
FINAL=$(curl -s "$GATEWAY/orders/$ORDER_ID")
FINAL_STATUS=$(echo "$FINAL" | jq -r '.status')
DELIVERED_AT=$(echo "$FINAL" | jq -r '.deliveredAt')
if [ "$FINAL_STATUS" = "Delivered" ]; then
    success "FINAL STATUS: $FINAL_STATUS"
    success "DeliveredAt: $DELIVERED_AT"
else
    warning "Final status: $FINAL_STATUS (forventede Delivered)"
fi
pause

# --- 7. Show notifications ---
section "7. NotificationService logs filtreret på correlation"

step "docker logs eaat-notification-service | grep $ORDER_ID"
echo ""
if docker logs eaat-notification-service 2>/dev/null | grep "$ORDER_ID" | tail -20; then
    echo ""
    success "Alle notifikationer for correlation $ORDER_ID logget korrekt"
else
    warning "Kunne ikke læse docker logs (kører du i Docker miljø?)"
fi

# --- Summary ---
section "Flow complete"

echo ""
echo -e "${BOLD}Order ID / Correlation ID:${RESET} $ORDER_ID"
echo -e "${BOLD}Delivery ID:${RESET} $DELIVERY_ID"
echo ""
echo -e "${DIM}For at se hele Order-historikken: curl $GATEWAY/orders/$ORDER_ID | jq${RESET}"
echo ""
