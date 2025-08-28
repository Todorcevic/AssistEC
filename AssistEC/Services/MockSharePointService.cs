using AssistEC.Models;
using AssistEC.Services.Abstractions;

namespace AssistEC.Services;

public class MockSharePointService : ISharePointService
{
    private readonly ILogger<MockSharePointService> _logger;
    private readonly List<SharePointDocument> _mockDocuments;

    public MockSharePointService(ILogger<MockSharePointService> logger)
    {
        _logger = logger;
        _mockDocuments = GenerateMockDocuments();
    }

    public async Task<List<SharePointDocument>> SearchDocumentsAsync(string query)
    {
        await Task.Delay(500); // Simular latencia de red
        
        _logger.LogInformation($"Búsqueda simulada para: {query}");

        if (string.IsNullOrEmpty(query))
        {
            return _mockDocuments.Take(5).ToList();
        }

        // Búsqueda mejorada por palabras clave en nombre y contenido
        var keywords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Filtrar palabras de parada comunes
        var stopWords = new HashSet<string> 
        { 
            "el", "la", "de", "que", "y", "en", "un", "es", "se", "no", "te", "lo", "le", "da", "su", "por", "son", "con", "para", "al", "del", "los", "las", "una", "como", "pero", "sus", "fue", "ser", "todo", "está", "muy", "ya", "o", "cuando", "si", "más", "hasta", "sobre", "también", "me", "mi", "yo", "tú", "él", "ella", "nosotros", "ustedes", "ellos", "ellas", "tiene", "tienen", "cuántas", "cuántos", "cómo", "dónde", "qué", "quién", "cuál", "cuáles", "líneas", "archivo", "documento"
        };
        
        var relevantKeywords = keywords.Where(k => k.Length > 2 && !stopWords.Contains(k)).ToList();
        
        // Si no hay palabras relevantes, usar todas
        if (!relevantKeywords.Any())
        {
            relevantKeywords = keywords.ToList();
        }
        
        var filteredDocuments = _mockDocuments.Where(doc =>
            relevantKeywords.Any(keyword => 
                doc.Name.ToLower().Contains(keyword) || 
                doc.Content.ToLower().Contains(keyword)
            )).ToList();

        return filteredDocuments.Take(10).ToList();
    }

    public async Task<List<SharePointDocument>> GetRecentDocumentsAsync(int count = 10)
    {
        await Task.Delay(300); // Simular latencia de red
        
        _logger.LogInformation($"Obteniendo {count} documentos recientes simulados");
        
        return _mockDocuments
            .OrderByDescending(d => d.LastModified)
            .Take(count)
            .ToList();
    }

