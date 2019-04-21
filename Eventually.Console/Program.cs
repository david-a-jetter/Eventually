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

            var failOnAnnotateSave = 2L;
            var failOnAnnotateAck  = 2L;
            var failOnAnnotate     = 2L;

            var fieldService = new FieldService(fieldGenerationInterval, maxFields, failOnAnnotateSave);

            using (var publisher = new PublisherService(fieldService))
            using (var consumer  = new AnnotationService(
                publisher.AnnotateField,
                annotationRepublishInterval,
                failOnAnnotateAck,
                failOnAnnotate))
            {
                publisher.StartPublishing(consumer.Annotate, consumer.Acknowledge, fieldRepublishInterval);

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
                        var annotations         = consumer.Annotations;

                        if (maxFields == annotatedFieldCount)
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
