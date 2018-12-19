using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YadaYada.Bisque.Aws.CloudFormation;

namespace YadaYada.Bisque.Aws.Samples.Content
{
    class Program
    {
        private static List<Type> _templates;
        static void Main(string[] args)
        {
            // ReSharper disable once PossibleMistakenCallToGetType.2
            _templates = TemplateEngine.GetTemplateTypes(typeof(Program).GetType().GetTypeInfo().Assembly);
            do
            {
                Console.Clear();
                Console.Write("Enter template class name (partial OK) [<return> to exit]:");
                var templateToFind = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(templateToFind))
                {
                    break;
                }
                var templateTypes = _templates.Where(t => t.FullName.ToUpper().Contains(templateToFind.ToUpper()));

                Type templateToCreate = null;

                if (templateTypes.Count() > 1)
                {
                    templateToCreate = ChooseTemplate(templateTypes);
                }
                else
                {
                    templateToCreate = templateTypes.First();
                }

                if (templateToCreate != null)
                {
                    Template template = Activator.CreateInstance(templateToCreate) as Template;
                    var output = template.Save();
                    Console.WriteLine("Template output to '{0}.{1}.template'.", output.FullName,template.Key);
                    Console.Write("Press <return> to continue");
                    Console.ReadLine();
                }

            } while (true);

        }

        private static Type ChooseTemplate(IEnumerable<Type> templates)
        {
            Console.WriteLine();
            Console.WriteLine("More than one template found.");
            Console.WriteLine("Please choose:");
            for (int i = 0; i < templates.Count(); i++)
            {
                Console.WriteLine("[{0}] {1}", i, templates.ToList()[i].FullName);
            }
            var choice = Console.ReadLine();
            int choiceInt = 0;
            if (int.TryParse(choice, out choiceInt))
            {
                return templates.ToList()[choiceInt];
            }
            return null;
        }
    }
}
