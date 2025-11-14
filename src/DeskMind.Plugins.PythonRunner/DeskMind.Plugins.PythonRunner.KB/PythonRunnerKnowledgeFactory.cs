using DeskMind.Core.Security;
using DeskMind.Rag.Abstractions;
using DeskMind.Rag.Hosting;
using DeskMind.Rag.Models;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace DeskMind.Plugins.PythonRunner.KB
{
    /// <summary>
    /// RAG knowledge source for the Python Runner.
    ///
    /// It scans all embedded resources in this assembly, filters by allowed extensions
    /// (e.g. .pdf, .md, .txt), writes them to a temp folder, and lets the RAG ingestion
    /// pipeline (extractors + splitter + embeddings) build the vector DB automatically.
    ///
    /// You can reuse this pattern in other plugins by copying this factory and changing
    /// the default namespace / metadata.
    /// </summary>
    public sealed class PythonRunnerKnowledgeFactory : RagSourceFactoryBase
    {
        private const string SourceName = "python_runner_kb";
        private const string KnowledgeVersion = "1.0.0";

        // Adjust this if your default namespace changes
        private const string ResourceNamespacePrefix = "DeskMind.Plugins.PythonRunner.KB.";

        // Any files with these extensions will be ingested
        private static readonly string[] AllowedExtensions =
        {
            ".pdf",
            ".md",
            ".txt",
            ".html",
            ".docx",
            ".pptx",
            ".xlsx"
        };

        public PythonRunnerKnowledgeFactory()
            : base(new RagSourceMetadata(
                Name: SourceName,
                DisplayName: "Python Runner – Knowledge Base",
                Description: "Embedded Python-related documentation (PEP8, cheatsheets, etc.) used by DeskMind RAG.",
                IconGlyph: "\uE7B8",          // code-like glyph if you want
                VectorStoreName: "local",
                IsKnowledgePack: true,
                Version: KnowledgeVersion,
                Tags: new[] { "python", "runner", "pep8", "kb" }
            ))
        {
        }

        public override bool IsAvailable() => true;

        public override void LoadConfiguration()
        {
            // No-op for now. You can later load persisted configuration here (e.g., versioning).
        }

        public override void SaveConfiguration()
        {
            // No-op for now. You can persist configuration (including KnowledgeVersion) if needed.
        }

        public override T GetConfigValue<T>(string name, T defaultValue)
        {
            // Minimal implementation: no stored config yet, just return default.
            return defaultValue;
        }

        public override IRagSource? CreateSource(IServiceProvider services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // 1) Resolve RAG services from DI
            var memoryResolver = services.GetRequiredService<IVectorMemoryResolver>();
            var embeddings = services.GetRequiredService<IEmbeddingGenerator>();
            var ingestionFactory = services.GetRequiredService<IDocumentIngestionServiceFactory>();
            var retrieverFactory = services.GetRequiredService<IRagRetrieverFactory>();

            var memoryName = Metadata.VectorStoreName ?? "local";
            var memory = memoryResolver.Get(memoryName);

            var ingestion = ingestionFactory.Create(memory, embeddings);
            var retriever = retrieverFactory.Create(memory, embeddings);

            var source = new RagSource(
                name: Metadata.Name,
                memory: memory,
                ingestion: ingestion,
                retriever: retriever,
                embeddings: embeddings);

            // 2) Ingest all embedded resources that look like documents
            IngestAllEmbeddedDocuments(source.Ingestion);

            return source;
        }

        /// <summary>
        /// Enumerates all embedded resources in this KB assembly, filters by allowed
        /// extensions, writes them to a temp folder, and calls IngestAsync for each.
        /// </summary>
        private void IngestAllEmbeddedDocuments(IDocumentIngestionService ingestion)
        {
            var asm = typeof(PythonRunnerKnowledgeFactory).Assembly;
            var allResources = asm.GetManifestResourceNames();

            var docResources = allResources
                .Where(name =>
                    name.StartsWith(ResourceNamespacePrefix, StringComparison.OrdinalIgnoreCase) &&
                    AllowedExtensions.Any(ext =>
                        name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            if (docResources.Length == 0)
            {
                // Nothing to ingest; that's OK, but you may want to log from outside.
                return;
            }

            // Temp root: %TEMP%\DeskMind\PythonRunnerKB\<SourceName>\
            var tempRoot = Path.Combine(
                Path.GetTempPath(),
                "DeskMind",
                "PythonRunnerKB",
                SourceName);

            Directory.CreateDirectory(tempRoot);

            foreach (var resName in docResources)
            {
                using var stream = asm.GetManifestResourceStream(resName);
                if (stream == null)
                    continue;

                var fullPath = MapResourceNameToTempPath(tempRoot, resName);

                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                using (var file = File.Create(fullPath))
                {
                    stream.CopyTo(file);
                }

                // Use the full RAG pipeline: extractors choose based on file extension.
                ingestion
                    .IngestAsync(fullPath, ct: CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        /// <summary>
        /// Maps an embedded resource name like:
        ///   "DeskMind.Plugins.PythonRunner.KB.PDF.pep8_guidelines.pdf"
        /// to a temp file path like:
        ///   "<tempRoot>\PDF\pep8_guidelines.pdf"
        ///
        /// For deeper paths:
        ///   "DeskMind.Plugins.PythonRunner.KB.Docs.Errors.try_except_guide.md"
        /// becomes:
        ///   "<tempRoot>\Docs\Errors\try_except_guide.md"
        /// </summary>
        private static string MapResourceNameToTempPath(string tempRoot, string resourceName)
        {
            // Strip namespace prefix
            string shortName = resourceName;
            if (shortName.StartsWith(ResourceNamespacePrefix, StringComparison.OrdinalIgnoreCase))
                shortName = shortName.Substring(ResourceNamespacePrefix.Length);

            // Find extension
            int lastDot = shortName.LastIndexOf('.');
            if (lastDot < 0)
            {
                // No extension → treat dots as directory separators
                string path = shortName.Replace('.', Path.DirectorySeparatorChar);
                return Path.Combine(tempRoot, path);
            }

            string ext = shortName.Substring(lastDot + 1);        // pdf
            string baseName = shortName.Substring(0, lastDot);    // PDF.pep8_guidelines

            // Split into segments: ["PDF", "pep8_guidelines"]
            string[] segments = baseName.Split('.');

            // Directory = all segments except last
            string directoryPath = string.Empty;

            if (segments.Length > 1)
            {
                // Join directory segments with separator: "PDF"
                directoryPath = string.Join(
                    Path.DirectorySeparatorChar.ToString(),
                    segments,
                    0,
                    segments.Length - 1);
            }

            // File name
            string fileNameWithoutExt = segments[segments.Length - 1];
            string fileName = fileNameWithoutExt + "." + ext;

            // Build final path
            if (string.IsNullOrEmpty(directoryPath))
            {
                return Path.Combine(tempRoot, fileName);
            }

            return Path.Combine(tempRoot, directoryPath, fileName);
        }

        public override void UpdateSecurityState(SecurityPolicy policy, string currentUserId)
        {
            //TODO: Security
        }
    }
}