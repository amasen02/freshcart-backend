#!/usr/bin/env python3
"""Generate per-service Helm values.yaml + dev/staging/prod overlays for FreshCart.

The shape mirrors deploy/helm/identity/values.yaml exactly; only the service-specific
config keys, secret keys, datastore egress, resource profile, SignalR affinity and
Key Vault object names differ. Driven by the appsettings.json each service actually reads.
"""
import pathlib

JWT_ISSUER = "https://freshcart.local/identity"
JWT_AUDIENCE = "https://freshcart.local"
SPA_ORIGIN = "https://www.freshcart.local"
OTEL_ENDPOINT = "http://otel-collector.observability.svc.cluster.local:4317"
BROKER_HOST_CLUSTER = "amqp://rabbitmq.freshcart.svc.cluster.local:5672"
DEV_SIGNING_KEY = "replace-from-keyvault-in-real-environment-min-32-chars"

# resource profiles
STD = {
    "requests": {"cpu": "100m", "memory": "256Mi", "ephemeral-storage": "128Mi"},
    "limits": {"cpu": "500m", "memory": "512Mi", "ephemeral-storage": "512Mi"},
}
# connection-oriented services (gateway / SignalR fan-out) get more headroom
CONN = {
    "requests": {"cpu": "150m", "memory": "384Mi", "ephemeral-storage": "128Mi"},
    "limits": {"cpu": "750m", "memory": "768Mi", "ephemeral-storage": "512Mi"},
}

