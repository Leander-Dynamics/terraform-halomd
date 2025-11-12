{{- define "fastapi-app.fullname" -}}
{{- printf "%s-%s" .Release.Name "fastapi" | trunc 63 | trimSuffix "-" -}}
{{- end -}}
