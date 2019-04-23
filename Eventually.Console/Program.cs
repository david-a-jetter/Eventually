using Eventually.Core.Consumer;
using Eventually.Core.Publisher;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace Eventually.ConsoleRunner
{
    static class Program
    {
        static void Main(string[] args)
        {
            var maxFields = 10000L;
            var fieldGenerationInterval     = TimeSpan.Zero;
            var annotationRepublishInterval = TimeSpan.FromSeconds(1);
            var fieldRepublishInterval      = TimeSpan.FromSeconds(1);

            var failOnAnnotateSaveOnEvery = 2;
            var failOnAnnotateAckOnEvery  = 2;
            var failOnAnnotateOnEvery     = 2;

            var fieldService = new FieldService(fieldGenerationInterval, maxFields, failOnAnnotateSaveOnEvery);

            using (var dataEntryService  = new DataEntryService(fieldService))
            using (var annotationService = new AnnotationService(
                dataEntryService.AnnotateField,
                annotationRepublishInterval,
                failOnAnnotateAckOnEvery,
                failOnAnnotateOnEvery))
            {
                dataEntryService.StartPublishing(
                    annotationService.Annotate,
                    annotationService.Acknowledge,
                    fieldRepublishInterval);

                bool consistency = false;

                var timer = new Stopwatch();
                timer.Start();

                Observable
                    .Interval(TimeSpan.FromSeconds(1))
                    .TakeUntil(_ => consistency)
                    .Subscribe(_ =>
                    {
                        var fields              = fieldService.Fields;
                        var fieldCount          = fields.Count;
                        var annotatedFieldCount = fields.Where(field => field.ActiveAnnotation != null).Count();
                        var annotations         = annotationService.Annotations;

                        if (fieldCount == annotatedFieldCount)
                        {
                            consistency = true;
                        }
                                                
                        Console.WriteLine($"Execution Seconds:       {timer.Elapsed.TotalSeconds.ToString("#")}");
                        Console.WriteLine($"Field Count:             {fieldCount}");
                        Console.WriteLine($"Annotated Field Count:   {annotatedFieldCount}");
                        Console.WriteLine($"Annotations Count:       {annotations.Count}");
                        Console.WriteLine($"Unannotated Field Count: {fieldCount - annotatedFieldCount}");
                        Console.WriteLine();

                        if (consistency)
                        {
                            timer.Stop();

                            Console.WriteLine("------------------------------");
                            Console.WriteLine("Eventual Consistency Achieved!");
                            Console.WriteLine("------------------------------");
                        }
                    });

                Console.ReadKey();
            }
        }
    }
}
