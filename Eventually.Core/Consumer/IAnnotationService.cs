using Eventually.Core.Publisher.Models;
using System.Threading.Tasks;

namespace Eventually.Core.Consumer
{
    interface IAnnotationService
    {
        Task Annotate(FirstClassField field);

        Task Acknowledge(long fieldId, Annotation annotation);
    }
}
