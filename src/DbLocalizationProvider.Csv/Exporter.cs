using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DbLocalizationProvider.Export;

namespace DbLocalizationProvider.Csv
{
    public class Exporter : IResourceExporter
    {
        private readonly Func<ICollection<CultureInfo>> _languagesFactory;

        public Exporter(Func<ICollection<CultureInfo>> languagesFactory)
        {
            _languagesFactory = languagesFactory;
        }
        
        public string FormatName => "CSV";
        
        public string ProviderId => "csv";

        public ExportResult Export(ICollection<LocalizationResource> resources, NameValueCollection parameters)
        {
            var records = new List<object>();
            var languages = _languagesFactory();

            foreach (var resource in resources.OrderBy(x => x.ResourceKey))
            {
                dynamic record = new ExpandoObject();

                record.ResourceKey = resource.ResourceKey;

                foreach (var language in languages)
                {
                    var translation = resource.Translations.ByLanguage(language.Name, false);
                    AddProperty(record, language.Name, translation);
                }

                records.Add(record);
            }

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture);
            
            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
            using (var csv = new CsvWriter(streamWriter, csvConfig))
            {
                csv.WriteRecords(records);
                csv.Flush();
                var bytes = stream.ToArray();
                var csvContent = Encoding.UTF8.GetString(bytes);
                var fileName = $"localization-resources-{DateTime.UtcNow:yyyyMMdd}.csv";
                return new ExportResult(csvContent, "text/csv", fileName);
            }
        }

        private void AddProperty(ExpandoObject record, string languageName, string translation)
        {
            var recordDict = record as IDictionary<string, object>;

            if (recordDict.ContainsKey(languageName))
            {
                recordDict[languageName] = translation;
            }
            else
            {
                recordDict.Add(languageName, translation);
            }
        }
    }
}
