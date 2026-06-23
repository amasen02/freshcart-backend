# Payment service - raw Kubernetes manifests (reference)

This folder contains the **expanded** Kubernetes manifests for the payment service, exactly as
the Helm chart at `deploy/helm/payment/` would render them. They are kept here as a teaching
reference - open them next to the Helm templates to see the substitution.

In production you should deploy via **Helm**, not by applying these directly:

```bash
helm upgrade --install payment deploy/helm/payment \
  --namespace freshcart --create-namespace \
  --values deploy/helm/payment/values-prod.yaml
```

Files:

- `namespace.yaml` - `freshcart` namespace with PodSecurity restricted enforcement.
- `deployment.yaml` - Deployment with non-root container, read-only root fs, probes, resource requests/limits.
- `service.yaml` - ClusterIP Service.
- `ingress.yaml` - NGINX Ingress with TLS via cert-manager.
- `configmap.yaml` - Non-secret configuration.
- `secret.yaml` - Placeholder. In real environments secrets come from Azure Key Vault via CSI.
- `hpa.yaml` - HorizontalPodAutoscaler 2-10 on CPU 65% + memory 75%.
- `pdb.yaml` - PodDisruptionBudget keeps at least one replica during voluntary disruption.
- `serviceaccount.yaml` - ServiceAccount annotated for Azure Workload Identity.
- `networkpolicy.yaml` - Default-deny + explicit allow-list (gateway + nginx-ingress).
- `servicemonitor.yaml` - Prometheus scrape config.
- `secretproviderclass.yaml` - Mounts Key Vault secrets as files.
