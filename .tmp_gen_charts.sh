#!/usr/bin/env bash
set -euo pipefail

chart() {
  local svc="$1" desc="$2" cat="$3"; shift 3
  local kw="$*"
  local kwlines=""
  for k in $kw; do kwlines+="  - ${k}"$'\n'; done
  cat > "deploy/helm/${svc}/Chart.yaml" <<EOF
apiVersion: v2
name: ${svc}
description: ${desc}
type: application

# Bumped on every chart change (template, defaults). NOT the app version.
version: 0.1.0

appVersion: '0.1.0'

home: https://github.com/amasen02/FreshCart
sources:
  - https://github.com/amasen02/FreshCart

maintainers:
  - name: Ama Senevirathne
    email: amabandarasp@gmail.com

keywords:
  - freshcart
${kwlines%$'\n'}
  - dotnet10
  - aks

annotations:
  category: ${cat}
  licenses: MIT
EOF
}

chart catalog "FreshCart Catalog service — vertical-slice product catalog on Marten and Postgres." Application catalog products marten postgres
chart pricing "FreshCart Pricing service — stateless gRPC price and coupon calculator." Application pricing grpc calculator
chart basket "FreshCart Basket service — per-user basket on Redis with a transactional outbox." Application basket redis outbox
chart ordering "FreshCart Ordering service — DDD aggregate with a MassTransit checkout saga." Application ordering ddd saga masstransit
chart inventory "FreshCart Inventory service — stock reservation over REST and gRPC." Application inventory stock grpc dapper
chart payment "FreshCart Payment service — event-sourced payment capture with a Mongo event store." Application payment event-sourcing mongodb compliance
chart delivery "FreshCart Delivery service — hexagonal delivery scheduling on MongoDB." Application delivery hexagonal mongodb logistics
chart notification "FreshCart Notification service — RabbitMQ fan-out and a SignalR notification hub." Application notification signalr rabbitmq realtime
chart customersupport "FreshCart Customer Support service — SignalR live chat with agent assignment." Application customer-support signalr chat realtime
chart reviews "FreshCart Reviews service — vertical-slice product reviews on MongoDB." Application reviews mongodb moderation
chart gateway "FreshCart API Gateway — YARP edge with the cookie-to-JWT BFF trust boundary." Networking gateway yarp bff edge
echo "charts generated"
