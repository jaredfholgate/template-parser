using Newtonsoft.Json;
using System.Diagnostics;
using System.CommandLine;
using Template.Parser.Core;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Reflection.Metadata;

namespace Template.Parser.Cli
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand("ARM Parser");

            var sourceTemplateOption = new Option<string>("--sourceTemplate", "The fully qualified file path for the source ARM template.")
            {
                IsRequired = true,
                Arity = ArgumentArity.ExactlyOne,
            };
            sourceTemplateOption.AddAlias("-s");
            rootCommand.AddOption(sourceTemplateOption);

            var parametersOption = new Option<List<string>>("--parameter", "A parameter key value pair, in the format key=value.")
            {
                Arity = ArgumentArity.ZeroOrMore
            };
            parametersOption.AddAlias("-p");
            rootCommand.AddOption(parametersOption);

            var locationOption = new Option<string>("--location", "The default location value for the template.")
            {
                Arity = ArgumentArity.ZeroOrOne
            };
            parametersOption.AddAlias("-l");

            rootCommand.SetHandler((sourceTemplate, parameters, location) =>
            {
                RunParser(sourceTemplate.Trim(), parameters, location);
            },
            sourceTemplateOption, 
            parametersOption,
            locationOption);

            await rootCommand.InvokeAsync(args);
        }

        static void RunParser(string sourceTemplate, List<string> parameters, string location)
        {            
            if(string.IsNullOrEmpty(location))
            {
                location = "${default_location}";
            }

            var defaults = PlaceholderInputGenerator.GeneratePlaceholderDeploymentMetadata(location);

            Debug.WriteLine($"Reading {sourceTemplate}");
            var template = File.ReadAllText(sourceTemplate);

            Debug.WriteLine($"Parsing {sourceTemplate}");
            var parser = new ArmTemplateProcessor(template);
            JToken result = null;
            if(parameters.Count > 0)
            {
                var parametersJson = BuildParameters(parameters);
                result = parser.ProcessTemplate(parametersJson, defaults);
            }
            else
            {
                result = parser.ProcessTemplate(string.Empty, defaults);
            }

            Debug.WriteLine($"Serialising {sourceTemplate}");
            var json = JsonConvert.SerializeObject(result.SelectToken("resources")[0], Formatting.Indented);
            Console.Write(json);
        }

        public static string BuildParameters(List<string> parameters)
        {
            JObject jsonParameters = new JObject();

            foreach (var parameter in parameters)
            {
                var split = parameter.Trim().Split('=');
                var value = split[1];
                if (value.StartsWith("[[["))
                {
                    var typeValueSplit = value.Split(new string[] { "[[[", "]]]" }, StringSplitOptions.RemoveEmptyEntries);
                    switch (typeValueSplit[0])
                    {
                        case "Int64":
                            jsonParameters[split[0]] = new JObject(new JProperty("value", Int64.Parse(typeValueSplit[1])));
                            break;

                        default:
                            jsonParameters[split[0]] = new JObject(new JProperty("value", typeValueSplit[1]));
                            break;
                    }
                }
                else
                {
                    jsonParameters[split[0]] = new JObject(new JProperty("value", split[1]));
                }
            }

            return JObject.FromObject(new { parameters = jsonParameters }).ToString();
        }
    }
}
