using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DbLocalizationProvider.Import;

namespace DbLocalizationProvider.Csv
{
    public class FormatParser : IResourceFormatParser
    {
        private readonly Func<ICollection<CultureInfo>> _languagesFactory;

        public FormatParser(Func<ICollection<CultureInfo>> languagesFactory)
        {
            _languagesFactory = languagesFactory;
        }

        public string FormatName => "CSV";
        
        public string[] SupportedFileExtensions => new[] {".csv"};
        
        public string ProviderId => "csv";

        public ParseResult Parse(string fileContent)
        {
            var resources = new List<LocalizationResource>();
            var languages = _languagesFactory();
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture);

            using (var stream = AsStream(fileContent))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var csv = new CsvReader(reader, csvConfig))
            {
                var records = csv.GetRecords<dynamic>().ToList();

                foreach (var record in records)
                {
                    var dict = (IDictionary<string, object>)record;
                    var resourceKey = dict["ResourceKey"] as string;
                    var resource = new LocalizationResource(resourceKey, false);
                    SetTranslations(resource, dict, languages);
                    resources.Add(resource);
                }
            }

            return new ParseResult(resources, languages);
        }

        private void SetTranslations(LocalizationResource resource, IDictionary<string, object> record, IEnumerable<CultureInfo> languages)
        {
            resource.Translations.AddRange(languages.Select(x => new LocalizationResourceTranslation
            {
                Language = x.Name,
                Value = record.ContainsKey(x.Name)
                    ? record[x.Name] as string
                    : null
            }));
        }

        private Stream AsStream(string fileContent)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(fileContent);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
