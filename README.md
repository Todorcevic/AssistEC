# AssistEC - Asistente de IA para SharePoint

Aplicación desarrollada para **Experts Coding** que integra inteligencia artificial con documentos de SharePoint.

## Descripción

AssistEC es un asistente inteligente que permite consultar y analizar documentos almacenados en SharePoint utilizando OpenAI. Los usuarios pueden hacer preguntas en lenguaje natural sobre sus documentos y recibir respuestas contextuales basadas en el contenido.

## Características

- 🤖 **Chat inteligente** con OpenAI para consultas en lenguaje natural
- 📂 **Integración con SharePoint** via Microsoft Graph API
- 🔍 **Búsqueda avanzada** de documentos
- 📊 **Resúmenes automáticos** de documentos
- 🎯 **Respuestas contextuales** basadas en contenido real
- 🔧 **Modo de prueba** con documentos simulados

## Tecnologías

- .NET 8 / Blazor Server
- OpenAI SDK
- Microsoft Graph SDK
- Azure Identity
- Bootstrap

## Configuración

1. **Variable de entorno OpenAI:**
   ```
   OPENAI_API_KEY_UNITY=tu-api-key
   ```

2. **SharePoint (opcional):**
   ```json
   {
     "SharePoint": {
       "UseMockService": false,
       "TenantId": "tu-tenant-id",
       "ClientId": "tu-client-id",
       "ClientSecret": "tu-client-secret",
       "SiteUrl": "https://empresa.sharepoint.com/sites/sitio"
     }
   }
   ```

## Ejecutar

```bash
dotnet run
```

Navegar a: `http://localhost:5094/ai-assistant`

## Desarrollado por

**Experts Coding** - Soluciones tecnológicas innovadoras