#!/usr/bin/env bash
set -euo pipefail

gen() {
  local svc="$1"
  local dir="deploy/helm/${svc}/templates"
  mkdir -p "$dir"

  cat > "$dir/_helpers.tpl" <<EOF
{{/* vim: set filetype=mustache: */}}
{{/*
Expand the name of the chart.
*/}}
{{- define "${svc}.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "${svc}.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- \$name := default .Chart.Name .Values.nameOverride -}}
{{- if contains \$name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name \$name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}
{{- end }}

{{/*
Chart label.
*/}}
{{- define "${svc}.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end }}

{{/*
Common labels.
*/}}
{{- define "${svc}.labels" -}}
helm.sh/chart: {{ include "${svc}.chart" . }}
{{ include "${svc}.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- with .Values.podLabels }}
{{ toYaml . }}
{{- end }}
{{- end }}

{{/*
Selector labels.
*/}}
{{- define "${svc}.selectorLabels" -}}
app.kubernetes.io/name: {{ include "${svc}.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
ServiceAccount name.
*/}}
{{- define "${svc}.serviceAccountName" -}}
{{- if .Values.serviceAccount.create -}}
{{- default (include "${svc}.fullname" .) .Values.serviceAccount.name -}}
{{- else -}}
{{- default "default" .Values.serviceAccount.name -}}
{{- end -}}
{{- end }}
EOF

  cat > "$dir/deployment.yaml" <<EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "${svc}.fullname" . }}
  labels: {{- include "${svc}.labels" . | nindent 4 }}
spec:
  replicas: {{ if .Values.autoscaling.enabled }}{{ .Values.autoscaling.minReplicas }}{{ else }}{{ .Values.replicaCount }}{{ end }}
  revisionHistoryLimit: {{ .Values.revisionHistoryLimit }}
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxUnavailable: 0
      maxSurge: 1
  selector:
    matchLabels: {{- include "${svc}.selectorLabels" . | nindent 6 }}
  template:
    metadata:
      labels: {{- include "${svc}.labels" . | nindent 8 }}
      annotations:
        checksum/config: {{ include (print \$.Template.BasePath "/configmap.yaml") . | sha256sum }}
        {{- with .Values.podAnnotations }}
        {{- toYaml . | nindent 8 }}
        {{- end }}
    spec:
      serviceAccountName: {{ include "${svc}.serviceAccountName" . }}
      automountServiceAccountToken: true
      {{- with .Values.imagePullSecrets }}
      imagePullSecrets: {{- toYaml . | nindent 8 }}
      {{- end }}
      securityContext: {{- toYaml .Values.podSecurityContext | nindent 8 }}
      topologySpreadConstraints:
        - maxSkew: 1
          topologyKey: topology.kubernetes.io/zone
          whenUnsatisfiable: ScheduleAnyway
          labelSelector:
            matchLabels: {{- include "${svc}.selectorLabels" . | nindent 14 }}
      containers:
        - name: ${svc}
          image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
          imagePullPolicy: {{ .Values.image.pullPolicy }}
          securityContext: {{- toYaml .Values.securityContext | nindent 12 }}
          ports:
            - name: http
              containerPort: 8080
              protocol: TCP
          envFrom:
            - configMapRef:
                name: {{ include "${svc}.fullname" . }}-config
            - secretRef:
                name: {{ if .Values.secrets.externalSecretName }}{{ .Values.secrets.externalSecretName }}{{ else }}{{ include "${svc}.fullname" . }}-secret{{ end }}
          livenessProbe:
            httpGet:
              path: {{ .Values.probes.liveness.path }}
              port: http
            initialDelaySeconds: {{ .Values.probes.liveness.initialDelaySeconds }}
            periodSeconds: {{ .Values.probes.liveness.periodSeconds }}
            failureThreshold: {{ .Values.probes.liveness.failureThreshold }}
          readinessProbe:
            httpGet:
              path: {{ .Values.probes.readiness.path }}
              port: http
            initialDelaySeconds: {{ .Values.probes.readiness.initialDelaySeconds }}
            periodSeconds: {{ .Values.probes.readiness.periodSeconds }}
            failureThreshold: {{ .Values.probes.readiness.failureThreshold }}
          startupProbe:
            httpGet:
              path: {{ .Values.probes.startup.path }}
              port: http
            initialDelaySeconds: {{ .Values.probes.startup.initialDelaySeconds }}
            periodSeconds: {{ .Values.probes.startup.periodSeconds }}
            failureThreshold: {{ .Values.probes.startup.failureThreshold }}
          resources: {{- toYaml .Values.resources | nindent 12 }}
          volumeMounts:
            - name: tmp
              mountPath: /tmp
            {{- if .Values.keyVault.enabled }}
            - name: keyvault-secrets
              mountPath: /mnt/secrets
              readOnly: true
            {{- end }}
      volumes:
        - name: tmp
          emptyDir: {}
        {{- if .Values.keyVault.enabled }}
        - name: keyvault-secrets
          csi:
            driver: secrets-store.csi.k8s.io
            readOnly: true
            volumeAttributes:
              secretProviderClass: {{ include "${svc}.fullname" . }}-spc
        {{- end }}
      {{- with .Values.nodeSelector }}
      nodeSelector: {{- toYaml . | nindent 8 }}
      {{- end }}
      {{- with .Values.affinity }}
      affinity: {{- toYaml . | nindent 8 }}
      {{- end }}
      {{- with .Values.tolerations }}
      tolerations: {{- toYaml . | nindent 8 }}
      {{- end }}
EOF

  cat > "$dir/service.yaml" <<EOF
apiVersion: v1
kind: Service
metadata:
  name: {{ include "${svc}.fullname" . }}
  labels: {{- include "${svc}.labels" . | nindent 4 }}
spec:
  type: {{ .Values.service.type }}
  {{- with .Values.service.sessionAffinity }}
  sessionAffinity: {{ . }}
  sessionAffinityConfig:
    clientIP:
      timeoutSeconds: 10800
  {{- end }}
  ports:
    - name: http
      port: {{ .Values.service.port }}
      targetPort: {{ .Values.service.targetPort }}
      protocol: TCP
      {{- with .Values.service.appProtocol }}
      appProtocol: {{ . }}
      {{- end }}
  selector: {{- include "${svc}.selectorLabels" . | nindent 4 }}
EOF

  cat > "$dir/configmap.yaml" <<EOF
apiVersion: v1
kind: ConfigMap
metadata:
  name: {{ include "${svc}.fullname" . }}-config
  labels: {{- include "${svc}.labels" . | nindent 4 }}
data:
  {{- range \$key, \$value := .Values.config }}
  {{ \$key }}: {{ \$value | quote }}
  {{- end }}
EOF

  cat > "$dir/secret.yaml" <<EOF
{{- if not .Values.secrets.externalSecretName }}
apiVersion: v1
kind: Secret
metadata:
  name: {{ include "${svc}.fullname" . }}-secret
  labels: {{- include "${svc}.labels" . | nindent 4 }}
type: Opaque
stringData:
  {{- range \$key, \$value := .Values.secrets.literal }}
  {{- if \$value }}
  {{ \$key }}: {{ \$value | quote }}
  {{- end }}
  {{- end }}
{{- end }}
EOF

  cat > "$dir/hpa.yaml" <<EOF
{{- if .Values.autoscaling.enabled }}
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: {{ include "${svc}.fullname" . }}
  labels: {{- include "${svc}.labels" . | nindent 4 }}
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: {{ include "${svc}.fullname" . }}
  minReplicas: {{ .Values.autoscaling.minReplicas }}
  maxReplicas: {{ .Values.autoscaling.maxReplicas }}
  metrics:
    {{- if .Values.autoscaling.targetCPUUtilizationPercentage }}
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: {{ .Values.autoscaling.targetCPUUtilizationPercentage }}
    {{- end }}
    {{- if .Values.autoscaling.targetMemoryUtilizationPercentage }}
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: {{ .Values.autoscaling.targetMemoryUtilizationPercentage }}
    {{- end }}
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
        - type: Percent
          value: 50
          periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
        - type: Percent
          value: 100
          periodSeconds: 30
{{- end }}
EOF

  cat > "$dir/ingress.yaml" <<EOF
{{- if .Values.ingress.enabled }}
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: {{ include "${svc}.fullname" . }}
  labels: {{- include "${svc}.labels" . | nindent 4 }}
  {{- with .Values.ingress.annotations }}
  annotations: {{- toYaml . | nindent 4 }}
  {{- end }}
spec:
  ingressClassName: {{ .Values.ingress.className }}
  {{- with .Values.ingress.tls }}
  tls: {{- toYaml . | nindent 4 }}
  {{- end }}
  rules:
    {{- range .Values.ingress.hosts }}
    - host: {{ .host | quote }}
      http:
        paths:
          {{- range .paths }}
          - path: {{ .path }}
            pathType: {{ .pathType }}
            backend:
              service:
                name: {{ include "${svc}.fullname" \$ }}
                port:
                  number: {{ \$.Values.service.port }}
          {{- end }}
    {{- end }}
{{- end }}
EOF

  cat > "$dir/networkpolicy.yaml" <<EOF
{{- if .Values.networkPolicy.enabled }}
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: {{ include "${svc}.fullname" . }}
  labels: {{- include "${svc}.labels" . | nindent 4 }}
spec:
  podSelector:
    matchLabels: {{- include "${svc}.selectorLabels" . | nindent 6 }}
  policyTypes:
    - Ingress
    - Egress
  ingress:
    - from:
        {{- toYaml .Values.networkPolicy.ingressFrom | nindent 8 }}
      ports:
        - protocol: TCP
          port: 8080
  egress:
    - to:
        {{- range .Values.networkPolicy.egressTo }}
        {{- if .cidr }}
        - ipBlock:
            cidr: {{ .cidr | quote }}
        {{- end }}
        {{- end }}
    - to:
        - namespaceSelector:
            matchLabels: { name: kube-system }
      ports:
        - protocol: UDP
          port: 53
        - protocol: TCP
          port: 53
{{- end }}
EOF

  cat > "$dir/pdb.yaml" <<EOF
{{- if .Values.podDisruptionBudget.enabled }}
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: {{ include "${svc}.fullname" . }}
  labels: {{- include "${svc}.labels" . | nindent 4 }}
spec:
  minAvailable: {{ .Values.podDisruptionBudget.minAvailable }}
  selector:
    matchLabels: {{- include "${svc}.selectorLabels" . | nindent 6 }}
{{- end }}
EOF

  cat > "$dir/serviceaccount.yaml" <<EOF
{{- if .Values.serviceAccount.create }}
apiVersion: v1
kind: ServiceAccount
metadata:
  name: {{ include "${svc}.serviceAccountName" . }}
  labels: {{- include "${svc}.labels" . | nindent 4 }}
  {{- if .Values.serviceAccount.azureWorkloadIdentityClientId }}
  annotations:
    azure.workload.identity/client-id: {{ .Values.serviceAccount.azureWorkloadIdentityClientId | quote }}
  {{- end }}
{{- end }}
EOF

  cat > "$dir/servicemonitor.yaml" <<EOF
{{- if .Values.serviceMonitor.enabled }}
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: {{ include "${svc}.fullname" . }}
  labels:
    {{- include "${svc}.labels" . | nindent 4 }}
    {{- with .Values.serviceMonitor.labels }}
    {{- toYaml . | nindent 4 }}
    {{- end }}
spec:
  selector:
    matchLabels: {{- include "${svc}.selectorLabels" . | nindent 6 }}
  endpoints:
    - port: http
      path: /metrics
      interval: {{ .Values.serviceMonitor.interval }}
      scrapeTimeout: {{ .Values.serviceMonitor.scrapeTimeout }}
{{- end }}
EOF

  cat > "$dir/secretproviderclass.yaml" <<EOF
{{- if .Values.keyVault.enabled }}
apiVersion: secrets-store.csi.x-k8s.io/v1
kind: SecretProviderClass
metadata:
  name: {{ include "${svc}.fullname" . }}-spc
  labels: {{- include "${svc}.labels" . | nindent 4 }}
spec:
  provider: azure
  parameters:
    usePodIdentity: "false"
    useVMManagedIdentity: "false"
    clientID: {{ .Values.serviceAccount.azureWorkloadIdentityClientId | quote }}
    keyvaultName: {{ .Values.keyVault.name | quote }}
    cloudName: ""
    tenantId: {{ .Values.keyVault.tenantId | quote }}
    objects: |
      array:
        {{- range .Values.keyVault.secrets }}
        - |
          objectName: {{ . | quote }}
          objectType: secret
        {{- end }}
  secretObjects:
    - secretName: {{ include "${svc}.fullname" . }}-keyvault
      type: Opaque
      data:
        {{- range .Values.keyVault.secrets }}
        - objectName: {{ . | quote }}
          key: {{ . | quote }}
        {{- end }}
{{- end }}
EOF
}

for svc in catalog pricing basket ordering inventory payment delivery notification customersupport reviews gateway; do
  gen "$svc"
done
echo "templates generated"