# Per-service definition.
#   image_repo: ACR repository
#   config: extra non-secret config entries (ordered)
#   secrets: literal secret keys with their DEV placeholder values
#   kv: Key Vault object names -> target env key
#   resources: STD or CONN
#   session_affinity: True for SignalR services
#   broker: True if the service talks to RabbitMQ (adds broker password secret + host config)
#   public_host: subdomain stem for the ingress host (None => no public ingress, internal-only)
SERVICES = {
    "catalog": dict(
        image_repo="freshcart-catalog",
        otel_name="freshcart-catalog",
        config=[("MessageBroker__Host", BROKER_HOST_CLUSTER), ("MessageBroker__UserName", "freshcart")],
        secrets=[
            ("ConnectionStrings__catalogdb", "Host=catalogdb;Port=5432;Database=freshcart_catalog;Username=freshcart;Password=replace-from-keyvault"),
            ("ConnectionStrings__cache", "redis.freshcart.svc.cluster.local:6379,password=replace-from-keyvault"),
            ("MessageBroker__Password", "replace-from-keyvault"),
        ],
        kv=[("catalogdb-connection-string", "ConnectionStrings__catalogdb"),
            ("cache-connection-string", "ConnectionStrings__cache"),
            ("messagebroker-password", "MessageBroker__Password")],
        resources=STD, session_affinity=False, broker=True, public_host="catalog",
    ),
    "pricing": dict(
        image_repo="freshcart-pricing",
        otel_name="freshcart-pricing",
        config=[("Pricing__TaxRatePercentage", "8.0"),
                ("Kestrel__EndpointDefaults__Protocols", "Http2")],
        secrets=[],
        kv=[],
        resources=STD, session_affinity=False, broker=False, public_host=None,
    ),
    "basket": dict(
        image_repo="freshcart-basket",
        otel_name="freshcart-basket",
        config=[("Services__Catalog__BaseAddress", "http://catalog:8080"),
                ("Services__Pricing__Address", "http://pricing:8080"),
                ("MessageBroker__Host", BROKER_HOST_CLUSTER), ("MessageBroker__UserName", "freshcart"),
                ("Outbox__BatchSize", "100"), ("Outbox__PollInterval", "00:00:05")],
        secrets=[
            ("ConnectionStrings__basketdb", "Host=basketdb;Port=5432;Database=freshcart_basket;Username=freshcart;Password=replace-from-keyvault"),
            ("ConnectionStrings__cache", "redis.freshcart.svc.cluster.local:6379,password=replace-from-keyvault"),
            ("MessageBroker__Password", "replace-from-keyvault"),
        ],
        kv=[("basketdb-connection-string", "ConnectionStrings__basketdb"),
            ("cache-connection-string", "ConnectionStrings__cache"),
            ("messagebroker-password", "MessageBroker__Password")],
        resources=STD, session_affinity=False, broker=True, public_host="basket",
    ),
    "ordering": dict(
        image_repo="freshcart-ordering",
        otel_name="freshcart-ordering",
        config=[("Services__Inventory__Address", "http://inventory:8080"),
                ("Services__Payment__BaseAddress", "http://payment:8080"),
                ("MessageBroker__Host", BROKER_HOST_CLUSTER), ("MessageBroker__UserName", "freshcart"),
                ("Outbox__BatchSize", "100"), ("Outbox__PollInterval", "00:00:05")],
        secrets=[
            ("ConnectionStrings__orderingdb", "Server=orderingdb;Database=FreshCart.Ordering;User Id=app;Password=replace-from-keyvault;Encrypt=True;TrustServerCertificate=True"),
            ("MessageBroker__Password", "replace-from-keyvault"),
        ],
        kv=[("orderingdb-connection-string", "ConnectionStrings__orderingdb"),
            ("messagebroker-password", "MessageBroker__Password")],
        resources=STD, session_affinity=False, broker=True, public_host="ordering",
    ),
    "inventory": dict(
        image_repo="freshcart-inventory",
        otel_name="freshcart-inventory",
        config=[("Kestrel__EndpointDefaults__Protocols", "Http2"),
                ("MessageBroker__Host", BROKER_HOST_CLUSTER), ("MessageBroker__UserName", "freshcart")],
        secrets=[
            ("ConnectionStrings__inventorydb", "Server=inventorydb;Database=FreshCart.Inventory;User Id=app;Password=replace-from-keyvault;Encrypt=True;TrustServerCertificate=True"),
            ("MessageBroker__Password", "replace-from-keyvault"),
        ],
        kv=[("inventorydb-connection-string", "ConnectionStrings__inventorydb"),
            ("messagebroker-password", "MessageBroker__Password")],
        resources=STD, session_affinity=False, broker=True, public_host="inventory",
    ),
    "payment": dict(
        image_repo="freshcart-payment",
        otel_name="freshcart-payment",
        config=[],
        secrets=[
            ("ConnectionStrings__paymentreaddb", "Server=paymentreaddb;Database=FreshCart.PaymentRead;User Id=app;Password=replace-from-keyvault;Encrypt=True;TrustServerCertificate=True"),
            ("ConnectionStrings__paymentevents", "mongodb://app:replace-from-keyvault@paymentevents:27017/paymentevents"),
        ],
        kv=[("paymentreaddb-connection-string", "ConnectionStrings__paymentreaddb"),
            ("paymentevents-connection-string", "ConnectionStrings__paymentevents")],
        resources=STD, session_affinity=False, broker=False, public_host=None,
    ),
    "delivery": dict(
        image_repo="freshcart-delivery",
        otel_name="freshcart-delivery",
        config=[("MessageBroker__Host", BROKER_HOST_CLUSTER), ("MessageBroker__UserName", "freshcart")],
        secrets=[
            ("ConnectionStrings__deliverydb", "mongodb://app:replace-from-keyvault@deliverydb:27017/deliverydb"),
            ("MessageBroker__Password", "replace-from-keyvault"),
        ],
        kv=[("deliverydb-connection-string", "ConnectionStrings__deliverydb"),
            ("messagebroker-password", "MessageBroker__Password")],
        resources=STD, session_affinity=False, broker=True, public_host="delivery",
    ),
    "notification": dict(
        image_repo="freshcart-notification",
        otel_name="freshcart-notification",
        config=[("MessageBroker__Host", BROKER_HOST_CLUSTER), ("MessageBroker__UserName", "freshcart")],
        secrets=[
            ("ConnectionStrings__notificationsdb", "mongodb://app:replace-from-keyvault@notificationsdb:27017/notificationsdb"),
            ("ConnectionStrings__cache", "redis.freshcart.svc.cluster.local:6379,password=replace-from-keyvault"),
            ("MessageBroker__Password", "replace-from-keyvault"),
        ],
        kv=[("notificationsdb-connection-string", "ConnectionStrings__notificationsdb"),
            ("cache-connection-string", "ConnectionStrings__cache"),
            ("messagebroker-password", "MessageBroker__Password")],
        resources=CONN, session_affinity=True, broker=True, public_host="notification",
    ),
    "customersupport": dict(
        image_repo="freshcart-customersupport",
        otel_name="freshcart-customersupport",
        config=[],
        secrets=[
            ("ConnectionStrings__supportchatdb", "mongodb://app:replace-from-keyvault@supportchatdb:27017/supportchatdb"),
            ("ConnectionStrings__cache", "redis.freshcart.svc.cluster.local:6379,password=replace-from-keyvault"),
        ],
        kv=[("supportchatdb-connection-string", "ConnectionStrings__supportchatdb"),
            ("cache-connection-string", "ConnectionStrings__cache")],
        resources=CONN, session_affinity=True, broker=False, public_host="support",
    ),
    "reviews": dict(
        image_repo="freshcart-reviews",
        otel_name="freshcart-reviews",
        config=[("MessageBroker__Host", BROKER_HOST_CLUSTER), ("MessageBroker__UserName", "freshcart")],
        secrets=[
            ("ConnectionStrings__reviewsdb", "mongodb://app:replace-from-keyvault@reviewsdb:27017/reviewsdb"),
            ("MessageBroker__Password", "replace-from-keyvault"),
        ],
        kv=[("reviewsdb-connection-string", "ConnectionStrings__reviewsdb"),
            ("messagebroker-password", "MessageBroker__Password")],
        resources=STD, session_affinity=False, broker=True, public_host="reviews",
    ),
    "gateway": dict(
        image_repo="freshcart-gateway",
        otel_name="freshcart-gateway",
        config=[("Cors__AllowedOrigins__0", SPA_ORIGIN)],
        secrets=[
            ("ConnectionStrings__cache", "redis.freshcart.svc.cluster.local:6379,password=replace-from-keyvault"),
            ("Jwt__SigningKey", DEV_SIGNING_KEY),
        ],
        kv=[("cache-connection-string", "ConnectionStrings__cache"),
            ("jwt-signing-key", "Jwt__SigningKey")],
        resources=CONN, session_affinity=False, broker=False, public_host="www",
    ),
}

