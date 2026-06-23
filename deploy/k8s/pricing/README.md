# Pricing service - raw Kubernetes manifests (reference)

This folder contains the **expanded** Kubernetes manifests for the pricing service, exactly as
the Helm chart at `deploy/helm/pricing/` would render them. They are kept here as a teaching
reference - open them next to the Helm templates to see the substitution.

In production you should deploy via **Helm**, not by applying these directly:

```bash
helm upgrade --install pricing deploy/helm/pricing \
  --namespace freshcart --create-namespace \
  --values deploy/helm/pricing/values-prod.yaml
```

Files:

- `namespace.yaml` - `freshcart` namespace with PodSecurity restricted enforcement.
- `deployment.yaml` - Deployment with non-root container, read-only root fs, probes, resource requests/limits.
- `service.yaml` - ClusterIP Service.
- `ingress.yaml` - NGINX Ingress with TLS via cert-manager.
- `configmap.yaml` - Non-secret configuration, including the embedded SQLite data source.
- `secret.yaml` - Empty placeholder. Pricing has no network secrets; its only datastore is the embedded SQLite file in the ConfigMap.
- `hpa.yaml` - HorizontalPodAutoscaler 2-10 on CPU 65% + memory 75%.
- `pdb.yaml` - PodDisruptionBudget keeps at least one replica during voluntary disruption.
- `serviceaccount.yaml` - ServiceAccount annotated for Azure Workload Identity.
- `networkpolicy.yaml` - Default-deny + explicit allow-list (gateway + nginx-ingress).
- `servicemonitor.yaml` - Prometheus scrape config.

There is no `secretproviderclass.yaml`: pricing pulls nothing from Azure Key Vault.
