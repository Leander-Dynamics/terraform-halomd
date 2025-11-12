{{- define "edr-agent.fullname" -}}
{{- printf "%s-%s" .Release.Name "edr-agent" | trunc 63 | trimSuffix "-" -}}
{{- end -}}