# every service validates the shared JWT, so the signing key is always a secret + KV object
for name, d in SERVICES.items():
    if name != "gateway":
        d["secrets"].append(("Jwt__SigningKey", DEV_SIGNING_KEY))
        d["kv"].append(("jwt-signing-key", "Jwt__SigningKey"))


def q(v):
    return "'" + str(v) + "'"


def res_block(res, indent):
    pad = " " * indent
    lines = []
    lines.append(f"{pad}requests:")
    for k, v in res["requests"].items():
        lines.append(f"{pad}  {k}: {q(v)}")
    lines.append(f"{pad}limits:")
    for k, v in res["limits"].items():
        lines.append(f"{pad}  {k}: {q(v)}")
    return "\n".join(lines)


def base_values(name, d):
    has_ingress = d["public_host"] is not None
    host = f"{d['public_host']}.freshcart.local" if has_ingress else None
    lines = []
    lines.append("# ---------------------------------------------------------------------------")
    lines.append(f"# Default values for the {name} Helm chart.")
    lines.append("# Per-environment overrides live in values-dev.yaml / values-staging.yaml /")
    lines.append("# values-prod.yaml; keep this file as the most restrictive baseline.")
    lines.append("# ---------------------------------------------------------------------------")
    lines.append("")
    lines.append("nameOverride: ''")
    lines.append("fullnameOverride: ''")
    lines.append("")
    lines.append("image:")
    lines.append(f"  repository: freshcartacr.azurecr.io/freshcart/{name}")
    lines.append("  tag: latest                                # overridden by CI to commit SHA")
    lines.append("  pullPolicy: IfNotPresent")
    lines.append("")
    lines.append("imagePullSecrets:")
    lines.append("  - name: freshcart-acr-pull")
    lines.append("")
    lines.append("replicaCount: 2")
    lines.append("")
    lines.append("revisionHistoryLimit: 5")
    lines.append("")
    lines.append("serviceAccount:")
    lines.append("  create: true")
    lines.append(f"  name: {name}")
    lines.append("  # Workload Identity: bind the federated identity client id at install time.")
    lines.append("  azureWorkloadIdentityClientId: ''")
    lines.append("")
    lines.append("podLabels:")
    lines.append("  app.kubernetes.io/component: backend")
    lines.append("  app.kubernetes.io/part-of: freshcart")
    lines.append("")
    lines.append("podAnnotations:")
    lines.append("  checksum/config: ''")
    lines.append("")
    lines.append("podSecurityContext:")
    lines.append("  fsGroup: 1000")
    lines.append("  runAsNonRoot: true")
    lines.append("  runAsUser: 1000")
    lines.append("  seccompProfile:")
    lines.append("    type: RuntimeDefault")
    lines.append("")
    lines.append("securityContext:")
    lines.append("  allowPrivilegeEscalation: false")
    lines.append("  readOnlyRootFilesystem: true")
    lines.append("  runAsNonRoot: true")
    lines.append("  runAsUser: 1000")
    lines.append("  capabilities:")
    lines.append("    drop:")
    lines.append("      - ALL")
    lines.append("")
    lines.append("service:")
    lines.append("  type: ClusterIP")
    lines.append("  port: 80")
    lines.append("  targetPort: 8080")
    if name in ("pricing", "inventory"):
        lines.append("  # gRPC over HTTP/2 between services; the named appProtocol keeps the mesh h2c-aware.")
        lines.append("  appProtocol: grpc")
    else:
        lines.append("  appProtocol: http")
    if d["session_affinity"]:
        lines.append("  # SignalR connections must stick to one replica across the WebSocket lifetime.")
        lines.append("  sessionAffinity: ClientIP")
    lines.append("")
    lines.append("ingress:")
    if has_ingress:
        lines.append("  enabled: true")
        lines.append("  className: nginx")
        lines.append("  annotations:")
        lines.append("    nginx.ingress.kubernetes.io/proxy-body-size: '8m'")
        lines.append("    nginx.ingress.kubernetes.io/proxy-read-timeout: '60'")
        if d["session_affinity"]:
            lines.append("    # WebSocket upgrade for the SignalR hub needs a long read timeout.")
            lines.append("    nginx.ingress.kubernetes.io/proxy-read-timeout: '3600'")
            lines.append("    nginx.ingress.kubernetes.io/proxy-send-timeout: '3600'")
        lines.append("    cert-manager.io/cluster-issuer: letsencrypt-prod")
        lines.append("  hosts:")
        lines.append(f"    - host: {host}")
        lines.append("      paths:")
        lines.append("        - path: /")
        lines.append("          pathType: Prefix")
        lines.append("  tls:")
        lines.append(f"    - secretName: {name}-tls")
        lines.append("      hosts:")
        lines.append(f"        - {host}")
    else:
        lines.append("  # Internal-only service; reached over gRPC/REST from inside the cluster, never the edge.")
        lines.append("  enabled: false")
        lines.append("  className: nginx")
        lines.append("  annotations: {}")
        lines.append("  hosts: []")
        lines.append("  tls: []")
    lines.append("")
    lines.append("resources:")
    lines.append(res_block(d["resources"], 2))
    lines.append("")
    lines.append("autoscaling:")
    lines.append("  enabled: true")
    lines.append("  minReplicas: 2")
    lines.append("  maxReplicas: 10")
    lines.append("  targetCPUUtilizationPercentage: 65")
    lines.append("  targetMemoryUtilizationPercentage: 75")
    lines.append("")
    lines.append("podDisruptionBudget:")
    lines.append("  enabled: true")
    lines.append("  minAvailable: 1")
    lines.append("")
    lines.append("# Health probes hit the endpoints surfaced by AddFreshCartServiceDefaults().")
    lines.append("# Only /alive and /ready exist in Production (/health is Development/Staging-only),")
    lines.append("# so the startup probe targets /ready and liveness takes over once dependencies pass.")
    lines.append("probes:")
    lines.append("  liveness:")
    lines.append("    path: /alive")
    lines.append("    initialDelaySeconds: 15")
    lines.append("    periodSeconds: 20")
    lines.append("    failureThreshold: 3")
    lines.append("  readiness:")
    lines.append("    path: /ready")
    lines.append("    initialDelaySeconds: 5")
    lines.append("    periodSeconds: 10")
    lines.append("    failureThreshold: 3")
    lines.append("  startup:")
    lines.append("    path: /ready")
    lines.append("    initialDelaySeconds: 5")
    lines.append("    periodSeconds: 5")
    lines.append("    failureThreshold: 30")
    lines.append("")
    lines.append("# Non-secret configuration lands in the ConfigMap.")
    lines.append("config:")
    lines.append("  ASPNETCORE_ENVIRONMENT: Production")
    lines.append("  ASPNETCORE_HTTP_PORTS: '8080'")
    lines.append(f"  OTEL_EXPORTER_OTLP_ENDPOINT: '{OTEL_ENDPOINT}'")
    lines.append(f"  OTEL_SERVICE_NAME: '{d['otel_name']}'")
    lines.append(f"  Jwt__Issuer: '{JWT_ISSUER}'")
    lines.append(f"  Jwt__Audience: '{JWT_AUDIENCE}'")
    for k, v in d["config"]:
        lines.append(f"  {k}: '{v}'")
    lines.append("")
    lines.append("# Sensitive values; pulled from Azure Key Vault in real environments. The literals")
    lines.append("# below are placeholders only and are overridden by the CSI mount when keyVault is on.")
    lines.append("secrets:")
    lines.append("  externalSecretName: ''")
    lines.append("  literal:")
    if d["secrets"]:
        for k, v in d["secrets"]:
            lines.append(f"    {k}: ''")
    else:
        lines.append("    {}")
    lines.append("")
    lines.append("# Azure Key Vault CSI driver wiring. Set keyVault.name in env values to enable.")
    lines.append("keyVault:")
    lines.append("  enabled: false")
    lines.append("  name: ''")
    lines.append("  tenantId: ''")
    if d["kv"]:
        lines.append("  secrets:")
        for obj, _key in d["kv"]:
            lines.append(f"    - {obj}")
    else:
        lines.append("  secrets: []")
    lines.append("")
    lines.append("serviceMonitor:")
    lines.append("  enabled: false")
    lines.append("  interval: 30s")
    lines.append("  scrapeTimeout: 10s")
    lines.append("  labels: {}")
    lines.append("")
    lines.append("# NetworkPolicy: default-deny + explicit allow-list.")
    lines.append("networkPolicy:")
    lines.append("  enabled: true")
    lines.append("  ingressFrom:")
    if name == "gateway":
        lines.append("    - namespaceSelector:")
        lines.append("        matchLabels: { name: ingress-nginx }")
    else:
        lines.append("    - namespaceSelector:")
        lines.append("        matchLabels: { name: freshcart }")
        lines.append("      podSelector:")
        lines.append("        matchLabels: { app.kubernetes.io/name: gateway }")
        if name in ("inventory", "payment"):
            lines.append("    # Ordering calls this service directly (gRPC/REST) during the checkout saga.")
            lines.append("    - namespaceSelector:")
            lines.append("        matchLabels: { name: freshcart }")
            lines.append("      podSelector:")
            lines.append("        matchLabels: { app.kubernetes.io/name: ordering }")
        if name in ("catalog", "pricing"):
            lines.append("    # Basket fans out to this service while pricing a basket.")
            lines.append("    - namespaceSelector:")
            lines.append("        matchLabels: { name: freshcart }")
            lines.append("      podSelector:")
            lines.append("        matchLabels: { app.kubernetes.io/name: basket }")
    lines.append("  egressTo:")
    lines.append("    - cidr: 10.0.0.0/8        # cluster pods + Azure private endpoints (db, cache, broker)")
    lines.append("    - cidr: 168.63.129.16/32  # Azure DNS")
    lines.append("")
    lines.append("nodeSelector: {}")
    lines.append("tolerations: []")
    lines.append("affinity:")
    lines.append("  podAntiAffinity:")
    lines.append("    preferredDuringSchedulingIgnoredDuringExecution:")
    lines.append("      - weight: 100")
    lines.append("        podAffinityTerm:")
    lines.append("          topologyKey: kubernetes.io/hostname")
    lines.append("          labelSelector:")
    lines.append("            matchLabels:")
    lines.append(f"              app.kubernetes.io/name: {name}")
    return "\n".join(lines) + "\n"


