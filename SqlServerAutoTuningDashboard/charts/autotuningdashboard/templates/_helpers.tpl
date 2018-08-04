{{- define "imagePullSecret" }}
{{- printf "{\"auths\": {\"%s\": {\"auth\": \"%s\"}}}" .Values.imageCredentials.registry (printf "%s:%s" .Values.imageCredentials.username .Values.imageCredentials.password | b64enc) | b64enc }}
{{- end }}

{{- define "sqlUserIdSecret" }}
{{- printf "%s" .Values.sql.userid | b64enc }}
{{- end }}

{{- define "sqlPasswordSecret" }}
{{- printf "%s" .Values.sql.password | b64enc }}
{{- end }}