    private List<SharePointDocument> GenerateMockDocuments()
    {
        return new List<SharePointDocument>
        {
            new()
            {
                Id = "1",
                Name = "Presupuesto 2024.xlsx",
                WebUrl = "https://contoso.sharepoint.com/sites/finanzas/presupuesto2024.xlsx",
                Content = @"Presupuesto anual 2024 de la empresa. 
                
                INGRESOS PROYECTADOS:
                - Ventas de productos: $2,500,000
                - Servicios de consultoría: $800,000
                - Licencias de software: $300,000
                Total ingresos: $3,600,000
                
                GASTOS PROYECTADOS:
                - Salarios y beneficios: $1,800,000
                - Alquiler de oficinas: $240,000
                - Marketing y publicidad: $300,000
                - Tecnología e infraestructura: $200,000
                - Gastos operativos: $180,000
                Total gastos: $2,720,000
                
                UTILIDAD PROYECTADA: $880,000
                
                Notas importantes:
                - Se espera un crecimiento del 15% en ventas respecto al año anterior
                - Se planea contratar 10 nuevos empleados
                - Inversión en nueva infraestructura de nube",
                LastModified = DateTime.Now.AddDays(-2),
                Author = "María González"
            },
            new()
            {
                Id = "2",
                Name = "Manual de Empleados.docx",
                WebUrl = "https://contoso.sharepoint.com/sites/rrhh/manual-empleados.docx",
                Content = @"Manual del Empleado - Política y Procedimientos
                
                CAPÍTULO 1: INTRODUCCIÓN
                Bienvenido a nuestra empresa. Este manual contiene información importante sobre políticas, beneficios y procedimientos.
                
                CAPÍTULO 2: HORARIOS DE TRABAJO
                - Horario estándar: 9:00 AM - 6:00 PM, Lunes a Viernes
                - Flexibilidad de horarios disponible previa aprobación
                - Trabajo remoto: Hasta 2 días por semana
                
                CAPÍTULO 3: BENEFICIOS
                - Seguro médico completo
                - 20 días de vacaciones anuales
                - Días de enfermedad pagados
                - Plan de pensiones con contribución del empleador
                
                CAPÍTULO 4: CÓDIGO DE CONDUCTA
                - Respeto mutuo y diversidad
                - Confidencialidad de información
                - Uso apropiado de recursos de la empresa
                
                CAPÍTULO 5: PROCEDIMIENTOS DE ESCALACIÓN
                Cualquier problema debe reportarse primero al supervisor directo, luego a RRHH si es necesario.",
                LastModified = DateTime.Now.AddDays(-7),
                Author = "Carlos Ruiz"
            },
            new()
            {
                Id = "3",
                Name = "Proyecto Alpha - Especificaciones.pdf",
                WebUrl = "https://contoso.sharepoint.com/sites/proyectos/proyecto-alpha-specs.pdf",
                Content = @"PROYECTO ALPHA - ESPECIFICACIONES TÉCNICAS
                
                RESUMEN EJECUTIVO:
                El Proyecto Alpha busca desarrollar una nueva plataforma de e-commerce que integre IA para personalización.
                
                OBJETIVOS:
                1. Crear una experiencia de compra personalizada
                2. Implementar recomendaciones basadas en IA
                3. Mejorar la conversión en un 25%
                4. Reducir el tiempo de carga a menos de 2 segundos
                
                TECNOLOGÍAS PROPUESTAS:
                - Frontend: React.js con TypeScript
                - Backend: .NET 8 con Entity Framework
                - Base de datos: SQL Server
                - IA/ML: Azure Cognitive Services
                - Hosting: Azure App Service
                
                CRONOGRAMA:
                - Fase 1 (Mes 1-2): Diseño y arquitectura
                - Fase 2 (Mes 3-5): Desarrollo del MVP
                - Fase 3 (Mes 6): Testing y optimización
                - Fase 4 (Mes 7): Deployment y lanzamiento
                
                PRESUPUESTO ESTIMADO: $450,000
                
                EQUIPO ASIGNADO:
                - Project Manager: Ana Silva
                - Tech Lead: Roberto Chen
                - Desarrolladores: 4 personas
                - QA Engineer: 1 persona
                - UX Designer: 1 persona",
                LastModified = DateTime.Now.AddDays(-1),
                Author = "Ana Silva"
            },
            new()
            {
                Id = "4",
                Name = "Políticas de Seguridad IT.docx",
                WebUrl = "https://contoso.sharepoint.com/sites/it/politicas-seguridad.docx",
                Content = @"POLÍTICAS DE SEGURIDAD DE TECNOLOGÍAS DE INFORMACIÓN
                
                1. GESTIÓN DE CONTRASEÑAS
                - Mínimo 12 caracteres con mayúsculas, minúsculas, números y símbolos
                - Cambio obligatorio cada 90 días
                - No reutilizar las últimas 12 contraseñas
                - Uso obligatorio de autenticación de dos factores (2FA)
                
                2. ACCESO A SISTEMAS
                - Principio de menor privilegio
                - Revisión trimestral de permisos
                - Desactivación inmediata de cuentas de empleados que se retiran
                
                3. PROTECCIÓN DE DATOS
                - Encriptación de datos sensibles en reposo y en tránsito
                - Backups diarios con retención de 30 días
                - Clasificación de información: Pública, Interna, Confidencial, Restringida
                
                4. INCIDENTES DE SEGURIDAD
                - Reporte inmediato al equipo de IT Security
                - Análisis forense cuando sea necesario
                - Comunicación a stakeholders según el protocolo establecido
                
                5. CAPACITACIÓN
                - Entrenamiento anual obligatorio en ciberseguridad
                - Simulacros de phishing trimestrales
                - Actualización continua sobre nuevas amenazas",
                LastModified = DateTime.Now.AddDays(-5),
                Author = "David López"
            },
            new()
            {
                Id = "5",
                Name = "Reunión Junta Directiva - Actas.docx",
                WebUrl = "https://contoso.sharepoint.com/sites/ejecutivo/actas-junta.docx",
                Content = @"ACTAS DE REUNIÓN - JUNTA DIRECTIVA
                Fecha: 15 de Agosto 2024
                Participantes: CEO, CFO, CTO, VP Ventas, VP Marketing
                
                TEMAS TRATADOS:
                
                1. REVISIÓN FINANCIERA Q2 2024
                - Ingresos superaron proyecciones en 8%
                - Márgenes de ganancia mantuvieron estabilidad
                - Cash flow positivo por sexto trimestre consecutivo
                
                2. EXPANSIÓN INTERNACIONAL
                - Aprobación para abrir oficina en México
                - Presupuesto asignado: $200,000 para setup inicial
                - Contratación de Director Regional prevista para septiembre
                
                3. INICIATIVAS DE SOSTENIBILIDAD
                - Compromiso para reducir huella de carbono 30% para 2025
                - Inversión en energías renovables para oficinas
                - Programa de reciclaje y reducción de papel
                
                4. ACTUALIZACIÓN TECNOLÓGICA
                - Migración a la nube completada exitosamente
                - Implementación de IA en procesos de atención al cliente
                - Presupuesto adicional aprobado para ciberseguridad
                
                DECISIONES TOMADAS:
                - Aprobar expansión a México
                - Aumentar inversión en R&D en 15%
                - Revisar salarios para mantener competitividad en el mercado
                
                PRÓXIMA REUNIÓN: 15 de Septiembre 2024",
                LastModified = DateTime.Now.AddHours(-12),
                Author = "Secretaria Ejecutiva"
            },
            new()
            {
                Id = "6",
                Name = "Análisis de Competencia 2024.pptx",
                WebUrl = "https://contoso.sharepoint.com/sites/marketing/analisis-competencia.pptx",
                Content = @"ANÁLISIS DE COMPETENCIA 2024
                
                COMPETIDORES PRINCIPALES:
                
                1. TECHCORP SOLUTIONS
                - Cuota de mercado: 35%
                - Fortalezas: Marca establecida, amplia red de distribución
                - Debilidades: Tecnología obsoleta, servicio al cliente deficiente
                - Precio promedio: 15% más alto que nosotros
                
                2. INNOVATECH
                - Cuota de mercado: 22%
                - Fortalezas: Innovación tecnológica, equipo joven
                - Debilidades: Falta de experiencia, recursos limitados
                - Precio promedio: Similar al nuestro
                
                3. MEGASYSTEMS
                - Cuota de mercado: 18%
                - Fortalezas: Precios competitivos, buena relación calidad-precio
                - Debilidades: Limitada capacidad de customización
                - Precio promedio: 20% menor que nosotros
                
                OPORTUNIDADES IDENTIFICADAS:
                - Mercado de pequeñas empresas está desatendido
                - Creciente demanda por soluciones cloud-native
                - Integración con IA es diferenciador clave
                
                AMENAZAS:
                - Nuevos entrantes con tecnología disruptiva
                - Guerra de precios en segmento corporativo
                - Cambios regulatorios en protección de datos
                
                RECOMENDACIONES ESTRATÉGICAS:
                1. Acelerar desarrollo de soluciones IA
                2. Enfocar marketing en pequeñas empresas
                3. Mejorar propuesta de valor vs. MegaSystems
                4. Invertir en customer success para retención",
                LastModified = DateTime.Now.AddDays(-3),
                Author = "Elena Morales"
            },
            new()
            {
                Id = "7",
                Name = "Plan de Marketing Q4.docx",
                WebUrl = "https://contoso.sharepoint.com/sites/marketing/plan-q4.docx",
                Content = @"PLAN DE MARKETING Q4 2024
                
                OBJETIVOS:
                - Incrementar leads calificados en 40%
                - Lanzar 2 campañas principales
                - Mejorar brand awareness en 25%
                - ROI mínimo de 300% en campañas digitales
                
                CAMPAÑAS PLANIFICADAS:
                
                1. CAMPAÑA 'FUTURO DIGITAL'
                - Duración: Octubre - Noviembre
                - Presupuesto: $80,000
                - Canales: LinkedIn, Google Ads, Content Marketing
                - Objetivo: C-level executives en empresas medianas
                
                2. CAMPAÑA 'BLACK FRIDAY TECH'
                - Duración: Noviembre
                - Presupuesto: $45,000
                - Canales: Redes sociales, Email marketing
                - Objetivo: Pequeñas empresas y startups
                
                ACTIVIDADES DE CONTENIDO:
                - 12 blog posts técnicos
                - 8 webinars con expertos
                - 4 whitepapers descargables
                - 16 posts en redes sociales por semana
                
                EVENTOS:
                - Participación en TechSummit 2024
                - Hosting de 'AI in Business' workshop
                - Partner event con Microsoft
                
                MÉTRICAS DE ÉXITO:
                - Cost per lead < $25
                - Conversion rate > 12%
                - Email open rate > 28%
                - Social engagement +35%
                
                PRESUPUESTO TOTAL: $150,000",
                LastModified = DateTime.Now.AddDays(-4),
                Author = "Marco Torres"
            },
            new()
            {
                Id = "8",
                Name = "Procedimientos de Emergencia.pdf",
                WebUrl = "https://contoso.sharepoint.com/sites/admin/emergencias.pdf",
                Content = @"PROCEDIMIENTOS DE EMERGENCIA EMPRESARIAL
                
                1. EVACUACIÓN DE OFICINAS
                
                RUTAS DE EVACUACIÓN:
                - Salida principal: Lobby del edificio
                - Salida secundaria: Escaleras de emergencia (Este y Oeste)
                - Punto de reunión: Parque Central frente al edificio
                
                PROTOCOLO:
                1. Al sonar la alarma, suspender actividades inmediatamente
                2. No usar elevadores
                3. Caminar ordenadamente hacia salidas designadas
                4. Coordinadores de piso verifican evacuación completa
                5. Reportarse en punto de reunión para conteo
                
                2. EMERGENCIAS MÉDICAS
                - Llamar inmediatamente al 911
                - Notificar a seguridad del edificio
                - Aplicar primeros auxilios si está capacitado
                - No mover a persona lesionada a menos que haya peligro inmediato
                
                3. INCENDIOS
                - Activar alarma de incendio
                - Llamar a bomberos (911)
                - Usar extintores solo si el fuego es pequeño y controlable
                - Evacuación inmediata si el fuego es grande
                
                4. TERREMOTOS
                - Durante: Protegerse bajo escritorio, alejarse de ventanas
                - Después: Evacuar ordenadamente si es seguro
                - No usar elevadores
                - Verificar heridos y daños
                
                5. AMENAZAS DE SEGURIDAD
                - Reportar inmediatamente a seguridad y policía
                - Seguir instrucciones de autoridades
                - Mantener la calma y no confrontar amenazas
                
                CONTACTOS DE EMERGENCIA:
                - Bomberos/Policía/Ambulancia: 911
                - Seguridad del edificio: ext. 5555
                - Coordinador de emergencias: Juan Pérez ext. 1234",
                LastModified = DateTime.Now.AddDays(-10),
                Author = "Oficina de Seguridad"
            }
        };
    }
}