def overlay(name, d, env):
    has_ingress = d["public_host"] is not None
    lines = []
    if env == "dev":
        lines.append("replicaCount: 1")
        lines.append("")
        lines.append("image:")
        lines.append("  pullPolicy: Always")
        lines.append("")
        if has_ingress:
            host = f"{d['public_host']}.dev.freshcart.local"
            lines.append("ingress:")
            lines.append("  hosts:")
            lines.append(f"    - host: {host}")
            lines.append("      paths:")
            lines.append("        - path: /")
            lines.append("          pathType: Prefix")
            lines.append("  tls:")
            lines.append(f"    - secretName: {name}-tls-dev")
            lines.append(f"      hosts: [{host}]")
            lines.append("")
        lines.append("resources:")
        lines.append("  requests: { cpu: '50m', memory: '128Mi' }")
        lines.append("  limits:   { cpu: '300m', memory: '384Mi' }")
        lines.append("")
        lines.append("autoscaling:")
        lines.append("  enabled: false")
        lines.append("")
        lines.append("podDisruptionBudget:")
        lines.append("  enabled: false")
        lines.append("")
        lines.append("config:")
        lines.append("  ASPNETCORE_ENVIRONMENT: Development")
        lines.append("")
        if d["secrets"]:
            lines.append("secrets:")
            lines.append("  literal:")
            for k, v in d["secrets"]:
                lines.append(f"    {k}: '{v}'")
            lines.append("")
        lines.append("serviceMonitor:")
        lines.append("  enabled: true")
    elif env == "staging":
        lines.append("replicaCount: 2")
        lines.append("")
        lines.append("image:")
        lines.append("  pullPolicy: IfNotPresent")
        lines.append("")
        if has_ingress:
            host = f"{d['public_host']}.staging.freshcart.local"
            lines.append("ingress:")
            lines.append("  hosts:")
            lines.append(f"    - host: {host}")
            lines.append("      paths:")
            lines.append("        - path: /")
            lines.append("          pathType: Prefix")
            lines.append("  tls:")
            lines.append(f"    - secretName: {name}-tls-staging")
            lines.append(f"      hosts: [{host}]")
            lines.append("")
        rq = "{ cpu: '150m', memory: '384Mi' }" if d["resources"] is CONN else "{ cpu: '100m', memory: '256Mi' }"
        lm = "{ cpu: '750m', memory: '768Mi' }" if d["resources"] is CONN else "{ cpu: '500m', memory: '512Mi' }"
        lines.append("resources:")
        lines.append(f"  requests: {rq}")
        lines.append(f"  limits:   {lm}")
        lines.append("")
        lines.append("autoscaling:")
        lines.append("  minReplicas: 2")
        lines.append("  maxReplicas: 6")
        lines.append("")
        lines.append("config:")
        lines.append("  ASPNETCORE_ENVIRONMENT: Staging")
        lines.append("")
        lines.append("keyVault:")
        lines.append("  enabled: true")
        lines.append("  name: freshcart-staging-kv")
        lines.append("  tenantId: ''           # set per-tenant in CI / az cli")
        lines.append("")
        lines.append("serviceMonitor:")
        lines.append("  enabled: true")
    else:  # prod
        lines.append("replicaCount: 3")
        lines.append("")
        lines.append("image:")
        lines.append("  pullPolicy: IfNotPresent")
        lines.append("")
        if has_ingress:
            stem = "www" if name == "gateway" else d["public_host"]
            host = f"{stem}.freshcart.com"
            lines.append("ingress:")
            lines.append("  hosts:")
            lines.append(f"    - host: {host}")
            lines.append("      paths:")
            lines.append("        - path: /")
            lines.append("          pathType: Prefix")
            lines.append("  tls:")
            lines.append(f"    - secretName: {name}-tls-prod")
            lines.append(f"      hosts: [{host}]")
            lines.append("")
        rq = "{ cpu: '300m', memory: '768Mi' }" if d["resources"] is CONN else "{ cpu: '250m', memory: '512Mi' }"
        lm = "{ cpu: '1500m', memory: '1536Mi' }" if d["resources"] is CONN else "{ cpu: '1000m', memory: '1Gi' }"
        lines.append("resources:")
        lines.append(f"  requests: {rq}")
        lines.append(f"  limits:   {lm}")
        lines.append("")
        lines.append("autoscaling:")
        lines.append("  minReplicas: 3")
        lines.append("  maxReplicas: 15")
        lines.append("  targetCPUUtilizationPercentage: 60")
        lines.append("")
        lines.append("podDisruptionBudget:")
        lines.append("  enabled: true")
        lines.append("  minAvailable: 2")
        lines.append("")
        lines.append("keyVault:")
        lines.append("  enabled: true")
        lines.append("  name: freshcart-prod-kv")
        lines.append("  tenantId: ''")
        lines.append("")
        lines.append("serviceMonitor:")
        lines.append("  enabled: true")
        lines.append("")
        lines.append("# Force at-most-one-pod-per-node on prod for resilience.")
        lines.append("affinity:")
        lines.append("  podAntiAffinity:")
        lines.append("    requiredDuringSchedulingIgnoredDuringExecution:")
        lines.append("      - topologyKey: kubernetes.io/hostname")
        lines.append("        labelSelector:")
        lines.append("          matchLabels:")
        lines.append(f"            app.kubernetes.io/name: {name}")
    return "\n".join(lines) + "\n"


root = pathlib.Path("deploy/helm")
for name, d in SERVICES.items():
    base = root / name
    (base / "values.yaml").write_text(base_values(name, d), encoding="utf-8")
    (base / "values-dev.yaml").write_text(overlay(name, d, "dev"), encoding="utf-8")
    (base / "values-staging.yaml").write_text(overlay(name, d, "staging"), encoding="utf-8")
    (base / "values-prod.yaml").write_text(overlay(name, d, "prod"), encoding="utf-8")
    print("values written:", name)
